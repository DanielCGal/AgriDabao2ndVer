using System;

namespace AgriDabao3D
{
    [Serializable]
    public class RegisterRequestDto
    {
        public string email;
        public string password;
        public string confirmPassword;
        public string displayName;
        public string dateOfBirth;
    }

    [Serializable]
    public class LoginRequestDto
    {
        public string identifier;
        public string password;
    }

    [Serializable]
    public class VerifyCodeRequestDto
    {
        public string email;
        public string code;
    }

    [Serializable]
    public class CodeRequestResponseDto
    {
        public string message;
        public long expiresInSeconds;
        public string devCode;

        public string email;
    }

    [Serializable]
    public class ForgotPasswordRequestDto
    {
        public string identifier;
    }

    [Serializable]
    public class ForgotPasswordVerifyRequestDto
    {
        public string identifier;
        public string code;
    }

    [Serializable]
    public class PasswordResetTicketResponseDto
    {
        public string resetToken;
        public long expiresInSeconds;
    }

    [Serializable]
    public class ResetPasswordRequestDto
    {
        public string resetToken;
        public string newPassword;
        public string confirmPassword;
    }

    [Serializable]
    public class ChangeEmailRequestDto
    {
        public string newEmail;
    }

    [Serializable]
    public class ConfirmEmailChangeRequestDto
    {
        public string code;
    }

    [Serializable]
    public class ChangePasswordRequestDto
    {
        public string currentPassword;
        public string newPassword;
        public string confirmPassword;
    }

    [Serializable]
    public class ChangeDisplayNameRequestDto
    {
        public string displayName;
    }

    [Serializable]
    public class UserResponseDto
    {
        public string id;
        public string email;
        public string displayName;
        public string dateOfBirth;
    }

    [Serializable]
    public class AuthResponseDto
    {
        public string accessToken;
        public string expiresAt;
        public bool hasFarm;
        public UserResponseDto user;
    }

    [Serializable]
    public class AdminEmailRequestDto
    {
        public string email;
    }

    [Serializable]
    public class AdminCapabilityDto
    {
        public bool admin;
        public bool mobileToolsEnabled;
    }

    [Serializable]
    public class AdminAccountLookupDto
    {
        public bool exists;
        public string email;
        public string displayName;
        public bool hasFarm;
        public string createdAt;
    }

    [Serializable]
    public class AdminDeleteResponseDto
    {
        public string message;
        public string email;
    }

    [Serializable]
    public class AdminCommandRequestDto
    {
        public string email;
        public string commandType;
        public string payload;
        public int amount;
        public int durationDays;
    }

    [Serializable]
    public class AdminCommandQueuedDto
    {
        public string message;
        public string email;
    }

    [Serializable]
    public class AdminCommandDto
    {
        public string id;
        public string commandType;
        public string payload;

        public int? amount;
        public int? durationDays;
    }

    [Serializable]
    public class ApiErrorDto
    {
        public string timestamp;
        public int status;
        public string error;
        public string message;
    }

    [Serializable]
    public class PresenceHeartbeatRequestDto
    {
        public string deviceId;
    }

    [Serializable]
    public class SettingsDto
    {
        public float musicVolume;
        public float sfxVolume;
        public float ambienceVolume;
        public float renderDistance;

        public float uiScale;
        public float textScale;

        public bool aiSummarization;
    }

    [Serializable]
    public class AiGenerateRequestDto
    {
        public string feature;
        public string systemInstruction;
        public System.Collections.Generic.List<string> userParts =
            new System.Collections.Generic.List<string>();
    }

    [Serializable]
    public class AiGenerateResponseDto
    {
        public bool available;
        public string text;
        public string finishReason;
        public string message;
    }

    [Serializable]
    public class FarmSaveRequestDto
    {
        public long expectedRevision;
        public int schemaVersion;
        public string generatorVersion;
        public FarmSnapshotDto snapshot;
    }

    [Serializable]
    public class FarmSaveResponseDto
    {
        public long revision;
        public int schemaVersion;
        public string generatorVersion;
        public FarmSnapshotDto snapshot;
        public string savedAt;
        public string lastLogoutAt;
    }
}
