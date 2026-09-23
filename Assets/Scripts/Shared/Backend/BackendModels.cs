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

    /// <summary>
    /// Sign-in credentials. <see cref="identifier"/> is whatever the player typed
    /// into the single login box - an email address or their display name - and
    /// the server works out which it is.
    /// </summary>
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

        /// <summary>
        /// The address the code was sent to, for the confirmation step to quote
        /// back. Empty on password recovery: the player has proven nothing at
        /// that point, so the server only ever names the account in masked form
        /// inside <see cref="message"/>.
        /// </summary>
        public string email;
    }

    // ------------------------------------------------------------ recovery

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

    /// <summary>
    /// Proof that the emailed reset code was accepted. Held in memory for the
    /// length of one reset and never written to the phone - it is not a login,
    /// and the account is only remembered once the reset actually completes.
    /// </summary>
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

    // ------------------------------------------------------- account panel

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

    // ------------------------------------------------- developer tools only

    /// <summary>
    /// Names an account for the admin tools. The server refuses these calls
    /// unless the signed-in account is on its own allow list, so holding this
    /// type is not itself permission to use it.
    /// </summary>
    [Serializable]
    public class AdminEmailRequestDto
    {
        public string email;
    }

    /// <summary>
    /// What the server says this account may do with the developer tools. The
    /// client never decides this for itself - the admin list and the mobile
    /// switch both live on the server, so the phone has to ask.
    /// </summary>
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

    /// <summary>
    /// An event or payment a developer is sending to one player mid-session, so
    /// an alpha tester can meet a typhoon or an outbreak inside a short sitting
    /// rather than waiting for the random roll to produce one.
    /// </summary>
    [Serializable]
    public class AdminCommandRequestDto
    {
        public string email;
        /// <summary>GRANT_MONEY, FORCE_WEATHER or FORCE_PEST_DISEASE.</summary>
        public string commandType;
        /// <summary>The weather or pest enum name; unused for money.</summary>
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

    /// <summary>One instruction the server had waiting for this player.</summary>
    [Serializable]
    public class AdminCommandDto
    {
        public string id;
        public string commandType;
        public string payload;

        /// <summary>
        /// Nullable, because the server sends null for whichever of these the
        /// command does not use - no duration on a money grant, no amount on a
        /// weather event. Declared as plain ints these threw on every fetch
        /// ("cannot convert null to System.Int32") and the whole batch was
        /// dropped, so a command arrived and was thrown away without ever being
        /// applied.
        /// </summary>
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

    /// <summary>
    /// Claims this account's play session for one device. The server answers
    /// 409 when a different device is already playing on the account.
    /// </summary>
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

        /// <summary>
        /// Interface and text size multipliers. A server that predates these
        /// columns leaves them at 0; GameSettings.ApplyFromDto reads a
        /// non-positive value as "not supplied" rather than clamping it.
        /// </summary>
        public float uiScale;
        public float textScale;

        /// <summary>
        /// When true the adviser and the climate evaluation answer briefly. A bool
        /// has no "absent" value, so an older server reports false, which is the
        /// default anyway.
        /// </summary>
        public bool aiSummarization;
    }

    /// <summary>
    /// One AI request. The prompt is still assembled in the game so it stays
    /// tunable in the Inspector, but the Gemini key and the generation settings
    /// now live on the server - the phone never holds the secret.
    /// </summary>
    [Serializable]
    public class AiGenerateRequestDto
    {
        /// <summary>ADVISOR, CLIMATE_EVALUATION, TASK_GENERATE or TASK_CHECK.</summary>
        public string feature;
        public string systemInstruction;
        public System.Collections.Generic.List<string> userParts =
            new System.Collections.Generic.List<string>();
    }

    [Serializable]
    public class AiGenerateResponseDto
    {
        /// <summary>False when the adviser is switched off server-side.</summary>
        public bool available;
        public string text;
        /// <summary>"STOP" means the model finished; anything else was cut off.</summary>
        public string finishReason;
        /// <summary>In-character text to show when <see cref="available"/> is false.</summary>
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
