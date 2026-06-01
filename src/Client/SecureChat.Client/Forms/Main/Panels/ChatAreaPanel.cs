#nullable enable

using System.Text;
using System.Text.Json;
using SecureChat.Client.Forms.Controls;
using SecureChat.Client.Forms.Helpers;
using SecureChat.Client.Forms.Main.Controls;
using SecureChat.Client.Models;
using SecureChat.Client.Services;
using SecureChat.Client.Services.Api;
using SecureChat.Client.Services.Crypto;
using SecureChat.Client.Services.SignalR;
using SecureChat.Client.Services.Storage;
using SecureChat.Shared.DTOs.Chat;
using SecureChat.Shared.DTOs.File;
using SecureChat.Shared.DTOs.Message;
using SecureChat.Shared.Enums;

namespace SecureChat.Client.Forms.Main.Panels;

/// <summary>
/// Displays the active chat, messages, typing indicator, and message composer.
/// </summary>
public class ChatAreaPanel : UserControl
{
    private static readonly TimeSpan KeyRotationInterval = TimeSpan.FromMinutes(15);
    private readonly ChatApiService _chatApiService;
    private readonly UserApiService _userApiService;
    private readonly ISignalRService _signalRService;
    private readonly AesGcmService _aesGcmService;
    private readonly RsaService _rsaService;
    private readonly KeyStorageService _keyStorageService;
    private readonly AuthSessionService _authSession;
    private readonly UserDataPathService _userDataPathService;
    private readonly SessionCache _sessionCache;
    private readonly LocalMessageStore _localMessageStore;
    private readonly FileApiService _fileApiService;
    private readonly FlowLayoutPanel _messagesPanel;
    private readonly Panel _headerPanel;
    private readonly Label _nameLabel;
    private readonly Label _statusLabel;
    private readonly Label _typingLabel;
    private readonly RoundedTextBox _messageTextBox;
    private readonly Button _emojiButton;
    private readonly Button _attachButton;
    private readonly RoundedButton _sendButton;
    private readonly System.Windows.Forms.Timer _typingTimer;
    private readonly System.Windows.Forms.Timer _keyRotationTimer;
    private readonly HashSet<Guid> _renderedMessageIds = new();
    private ChatSummaryDto? _activeChat;
    private Guid _currentUserId;
    private int _loadVersion;
    private bool _isSending;
    private int _typingDotCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatAreaPanel"/> class.
    /// </summary>
    public ChatAreaPanel(
        ChatApiService chatApiService,
        UserApiService userApiService,
        ISignalRService signalRService,
        AesGcmService aesGcmService,
        FileApiService fileApiService,
        RsaService rsaService,
        KeyStorageService keyStorageService,
        AuthSessionService authSession,
        UserDataPathService userDataPathService,
        SessionCache sessionCache,
        LocalMessageStore localMessageStore)
    {
        _chatApiService = chatApiService;
        _userApiService = userApiService;
        _signalRService = signalRService;
        _aesGcmService = aesGcmService;
        _fileApiService = fileApiService;
        _rsaService = rsaService;
        _keyStorageService = keyStorageService;
        _authSession = authSession;
        _userDataPathService = userDataPathService;
        _sessionCache = sessionCache;
        _localMessageStore = localMessageStore;
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        BackColor = ThemeHelper.BgPrimary;

        _messagesPanel = new FlowLayoutPanel();
        _headerPanel = new Panel();
        _nameLabel = new Label();
        _statusLabel = new Label();
        _typingLabel = new Label();
        _messageTextBox = new RoundedTextBox();
        _emojiButton = new Button();
        _attachButton = new Button();
        _sendButton = new RoundedButton();
        _typingTimer = new System.Windows.Forms.Timer { Interval = 450 };
        _typingTimer.Tick += TypingTimerTick;
        _keyRotationTimer = new System.Windows.Forms.Timer { Interval = (int)KeyRotationInterval.TotalMilliseconds };
        _keyRotationTimer.Tick += (_, _) =>
        {
            if (_activeChat is not null)
            {
                _sessionCache.RemoveSessionKey(_activeChat.Id.ToString());
            }
        };
        BuildControls();
        _keyRotationTimer.Start();
    }

    /// <summary>Sets the current user id used to identify outgoing messages.</summary>
    public void SetCurrentUser(Guid userId)
    {
        _currentUserId = userId;
    }

    /// <summary>Occurs after the current user sends a message.</summary>
    public event EventHandler<MessageDto>? MessageSent;

    /// <summary>Clears the active chat display.</summary>
    public void ClearChat()
    {
        _loadVersion++;
        _activeChat = null;
        _renderedMessageIds.Clear();
        _messagesPanel.Controls.Clear();
        _nameLabel.Text = "Select a chat";
        _statusLabel.Text = string.Empty;
        SetComposerEnabled(false, "Select a chat");
    }

    /// <summary>Loads a chat and renders messages.</summary>
    public async Task LoadChatAsync(ChatSummaryDto chat)
    {
        var version = ++_loadVersion;
        _activeChat = chat;
        _nameLabel.Text = chat.Name;
        _statusLabel.Text = chat.IsGroup ? "Group chat" : "Online";
        _renderedMessageIds.Clear();
        _messagesPanel.Controls.Clear();
        SetComposerEnabled(true, "Message");
        await RefreshHeaderStatusAsync(chat);
        if (version != _loadVersion || _activeChat?.Id != chat.Id)
        {
            return;
        }

        List<MessageDto> messages;
        try
        {
            messages = await _chatApiService.GetMessagesAsync(chat.Id, 1);
        }
        catch (Exception ex)
        {
            SetComposerEnabled(false, "This chat is blocked");
            _statusLabel.Text = "Blocked";
            MessageBox.Show(this, ex.Message, "Chat unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var rendered = await Task.Run(() => messages.OrderBy(message => message.SentAt).ToList());
        _messagesPanel.SuspendLayout();
        foreach (var message in rendered)
        {
            await AddMessageAsync(message, message.SenderId == _currentUserId, version);
        }

        _messagesPanel.ResumeLayout(true);
        ScrollToBottom();
    }

    /// <summary>Adds a received message to the panel.</summary>
    public async Task AddReceivedMessageAsync(MessageDto message)
    {
        if (_activeChat is null || message.ChatId != _activeChat.Id)
        {
            return;
        }

        await AddMessageAsync(message, message.SenderId == _currentUserId);
        ScrollToBottom();
    }

    /// <summary>Decrypts a message for surfaces outside the active conversation, such as chat previews.</summary>
    public async Task<string> GetPlaintextPreviewAsync(MessageDto message)
    {
        var plaintext = await TryDecryptMessageAsync(message);
        return string.IsNullOrWhiteSpace(plaintext) && message.Attachments.Count > 0
            ? $"File: {message.Attachments.First().FileName}"
            : plaintext;
    }

    /// <summary>Shows all files shared in the active chat.</summary>
    public async Task ShowSharedFilesAsync()
    {
        if (_activeChat is null)
        {
            return;
        }

        var messages = new List<MessageDto>();
        for (var page = 1; page <= 20; page++)
        {
            var batch = await _chatApiService.GetMessagesAsync(_activeChat.Id, page);
            if (batch.Count == 0)
            {
                break;
            }

            messages.AddRange(batch);
            if (batch.Count < 50)
            {
                break;
            }
        }

        var attachments = messages
            .SelectMany(message => message.Attachments.Select(attachment => (message.ChatId, Attachment: attachment)))
            .ToList();
        if (attachments.Count == 0)
        {
            MessageBox.Show(this, "No shared files in this chat.", "Shared files", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var form = new Form
        {
            Text = "Shared files",
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(560, 420),
            BackColor = ThemeHelper.BgPrimary,
            ForeColor = ThemeHelper.TextPrimary
        };
        var listBox = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.BgCard,
            ForeColor = ThemeHelper.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle
        };
        foreach (var item in attachments)
        {
            listBox.Items.Add($"{item.Attachment.FileName}   {FormatFileSize(item.Attachment.FileSize)}   {item.Attachment.MimeType}");
        }

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8)
        };
        var previewButton = CreateDialogButton("Preview");
        var downloadButton = CreateDialogButton("Download");
        previewButton.Click += async (_, _) =>
        {
            if (listBox.SelectedIndex >= 0)
            {
                var item = attachments[listBox.SelectedIndex];
                await PreviewAttachmentAsync(item.ChatId, item.Attachment);
            }
        };
        downloadButton.Click += async (_, _) =>
        {
            if (listBox.SelectedIndex >= 0)
            {
                var item = attachments[listBox.SelectedIndex];
                await SaveAttachmentAsync(item.ChatId, item.Attachment);
            }
        };
        buttonPanel.Controls.Add(previewButton);
        buttonPanel.Controls.Add(downloadButton);
        form.Controls.Add(listBox);
        form.Controls.Add(buttonPanel);
        form.ShowDialog(this);
    }

    /// <summary>Refreshes the active chat header status from the server.</summary>
    public async Task RefreshActiveChatStatusAsync()
    {
        if (_activeChat is not null)
        {
            await RefreshHeaderStatusAsync(_activeChat);
        }
    }

    /// <summary>Shows a typing indicator.</summary>
    public void ShowTyping(string displayName, bool isTyping)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => ShowTyping(displayName, isTyping));
            return;
        }

        _typingLabel.Tag = displayName;
        _typingLabel.Visible = isTyping;
        if (isTyping)
        {
            _typingTimer.Start();
        }
        else
        {
            _typingTimer.Stop();
        }
    }

    private async Task AddMessageAsync(MessageDto message, bool isMine, int? loadVersion = null)
    {
        var activeChatId = _activeChat?.Id;
        if (activeChatId is null || message.ChatId != activeChatId.Value)
        {
            return;
        }

        if (_renderedMessageIds.Contains(message.Id))
        {
            return;
        }

        var plaintext = await TryDecryptMessageAsync(message);
        if ((loadVersion is not null && loadVersion.Value != _loadVersion) || _activeChat?.Id != message.ChatId)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(async () => await AddMessageAsync(message, isMine, loadVersion));
            return;
        }

        if (_renderedMessageIds.Contains(message.Id))
        {
            return;
        }

        if (_messagesPanel.Controls.Count >= 150)
        {
            if (_messagesPanel.Controls[0] is MessageBubbleControl removed && removed.Message is not null)
            {
                _renderedMessageIds.Remove(removed.Message.Id);
            }

            _messagesPanel.Controls.RemoveAt(0);
        }

        var bubble = new MessageBubbleControl
        {
            Width = GetMessageRowWidth(),
            Message = message,
            PlainText = plaintext,
            IsMine = isMine,
            IsGroup = _activeChat?.IsGroup ?? false,
            Margin = new Padding(0, 2, 0, 2)
        };
        bubble.AttachmentClicked += async (_, attachment) => await PreviewAttachmentAsync(message.ChatId, attachment);
        _messagesPanel.Controls.Add(bubble);
        _renderedMessageIds.Add(message.Id);

        var insertIndex = _messagesPanel.Controls
            .OfType<MessageBubbleControl>()
            .Where(control => control != bubble && control.Message is not null)
            .Select((control, index) => new { Control = control, Index = index })
            .FirstOrDefault(item => item.Control.Message!.SentAt > message.SentAt)?.Index;
        if (insertIndex is not null)
        {
            _messagesPanel.Controls.SetChildIndex(bubble, insertIndex.Value);
        }
    }

    private async Task PreviewAttachmentAsync(Guid chatId, AttachmentDto attachment)
    {
        try
        {
            var key = await GetSessionKeyForDecryptAsync(chatId);
            if (key is null)
            {
                MessageBox.Show(this, "Session key is not available on this device.", "SecureChat", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var encryptedPath = await _fileApiService.DownloadEncryptedFileToTempAsync(attachment.Id);
            var previewPath = await DecryptAttachmentToLocalCacheAsync(encryptedPath, key, attachment);

            if (attachment.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                using var preview = new Form
                {
                    Text = attachment.FileName,
                    StartPosition = FormStartPosition.CenterParent,
                    Size = new Size(720, 520)
                };
                var picture = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Image = Image.FromFile(previewPath)
                };
                preview.Controls.Add(picture);
                preview.ShowDialog(this);
                picture.Image?.Dispose();
                return;
            }

            var result = MessageBox.Show(this, $"{attachment.FileName}\n{FormatFileSize(attachment.FileSize)}\n\nOpen preview file now?", "Attachment preview", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result == DialogResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(previewPath) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Attachment preview failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task SaveAttachmentAsync(Guid chatId, AttachmentDto attachment)
    {
        var key = await GetSessionKeyForDecryptAsync(chatId);
        if (key is null)
        {
            MessageBox.Show(this, "Session key is not available on this device.", "SecureChat", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            FileName = attachment.FileName,
            Filter = "All files|*.*"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var encryptedPath = await _fileApiService.DownloadEncryptedFileToTempAsync(attachment.Id);
        var plaintextPath = await DecryptAttachmentToLocalCacheAsync(encryptedPath, key, attachment);
        File.Copy(plaintextPath, dialog.FileName, overwrite: true);
        MessageBox.Show(this, "File downloaded.", "Shared files", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task<string> TryDecryptMessageAsync(MessageDto message)
    {
        try
        {
            var cached = await _localMessageStore.LoadPlaintextAsync(message.Id);
            if (cached is not null)
            {
                return cached;
            }

            var key = await GetSessionKeyForDecryptAsync(message.ChatId);
            if (key is null)
            {
                return "[Encrypted message: session key not available on this device]";
            }

            if (string.IsNullOrWhiteSpace(message.EncryptedContent))
            {
                return string.Empty;
            }

            return await Task.Run(async () =>
            {
                var data = new EncryptedData(
                    Convert.FromBase64String(message.IV),
                    Convert.FromBase64String(message.EncryptedContent),
                    Convert.FromBase64String(message.AuthTag));
                var plaintext = await _aesGcmService.DecryptStringAsync(data, key);
                await _localMessageStore.SavePlaintextAsync(message.Id, plaintext);
                return plaintext;
            });
        }
        catch
        {
            return "[Encrypted message: unable to decrypt]";
        }
    }

    private async void SendButtonClick(object? sender, EventArgs e)
    {
        await SendCurrentMessageAsync();
    }

    private async Task SendCurrentMessageAsync()
    {
        await SendCurrentMessageAsync(Array.Empty<Guid>());
    }

    private async Task SendCurrentMessageAsync(IReadOnlyCollection<Guid> attachmentIds)
    {
        await SendCurrentMessageAsync(attachmentIds, null);
    }

    private async Task SendCurrentMessageAsync(IReadOnlyCollection<Guid> attachmentIds, string? explicitText)
    {
        if (_isSending)
        {
            return;
        }

        if (_activeChat is null || (string.IsNullOrWhiteSpace(explicitText ?? _messageTextBox.Text) && attachmentIds.Count == 0))
        {
            return;
        }

        var text = explicitText is null
            ? (string.IsNullOrWhiteSpace(_messageTextBox.Text) ? string.Empty : _messageTextBox.Text.Trim())
            : explicitText;
        _messageTextBox.Text = string.Empty;
        _isSending = true;
        SetComposerEnabled(false, "Sending...", clearText: false);
        try
        {
            var key = await GetOrCreateSessionKeyForSendingAsync(_activeChat.Id);
            await PublishSessionKeyAsync(_activeChat.Id, key);
            var encrypted = await _aesGcmService.EncryptStringAsync(text, key);
            var dto = new SendMessageDto
            {
                ChatId = _activeChat.Id,
                EncryptedContent = Convert.ToBase64String(encrypted.Ciphertext),
                IV = Convert.ToBase64String(encrypted.IV),
                AuthTag = Convert.ToBase64String(encrypted.AuthTag),
                MessageType = MessageType.Text,
                EncryptedSessionKey = string.Empty,
                AttachmentIds = attachmentIds
            };

            var persisted = await _chatApiService.SendMessageAsync(dto);

            await _localMessageStore.SavePlaintextAsync(persisted.Id, text);
            await AddMessageAsync(persisted, true);
            ScrollToBottom();
            MessageSent?.Invoke(this, persisted);
        }
        catch (Exception ex)
        {
            if (attachmentIds.Count == 0 && explicitText is null)
            {
                _messageTextBox.Text = text;
            }

            MessageBox.Show(this, ex.Message, "Message not sent", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _isSending = false;
            if (_activeChat is not null)
            {
                SetComposerEnabled(true, "Message", clearText: false);
            }
        }
    }

    private async Task<byte[]?> GetSessionKeyForDecryptAsync(Guid chatId)
    {
        var key = _sessionCache.GetSessionKey(chatId.ToString());
        if (key is not null)
        {
            return key;
        }

        var currentAccount = await GetCurrentAccountAsync();
        if (currentAccount is null)
        {
            return null;
        }

        var sessionKey = await _chatApiService.GetSessionKeyAsync(chatId, currentAccount.DeviceId);
        var privateKeyPem = await _keyStorageService.LoadPrivateKeyAsync(currentAccount.DeviceId);
        if (sessionKey is null || string.IsNullOrWhiteSpace(privateKeyPem))
        {
            return null;
        }

        var encryptedSessionKey = Convert.FromBase64String(sessionKey.EncryptedSessionKey);
        key = await _rsaService.DecryptWithPrivateKeyAsync(encryptedSessionKey, privateKeyPem);
        _sessionCache.SetSessionKey(chatId.ToString(), key);
        return key;
    }

    private async Task<byte[]> GetOrCreateSessionKeyForSendingAsync(Guid chatId)
    {
        var existing = await GetSessionKeyForDecryptAsync(chatId);
        if (existing is not null)
        {
            return existing;
        }

        var key = await _aesGcmService.GenerateKeyAsync();
        _sessionCache.SetSessionKey(chatId.ToString(), key);
        await PublishSessionKeyAsync(chatId, key);
        return key;
    }

    /// <summary>Forces the active chat to fetch or exchange a fresh session key on the next message operation.</summary>
    public void ResetActiveSessionKey()
    {
        if (_activeChat is not null)
        {
            _sessionCache.RemoveSessionKey(_activeChat.Id.ToString());
        }
    }

    private async Task<LocalUser?> GetCurrentAccountAsync()
    {
        if (_authSession.CurrentAccount is not null)
        {
            return _authSession.CurrentAccount;
        }

        var accounts = await _keyStorageService.LoadAccountsAsync();
        var tokens = _authSession.GetTokens();
        var deviceId = ExtractClaim(tokens.access, "deviceId");
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            var currentAccount = accounts.FirstOrDefault(account => string.Equals(account.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
            if (currentAccount is not null)
            {
                return currentAccount;
            }
        }

        return accounts.OrderByDescending(account => account.LastLoginAt).FirstOrDefault();
    }

    private static string? ExtractClaim(string? accessToken, string claimName)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        var parts = accessToken.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty(claimName, out var value) ? value.GetString() : null;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return null;
        }
    }

    private async Task PublishSessionKeyAsync(Guid chatId, byte[] key)
    {
        var chat = await _chatApiService.GetChatAsync(chatId);
        foreach (var member in chat.Members)
        {
            var publicKeys = await _userApiService.GetPublicKeysAsync(member.Id);
            foreach (var publicKey in publicKeys)
            {
                var encryptedKey = await _rsaService.EncryptWithPublicKeyAsync(key, publicKey.PublicKeyPem);
                await _chatApiService.ExchangeSessionKeyAsync(chatId, new SessionKeyDto
                {
                    ChatId = chatId,
                    DeviceId = publicKey.DeviceId,
                    EncryptedSessionKey = Convert.ToBase64String(encryptedKey),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }
    }

    private async void MessageTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && !e.Shift)
        {
            e.SuppressKeyPress = true;
            await SendCurrentMessageAsync();
        }
    }

    private async void AttachButtonClick(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog();
        dialog.Title = "Select file or media";
        dialog.Filter = "Media and files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.mp4;*.mov;*.mp3;*.wav;*.pdf;*.txt;*.docx;*.xlsx;*.*";
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (_activeChat is null)
        {
            return;
        }

        var key = await GetOrCreateSessionKeyForSendingAsync(_activeChat.Id);
        await PublishSessionKeyAsync(_activeChat.Id, key);
        var encrypted = await _aesGcmService.EncryptFileAsync(dialog.FileName, key);
        var fileInfo = new FileInfo(dialog.FileName);
        var request = new UploadFileRequestDto
        {
            FileName = fileInfo.Name,
            FileSize = fileInfo.Length,
            MimeType = GetMimeType(fileInfo.Extension),
            EncryptedFileKey = string.Empty,
            IV = Convert.ToBase64String(encrypted.iv),
            AuthTag = Convert.ToBase64String(encrypted.authTag)
        };

        var upload = await _fileApiService.InitiateUploadAsync(request);
        await _fileApiService.UploadEncryptedFileAsync(upload.AttachmentId, encrypted.outputPath);
        await SendCurrentMessageAsync(new[] { upload.AttachmentId }, string.Empty);
    }

    private void TypingTimerTick(object? sender, EventArgs e)
    {
        _typingDotCount = (_typingDotCount + 1) % 4;
        var name = _typingLabel.Tag as string ?? "Someone";
        _typingLabel.Text = $"{name} is typing{new string('.', _typingDotCount)}";
    }

    private void ScrollToBottom()
    {
        if (_messagesPanel.InvokeRequired)
        {
            _messagesPanel.BeginInvoke(ScrollToBottom);
            return;
        }

        if (_messagesPanel.Controls.Count > 0)
        {
            _messagesPanel.ScrollControlIntoView(_messagesPanel.Controls[^1]);
        }
    }

    private void BuildControls()
    {
        _headerPanel.Dock = DockStyle.Top;
        _headerPanel.Height = 60;
        _headerPanel.BackColor = ThemeHelper.BgPrimary;
        _headerPanel.Paint += HeaderPaint;

        _nameLabel.SetBounds(66, 10, 260, 24);
        _nameLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        _nameLabel.ForeColor = ThemeHelper.TextPrimary;
        _nameLabel.Text = "Select a chat";

        _statusLabel.SetBounds(66, 34, 260, 18);
        _statusLabel.ForeColor = ThemeHelper.TextMuted;
        _statusLabel.Font = new Font("Segoe UI", 8.5F);

        var lockLabel = new Label
        {
            Text = "🔒",
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(Width - 78, 18),
            Size = new Size(28, 24),
            ForeColor = ThemeHelper.AccentGreen
        };

        var moreButton = new Button
        {
            Text = "⋮",
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(Width - 44, 14),
            Size = new Size(32, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeHelper.BgPrimary,
            ForeColor = ThemeHelper.TextSecondary
        };
        moreButton.FlatAppearance.BorderSize = 0;

        _headerPanel.Controls.Add(moreButton);
        _headerPanel.Controls.Add(lockLabel);
        _headerPanel.Controls.Add(_statusLabel);
        _headerPanel.Controls.Add(_nameLabel);

        _messagesPanel.Dock = DockStyle.Fill;
        _messagesPanel.FlowDirection = FlowDirection.TopDown;
        _messagesPanel.WrapContents = false;
        _messagesPanel.AutoScroll = true;
        _messagesPanel.BackColor = ThemeHelper.BgPrimary;
        _messagesPanel.Resize += (_, _) =>
        {
            foreach (Control control in _messagesPanel.Controls)
            {
                control.Width = GetMessageRowWidth();
            }
        };

        var inputPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 80,
            BackColor = ThemeHelper.BgSecondary
        };

        _typingLabel.Dock = DockStyle.Bottom;
        _typingLabel.Height = 22;
        _typingLabel.ForeColor = ThemeHelper.TextMuted;
        _typingLabel.Padding = new Padding(18, 0, 0, 0);
        _typingLabel.Visible = false;

        _emojiButton.SetBounds(14, 18, 36, 36);
        _emojiButton.Text = "☺";
        ConfigureIconButton(_emojiButton);
        _emojiButton.Click += EmojiButtonClick;

        _attachButton.SetBounds(56, 18, 36, 36);
        _attachButton.Text = "📎";
        ConfigureIconButton(_attachButton);
        _attachButton.Click += AttachButtonClick;

        _messageTextBox.SetBounds(100, 15, inputPanel.Width - 190, 42);
        _messageTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        _messageTextBox.PlaceholderText = "Message";
        _messageTextBox.InnerTextBox.Multiline = true;
        _messageTextBox.InnerTextBox.KeyDown += MessageTextBoxKeyDown;

        _sendButton.SetBounds(inputPanel.Width - 78, 15, 58, 42);
        _sendButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        _sendButton.Text = "➤";
        _sendButton.Click += SendButtonClick;

        inputPanel.Controls.Add(_sendButton);
        inputPanel.Controls.Add(_messageTextBox);
        inputPanel.Controls.Add(_attachButton);
        inputPanel.Controls.Add(_emojiButton);

        Controls.Add(_messagesPanel);
        Controls.Add(_typingLabel);
        Controls.Add(inputPanel);
        Controls.Add(_headerPanel);
    }

    private void SetComposerEnabled(bool enabled, string placeholder, bool clearText = true)
    {
        _messageTextBox.Enabled = enabled;
        _attachButton.Enabled = enabled;
        _emojiButton.Enabled = enabled;
        _sendButton.Enabled = enabled;
        _messageTextBox.PlaceholderText = placeholder;
        if (clearText)
        {
            _messageTextBox.Text = string.Empty;
        }
    }

    private int GetMessageRowWidth()
    {
        return Math.Max(320, _messagesPanel.ClientSize.Width - 24);
    }

    private void EmojiButtonClick(object? sender, EventArgs e)
    {
        var menu = new ContextMenuStrip
        {
            BackColor = ThemeHelper.BgCard,
            ForeColor = ThemeHelper.TextPrimary
        };
        foreach (var emoji in new[] { "😀", "😂", "😊", "😍", "😢", "😭", "😡", "👍", "❤️", "🙏" })
        {
            var item = new ToolStripMenuItem(emoji);
            item.Click += (_, _) =>
            {
                _messageTextBox.Text += emoji;
                _messageTextBox.Focus();
            };
            menu.Items.Add(item);
        }

        menu.Show(_emojiButton, new Point(0, -menu.PreferredSize.Height));
    }

    private void HeaderPaint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var avatar = new SolidBrush(ThemeHelper.AccentBlue);
        e.Graphics.FillEllipse(avatar, 18, 11, 38, 38);
        var initial = string.IsNullOrWhiteSpace(_nameLabel.Text) ? "S" : _nameLabel.Text[0].ToString().ToUpperInvariant();
        TextRenderer.DrawText(e.Graphics, initial, new Font("Segoe UI", 12F, FontStyle.Bold), new Rectangle(18, 11, 38, 38), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        using var pen = new Pen(ThemeHelper.Border);
        e.Graphics.DrawLine(pen, 0, 59, Width, 59);
    }

    private async Task RefreshHeaderStatusAsync(ChatSummaryDto chat)
    {
        try
        {
            var details = await _chatApiService.GetChatAsync(chat.Id);
            if (chat.IsGroup)
            {
                _statusLabel.Text = $"{details.Members.Count} member(s)";
                return;
            }

            var other = details.Members.FirstOrDefault(member => member.Id != _currentUserId);
            if (other is not null)
            {
                _statusLabel.Text = other.IsOnline
                    ? "Online"
                    : other.LastSeen is null ? "Offline" : $"Offline, last seen {other.LastSeen.Value.LocalDateTime:g}";
            }
        }
        catch
        {
            _statusLabel.Text = chat.IsGroup ? "Group chat" : "Offline";
        }
    }

    private async Task<string> DecryptAttachmentToLocalCacheAsync(string encryptedPath, byte[] key, AttachmentDto attachment)
    {
        var localFileCachePath = _userDataPathService.GetLocalPath("Files");
        Directory.CreateDirectory(localFileCachePath);
        var safeName = string.Join("_", attachment.FileName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "file";
        }

        var cachedPath = Path.Combine(localFileCachePath, $"{attachment.Id:N}_{safeName}");
        if (File.Exists(cachedPath))
        {
            return cachedPath;
        }

        var plaintextPath = await _aesGcmService.DecryptFileAsync(
            encryptedPath,
            key,
            Convert.FromBase64String(attachment.IV),
            Convert.FromBase64String(attachment.AuthTag));
        File.Copy(plaintextPath, cachedPath, overwrite: true);
        return cachedPath;
    }

    private static void ConfigureIconButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = ThemeHelper.BgSecondary;
        button.ForeColor = ThemeHelper.TextSecondary;
        button.Cursor = Cursors.Hand;
    }

    private static string GetMimeType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }

    private static string FormatFileSize(long size)
    {
        return size >= 1024 * 1024
            ? $"{size / 1024D / 1024D:0.0} MB"
            : $"{Math.Max(1, size / 1024D):0.0} KB";
    }

    private static Button CreateDialogButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Width = 100,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeHelper.BgCard,
            ForeColor = ThemeHelper.TextPrimary,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = ThemeHelper.Border;
        return button;
    }
}
