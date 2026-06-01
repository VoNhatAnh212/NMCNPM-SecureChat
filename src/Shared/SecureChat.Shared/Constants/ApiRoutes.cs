#nullable enable

namespace SecureChat.Shared.Constants;

/// <summary>
/// Contains route constants used by SecureChat HTTP APIs.
/// </summary>
public static class ApiRoutes
{
    /// <summary>Base API route prefix.</summary>
    public const string Base = "api";

    /// <summary>Authentication API routes.</summary>
    public static class Auth
    {
        /// <summary>Authentication route prefix.</summary>
        public const string Base = ApiRoutes.Base + "/auth";
        /// <summary>User registration endpoint.</summary>
        public const string Register = Base + "/register";
        /// <summary>OTP verification endpoint.</summary>
        public const string VerifyOtp = Base + "/verify-otp";
        /// <summary>Complete registration endpoint.</summary>
        public const string CompleteRegistration = Base + "/complete-registration";
        /// <summary>Login challenge endpoint.</summary>
        public const string Challenge = Base + "/challenge";
        /// <summary>Login challenge verification endpoint.</summary>
        public const string Verify = Base + "/verify";
        /// <summary>Token refresh endpoint.</summary>
        public const string Refresh = Base + "/refresh";
        /// <summary>Logout endpoint.</summary>
        public const string Logout = Base + "/logout";
    }

    /// <summary>User API routes.</summary>
    public static class User
    {
        /// <summary>User route prefix.</summary>
        public const string Base = ApiRoutes.Base + "/users";
        /// <summary>Current user profile endpoint.</summary>
        public const string Me = Base + "/me";
        /// <summary>User search endpoint.</summary>
        public const string Search = Base + "/search";
        /// <summary>User public profile endpoint.</summary>
        public const string Profile = Base + "/{userId:guid}";
        /// <summary>User public keys endpoint.</summary>
        public const string PublicKeys = Base + "/{userId:guid}/public-keys";
    }

    /// <summary>Chat API routes.</summary>
    public static class Chat
    {
        /// <summary>Chat route prefix.</summary>
        public const string Base = ApiRoutes.Base + "/chats";
        /// <summary>Chat detail endpoint.</summary>
        public const string ById = Base + "/{chatId:guid}";
        /// <summary>Chat members endpoint.</summary>
        public const string Members = Base + "/{chatId:guid}/members";
        /// <summary>Chat session keys endpoint.</summary>
        public const string SessionKeys = Base + "/{chatId:guid}/session-keys";
        /// <summary>Unread chat count endpoint.</summary>
        public const string Unread = Base + "/unread";
    }

    /// <summary>Message API routes.</summary>
    public static class Message
    {
        /// <summary>Message route prefix.</summary>
        public const string Base = ApiRoutes.Base + "/messages";
        /// <summary>Send message endpoint.</summary>
        public const string Send = Base;
        /// <summary>Message detail endpoint.</summary>
        public const string ById = Base + "/{messageId:guid}";
        /// <summary>Chat messages endpoint.</summary>
        public const string ByChat = ApiRoutes.Chat.Base + "/{chatId:guid}/messages";
        /// <summary>Message status endpoint.</summary>
        public const string Status = Base + "/{messageId:guid}/status";
        /// <summary>Mark message as read endpoint.</summary>
        public const string MarkRead = Base + "/{messageId:guid}/read";
    }

    /// <summary>Friend API routes.</summary>
    public static class Friend
    {
        /// <summary>Friend route prefix.</summary>
        public const string Base = ApiRoutes.Base + "/friends";
        /// <summary>Friend request endpoint.</summary>
        public const string Requests = Base + "/requests";
        /// <summary>Accept friend request endpoint.</summary>
        public const string Accept = Requests + "/{requestId:guid}/accept";
        /// <summary>Reject friend request endpoint.</summary>
        public const string Reject = Requests + "/{requestId:guid}/reject";
        /// <summary>Remove friend endpoint.</summary>
        public const string Remove = Base + "/{userId:guid}";
        /// <summary>Blocked users endpoint.</summary>
        public const string Blocked = Base + "/blocked";
        /// <summary>Block user endpoint.</summary>
        public const string Block = Base + "/{userId:guid}/block";
        /// <summary>Unblock user endpoint.</summary>
        public const string Unblock = Base + "/{userId:guid}/unblock";
    }

    /// <summary>Group API routes.</summary>
    public static class Group
    {
        /// <summary>Group route prefix.</summary>
        public const string Base = ApiRoutes.Base + "/groups";
        /// <summary>Group detail endpoint.</summary>
        public const string ById = Base + "/{groupId:guid}";
        /// <summary>Group members endpoint.</summary>
        public const string Members = Base + "/{groupId:guid}/members";
        /// <summary>Single group member endpoint.</summary>
        public const string Member = Base + "/{groupId:guid}/members/{userId:guid}";
        /// <summary>Group keys endpoint.</summary>
        public const string Keys = Base + "/{groupId:guid}/keys";
        /// <summary>Group avatar endpoint.</summary>
        public const string Avatar = Base + "/{groupId:guid}/avatar";
    }

    /// <summary>File API routes.</summary>
    public static class File
    {
        /// <summary>File route prefix.</summary>
        public const string Base = ApiRoutes.Base + "/files";
        /// <summary>Create upload session endpoint.</summary>
        public const string Upload = Base + "/upload";
        /// <summary>Attachment detail endpoint.</summary>
        public const string Attachment = Base + "/attachments/{attachmentId:guid}";
        /// <summary>Attachment download endpoint.</summary>
        public const string Download = Base + "/attachments/{attachmentId:guid}/download";
        /// <summary>Upload completion endpoint.</summary>
        public const string Complete = Base + "/attachments/{attachmentId:guid}/complete";
    }

    /// <summary>Device API routes.</summary>
    public static class Device
    {
        /// <summary>Device route prefix.</summary>
        public const string Base = ApiRoutes.Base + "/devices";
        /// <summary>Register device endpoint.</summary>
        public const string Register = Base + "/register";
        /// <summary>Device detail endpoint.</summary>
        public const string ById = Base + "/{deviceId:guid}";
        /// <summary>Revoke device endpoint.</summary>
        public const string Revoke = Base + "/{deviceId:guid}/revoke";
        /// <summary>Current device endpoint.</summary>
        public const string Current = Base + "/current";
    }
}
