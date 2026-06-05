#nullable enable

using System.Text.Json;
using SecureChat.Client.Models;

namespace SecureChat.Client.Services.Content;

/// <summary>
/// Provides local UI-only offensive language masking.
/// </summary>
public static class OffensiveLanguageFilter
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SecureChat",
        "AppSettings.json");

    /// <summary>Applies masking if the user enabled offensive language filtering.</summary>
    public static string Apply(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || !IsEnabled())
        {
            return text;
        }

        return LocalNlpModerationModel.MaskOffensiveTerms(text);
    }

    /// <summary>Gets a value indicating whether offensive language masking is enabled.</summary>
    public static bool IsEnabled()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return false;
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath));
            return settings?.MaskOffensiveLanguage == true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return false;
        }
    }
}
