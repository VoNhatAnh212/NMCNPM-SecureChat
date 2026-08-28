# SecureChat

SecureChat là ứng dụng chat client/server dùng WinForms + ASP.NET Core, có đăng ký bằng OTP email, TLS, RSA device key, AES-GCM session key, friend, group, file/media và lưu dữ liệu local riêng theo từng user. Chủ yếu sử dụng vibe code để hoàn thành đồ án.

## Yêu Cầu

- Windows
- Visual Studio 2022 hoặc mới hơn
- .NET SDK 10
- SQL Server LocalDB hoặc SQL Server Express
- SSMS nếu muốn xem database thủ công
- Tài khoản Gmail có bật 2-Step Verification để tạo App Password gửi OTP

## Cấu Trúc Chính

```text
src/
  Server/
    SecureChat.Server/          API server, SignalR, TLS cert, appsettings
    SecureChat.Application/     Business logic
    SecureChat.Infrastructure/  EF Core, SQL Server repositories
    SecureChat.Domain/          Entities
  Client/
    SecureChat.Client/          WinForms client
  Shared/
    SecureChat.Shared/          DTOs, enums, constants
```

## Cấu Hình Database

Mặc định server dùng LocalDB:

```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=SecureChat;Trusted_Connection=True;TrustServerCertificate=True"
```

File cấu hình:

```text
src/Server/SecureChat.Server/appsettings.json
```

Trong môi trường development, server đang bật tự migrate DB:

```json
"Database": {
  "AutoMigrate": true
}
```

Vì vậy bình thường chỉ cần chạy server, database `SecureChat` sẽ tự được tạo/cập nhật nếu chưa có. Nếu muốn xem database, mở SSMS và kết nối:

```text
Server name: (localdb)\MSSQLLocalDB
Authentication: Windows Authentication
Database: SecureChat
```

## Cấu Hình Gmail Để Gửi OTP

Server gửi OTP qua SMTP. Cần cấu hình trong:

```text
src/Server/SecureChat.Server/appsettings.json
```

Thay block `Email` thành dạng sau:

```json
"Email": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "Username": "your-email@gmail.com",
  "Password": "your-google-app-password",
  "FromEmail": "your-email@gmail.com",
  "FromName": "Secure Server"
}
```

Lưu ý quan trọng:

- Không dùng mật khẩu Gmail thường.
- Phải dùng Google App Password.
- Gmail phải bật 2-Step Verification.
- App Password thường có dạng 16 ký tự, ví dụ `abcd efgh ijkl mnop`.
- Khi dán vào `appsettings.json`, có thể bỏ khoảng trắng hoặc giữ nguyên nếu SMTP library chấp nhận. Khuyến nghị bỏ khoảng trắng:

```json
"Password": "abcdefghijklmnop"
```

### Cách Tạo Google App Password

1. Mở tài khoản Google.
2. Vào `Security`.
3. Bật `2-Step Verification` nếu chưa bật.
4. Vào `App passwords`.
5. Tạo app password cho app tùy chọn, ví dụ đặt tên `SecureChat`.
6. Copy app password và dán vào `Email:Password`.

Nếu Gmail không gửi OTP:

- Kiểm tra `Username` và `FromEmail` phải là cùng Gmail.
- Kiểm tra `Password` là App Password, không phải password Gmail.
- Kiểm tra mạng có cho kết nối SMTP port `587`.
- Kiểm tra server log xem có lỗi SMTP không.

Trong `appsettings.Development.json` có:

```json
"Email": {
  "LogOtpWhenSmtpMissing": true
}
```

Nếu chưa cấu hình SMTP, OTP có thể được log ra console server để test nhanh. Muốn gửi Gmail thật thì vẫn phải cấu hình SMTP như trên.

## TLS Và Certificate

Server dùng HTTPS. Certificate development nằm trong:

```text
src/Server/SecureChat.Server/certs/
```

Server sẽ tạo CA/cert nếu chưa có. Client verify server bằng file CA:

```text
src/Client/SecureChat.Client/App.config
```

```xml
<add key="ServerCaCertificateFile" value="..\..\..\..\..\Server\SecureChat.Server\certs\ca.cert" />
```

Nếu đổi vị trí chạy client hoặc copy exe ra chỗ khác, cần sửa đường dẫn `ServerCaCertificateFile` cho đúng tới `ca.cert`.

## Cách Chạy

### 1. Chạy Server

Trong Visual Studio:

1. Mở solution/project.
2. Set startup project là `SecureChat.Server`.
3. Run.

Hoặc dùng terminal:

```powershell
dotnet run --project src\Server\SecureChat.Server\SecureChat.Server.csproj
```

Server mặc định chạy HTTPS ở:

```text
https://localhost:7165
```

Swagger/API:

```text
https://localhost:7165/swagger
```

### 2. Chạy Client

Trong Visual Studio:

1. Set startup project là `SecureChat.Client`.
2. Run.

Hoặc terminal:

```powershell
dotnet run --project src\Client\SecureChat.Client\SecureChat.Client.csproj
```

Client kết nối server theo:

```text
src/Client/SecureChat.Client/App.config
```

```xml
<add key="ApiBaseUrl" value="https://localhost:7165" />
<add key="SignalRHubUrl" value="https://localhost:7165/hubs/chat" />
```

Nếu server chạy port khác, sửa hai dòng này.

## Luồng Sử Dụng Client

1. Mở client.
2. Chọn `Create Account`.
3. Nhập email, display name, password.
4. Bấm `Send OTP`.
5. Mở Gmail lấy OTP.
6. Nhập OTP để hoàn tất đăng ký.
7. Client sẽ tạo RSA key pair cho device.
8. Sau khi login, tìm bạn bằng nút `Find`.
9. Gửi friend request.
10. User kia mở `Requests` để accept.
11. Sau khi là friends, chọn bạn trong tab `Friends` hoặc đoạn chat trong tab `Chats` để nhắn tin.

## Test 2 User Trên Cùng 1 Máy

Có thể mở 2 client cùng lúc để test 2 user.

Khuyến nghị:

1. Chạy server trước.
2. Mở client thứ nhất, đăng nhập user A.
3. Mở client thứ hai, đăng nhập user B.
4. Nếu máy đã lưu nhiều account, Splash sẽ đưa về Login để chọn đúng account.

Dữ liệu local được tách riêng theo user:

```text
%APPDATA%\SecureChat\users\<email>_<userid>\
%LOCALAPPDATA%\SecureChat\users\<email>_<userid>\
```

Bên trong có token, plaintext message cache, file preview và temp file riêng. Điều này tránh nhầm token/message giữa 2 user khi chạy trên cùng máy.

## Các Tính Năng Chính

- Đăng ký bằng OTP Gmail.
- Login bằng device RSA key.
- TLS client verify server bằng CA cert.
- Friend request, accept/reject.
- Block user.
- Personal chat.
- Group chat.
- Add member, leave group.
- Nếu owner rời group thì group bị giải tán.
- AES-GCM cho nội dung tin nhắn.
- RSA dùng để trao đổi session key cho device.
- File/media encrypted upload/download/preview.
- Local message cache lưu plaintext phía client theo từng user.

## Xoá Dữ Liệu Để Test Lại

### Xoá toàn bộ dữ liệu server

Trong SSMS, chạy:

```sql
DELETE FROM Attachments;
DELETE FROM HiddenChats;
DELETE FROM Messages;
DELETE FROM ChatParticipants;
DELETE FROM Chats;
DELETE FROM GroupKeys;
DELETE FROM GroupMembers;
DELETE FROM Groups;
DELETE FROM FriendRequests;
DELETE FROM Friends;
DELETE FROM BlockedUsers;
DELETE FROM SecurityLogs;
DELETE FROM OtpRequests;
DELETE FROM RefreshTokens;
DELETE FROM PublicKeys;
DELETE FROM Devices;
DELETE FROM Users;
```

### Xoá toàn bộ dữ liệu client

Đóng client trước, sau đó xoá:

```text
%APPDATA%\SecureChat
%LOCALAPPDATA%\SecureChat
```

### Xoá file upload server

Xoá toàn bộ file trong:

```text
src/Server/SecureChat.Server/uploads
```

## Xoá Dữ Liệu Của 1 User

Trong SSMS, đổi email ở dòng đầu rồi chạy:

```sql
DECLARE @Email nvarchar(256) = N'user@example.com';
DECLARE @UserId uniqueidentifier;

SELECT @UserId = Id
FROM Users
WHERE NormalizedEmail = UPPER(LTRIM(RTRIM(@Email)))
   OR Email = LTRIM(RTRIM(@Email));

IF @UserId IS NULL
BEGIN
    PRINT 'User not found';
    RETURN;
END;

DECLARE @ChatIds TABLE (Id uniqueidentifier);
INSERT INTO @ChatIds
SELECT DISTINCT ChatId FROM ChatParticipants WHERE UserId = @UserId;

DELETE FROM Attachments WHERE MessageId IN (SELECT Id FROM Messages WHERE ChatId IN (SELECT Id FROM @ChatIds));
DELETE FROM HiddenChats WHERE UserId = @UserId OR ChatId IN (SELECT Id FROM @ChatIds);
DELETE FROM Messages WHERE ChatId IN (SELECT Id FROM @ChatIds);
DELETE FROM ChatParticipants WHERE ChatId IN (SELECT Id FROM @ChatIds);
DELETE FROM Chats WHERE Id IN (SELECT Id FROM @ChatIds);

DELETE FROM GroupKeys WHERE GroupId IN (SELECT GroupId FROM GroupMembers WHERE UserId = @UserId);
DELETE FROM GroupMembers WHERE UserId = @UserId;
DELETE FROM Groups WHERE CreatorId = @UserId;

DELETE FROM FriendRequests WHERE FromUserId = @UserId OR ToUserId = @UserId;
DELETE FROM Friends WHERE UserId = @UserId OR FriendUserId = @UserId;
DELETE FROM BlockedUsers WHERE UserId = @UserId OR BlockedUserId = @UserId;

DELETE FROM SecurityLogs WHERE UserId = @UserId;
DELETE FROM OtpRequests WHERE Email = @Email OR NormalizedEmail = UPPER(LTRIM(RTRIM(@Email)));
DELETE FROM RefreshTokens WHERE UserId = @UserId;
DELETE FROM PublicKeys WHERE DeviceId IN (SELECT Id FROM Devices WHERE UserId = @UserId);
DELETE FROM Devices WHERE UserId = @UserId;
DELETE FROM Users WHERE Id = @UserId;
```

Phía client, xoá folder user tương ứng trong:

```text
%APPDATA%\SecureChat\users\
%LOCALAPPDATA%\SecureChat\users\
```

## Lỗi Thường Gặp

### Không nhận được OTP Gmail

- Kiểm tra đã dùng Google App Password.
- Kiểm tra `Host = smtp.gmail.com`, `Port = 587`.
- Kiểm tra `Username`/`FromEmail` là Gmail thật.
- Xem log server để biết SMTP lỗi gì.

### Client báo lỗi certificate

- Kiểm tra file `ca.cert` tồn tại trong `src/Server/SecureChat.Server/certs`.
- Kiểm tra `ServerCaCertificateFile` trong `App.config`.
- Chạy server trước để server tạo cert nếu chưa có.

### Không thấy friend/group/message mới

- Đảm bảo server vẫn đang chạy.
- Đảm bảo cả hai client đã login đúng account.
- Tắt client cũ và mở lại sau khi thay đổi code.
- Với 2 client trên cùng máy, chọn đúng account ở màn Login.

### Database không thấy trong SSMS

Kết nối đúng LocalDB:

```text
(localdb)\MSSQLLocalDB
```

Database tên:

```text
SecureChat
```

Nếu chưa thấy database, chạy server một lần để auto migrate tạo DB.
