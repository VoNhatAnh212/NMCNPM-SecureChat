#nullable enable

using System.Drawing.Drawing2D;
using System.Security.Cryptography;
using System.Text;

namespace SecureChat.Client.Forms.Helpers;

/// <summary>
/// Provides image loading, resizing, avatar, and file icon helpers.
/// </summary>
public static class ImageHelper
{
    private static readonly Dictionary<string, Icon> FileIcons = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = SystemIcons.Information,
        [".doc"] = SystemIcons.Application,
        [".docx"] = SystemIcons.Application,
        [".xls"] = SystemIcons.Application,
        [".xlsx"] = SystemIcons.Application,
        [".ppt"] = SystemIcons.Application,
        [".pptx"] = SystemIcons.Application,
        [".zip"] = SystemIcons.Shield,
        [".rar"] = SystemIcons.Shield,
        [".7z"] = SystemIcons.Shield,
        [".mp4"] = SystemIcons.Asterisk,
        [".mov"] = SystemIcons.Asterisk,
        [".avi"] = SystemIcons.Asterisk,
        [".mp3"] = SystemIcons.Question,
        [".wav"] = SystemIcons.Question,
        [".png"] = SystemIcons.WinLogo,
        [".jpg"] = SystemIcons.WinLogo,
        [".jpeg"] = SystemIcons.WinLogo,
        [".gif"] = SystemIcons.WinLogo,
        [".txt"] = SystemIcons.Application
    };

    /// <summary>Resizes an image while preserving aspect ratio.</summary>
    public static Image ResizeImage(Image image, int maxWidth, int maxHeight)
    {
        var ratio = Math.Min((double)maxWidth / image.Width, (double)maxHeight / image.Height);
        ratio = Math.Min(1D, ratio);
        var width = Math.Max(1, (int)Math.Round(image.Width * ratio));
        var height = Math.Max(1, (int)Math.Round(image.Height * ratio));

        var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.DrawImage(image, 0, 0, width, height);
        return bitmap;
    }

    /// <summary>Creates a circular bitmap from the supplied image.</summary>
    public static Bitmap MakeCircularImage(Image image, int diameter)
    {
        var bitmap = new Bitmap(diameter, diameter);
        using var graphics = Graphics.FromImage(bitmap);
        using var path = new GraphicsPath();
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        path.AddEllipse(0, 0, diameter, diameter);
        graphics.SetClip(path);

        var resized = ResizeImage(image, diameter, diameter);
        using (resized)
        {
            var x = (diameter - resized.Width) / 2;
            var y = (diameter - resized.Height) / 2;
            graphics.DrawImage(resized, x, y, resized.Width, resized.Height);
        }

        return bitmap;
    }

    /// <summary>Safely loads an image from disk.</summary>
    public static Image? LoadImageFromFile(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            using var stream = File.OpenRead(path);
            using var image = Image.FromStream(stream);
            return new Bitmap(image);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    /// <summary>Gets a representative icon for a file extension.</summary>
    public static Icon GetFileTypeIcon(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return (Icon)SystemIcons.Application.Clone();
        }

        return FileIcons.TryGetValue(extension, out var icon)
            ? (Icon)icon.Clone()
            : (Icon)SystemIcons.Application.Clone();
    }

    /// <summary>Generates a circular initials avatar with a stable color.</summary>
    public static Bitmap GenerateAvatarFromInitials(string name, int size)
    {
        var initials = GetInitials(name);
        var color = ColorFromText(name);
        var bitmap = new Bitmap(size, size);

        using var graphics = Graphics.FromImage(bitmap);
        using var brush = new SolidBrush(color);
        using var textBrush = new SolidBrush(Color.White);
        using var font = new Font("Segoe UI", Math.Max(12, size / 3), FontStyle.Bold, GraphicsUnit.Point);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.FillEllipse(brush, 0, 0, size - 1, size - 1);
        graphics.DrawString(initials, font, textBrush, new RectangleF(0, 0, size, size), format);
        return bitmap;
    }

    private static string GetInitials(string name)
    {
        var parts = (name ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return "?";
        }

        return string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0])));
    }

    private static Color ColorFromText(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text ?? string.Empty));
        return Color.FromArgb(255, 80 + hash[0] % 120, 80 + hash[1] % 120, 80 + hash[2] % 120);
    }
}
