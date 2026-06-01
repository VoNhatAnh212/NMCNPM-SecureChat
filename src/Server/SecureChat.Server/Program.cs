using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SecureChat.Application;
using SecureChat.Application.Services.Realtime;
using SecureChat.Infrastructure;
using SecureChat.Infrastructure.Data;
using SecureChat.Server.Filters;
using SecureChat.Server.Hubs;
using SecureChat.Server.Middleware;
using SecureChat.Server.Security;
using SecureChat.Server.Services;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var startupLogger = LoggerFactory.Create(logging => logging.AddConsole()).CreateLogger("Startup");
var serverCertificate = ServerCertificateProvider.LoadOrCreate(configuration, builder.Environment, startupLogger);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.ServerCertificate = serverCertificate;
    });
});

builder.Services.AddInfrastructure(configuration);
builder.Services.AddApplication();

builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
builder.Services.AddSingleton<IUserIdProvider, SecureChatUserIdProvider>();

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"] ?? configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret is missing."))),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCors", policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SecureChat API",
        Version = "v1"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT Bearer token.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [securityScheme] = Array.Empty<string>()
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("DevelopmentCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

if (app.Configuration.GetValue("Database:AutoMigrate", app.Environment.IsDevelopment()))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    await EnsureAttachmentUploadSchemaAsync(dbContext);
}

app.Run();

static async Task EnsureAttachmentUploadSchemaAsync(AppDbContext dbContext)
{
    await dbContext.Database.ExecuteSqlRawAsync("""
        IF OBJECT_ID(N'[dbo].[ChatSessionKeys]', N'U') IS NOT NULL
        BEGIN
            DROP TABLE [dbo].[ChatSessionKeys];
        END

        IF EXISTS (
            SELECT 1
            FROM sys.foreign_keys
            WHERE name = N'FK_Attachments_Messages_MessageId'
        )
        BEGIN
            ALTER TABLE [dbo].[Attachments] DROP CONSTRAINT [FK_Attachments_Messages_MessageId];
        END

        IF EXISTS (
            SELECT 1
            FROM sys.columns
            WHERE object_id = OBJECT_ID(N'[dbo].[Attachments]')
              AND name = N'MessageId'
              AND is_nullable = 0
        )
        BEGIN
            ALTER TABLE [dbo].[Attachments] ALTER COLUMN [MessageId] uniqueidentifier NULL;
        END

        IF NOT EXISTS (
            SELECT 1
            FROM sys.foreign_keys
            WHERE name = N'FK_Attachments_Messages_MessageId'
        )
        BEGIN
            ALTER TABLE [dbo].[Attachments]
            ADD CONSTRAINT [FK_Attachments_Messages_MessageId]
            FOREIGN KEY ([MessageId]) REFERENCES [dbo].[Messages]([Id]) ON DELETE SET NULL;
        END
        """);
}
