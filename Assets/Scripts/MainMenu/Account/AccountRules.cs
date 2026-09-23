namespace AgriDabao3D
{
    /// <summary>
    /// The account rules the server enforces, checked on the phone first so a
    /// player sees the problem the moment they press the button instead of after
    /// a round trip. There is one method per form, each named after the request
    /// it guards: sign-up, the new password at the end of a reset, and the three
    /// changes on the Account panel.
    ///
    /// The server stays the authority. These copy RegisterRequest,
    /// ResetPasswordRequest, ChangeEmailRequest, ChangePasswordRequest,
    /// ChangeDisplayNameRequest and AccountFields.validateDisplayName, and they
    /// must not turn away a player the server would accept - a client rule
    /// tighter than the server's is worse than the round trip it saves, because
    /// it locks someone out with no way round it. Where the server is loose, this
    /// is loose too. If either side changes, change both.
    ///
    /// Anything that needs the database stays with the server alone: whether an
    /// address or display name is taken or already yours, and whether a current
    /// password is the right one.
    ///
    /// Each method returns the first problem it finds, checking fields in the
    /// order they appear from top to bottom, or null when everything here passes.
    /// Callers pass emails and display names already trimmed and passwords exactly
    /// as typed, which is how each is sent.
    ///
    /// Deliberately free of any Unity type, so it can be compiled and exercised
    /// on its own.
    /// </summary>
    public static class AccountRules
    {
        public const int EmailMax = 320;
        public const int PasswordMin = 8;
        public const int PasswordMax = 72;
        public const int DisplayNameMin = 3;
        public const int DisplayNameMax = 80;

        /// <summary>Create Player Account (RegisterRequest).</summary>
        public static string SignupProblem(string email, string password,
            string confirmPassword, string displayName)
        {
            displayName = displayName ?? string.Empty;

            // Blank is fine for the display name: it is optional at sign-up, and
            // the server stores none. Only a name that was actually typed has to
            // meet the rules.
            return EmailProblem(email, "Enter your email address.")
                ?? PasswordProblem(password, confirmPassword, false)
                ?? (displayName.Length > 0 ? DisplayNameProblem(displayName) : null);
        }

        /// <summary>The new password at the end of a reset (ResetPasswordRequest).</summary>
        public static string ResetPasswordProblem(string newPassword, string confirmPassword)
        {
            return PasswordProblem(newPassword, confirmPassword, true);
        }

        /// <summary>Change Email on the Account panel (ChangeEmailRequest).</summary>
        public static string ChangeEmailProblem(string newEmail)
        {
            return EmailProblem(newEmail, "Enter your new email.");
        }

        /// <summary>
        /// Change Password on the Account panel (ChangePasswordRequest). The
        /// current password only has to be filled in: its length is not checked,
        /// because the server does not check it either.
        /// </summary>
        public static string ChangePasswordProblem(string currentPassword,
            string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword))
                return "Enter your current password.";

            return PasswordProblem(newPassword, confirmPassword, true);
        }

        /// <summary>
        /// Change Display Name on the Account panel (ChangeDisplayNameRequest).
        /// Unlike at sign-up, a blank is refused: this request exists to set a
        /// name, and the server will not take an empty one.
        /// </summary>
        public static string ChangeDisplayNameProblem(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return "Enter your new display name.";

            return DisplayNameProblem(displayName);
        }

        private static string EmailProblem(string email, string blankMessage)
        {
            email = email ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email))
                return blankMessage;

            if (email.Length > EmailMax)
                return "That email address is too long.";

            if (!LooksLikeEmail(email))
                return "Enter a valid email address, like name@gmail.com.";

            return null;
        }

        /// <summary>
        /// A password and its confirmation. On the forms that replace a password
        /// the messages say "new password", so a player looking at a current and a
        /// new password box is never left guessing which one a message means.
        /// </summary>
        private static string PasswordProblem(string password, string confirmPassword, bool isNew)
        {
            password = password ?? string.Empty;
            confirmPassword = confirmPassword ?? string.Empty;
            string which = isNew ? "new password" : "password";

            if (string.IsNullOrWhiteSpace(password))
                return "Enter a " + which + ".";

            if (password.Length < PasswordMin)
                return "Your " + which + " needs at least " + PasswordMin + " characters.";

            if (password.Length > PasswordMax)
                return "Your " + which + " can be at most " + PasswordMax + " characters.";

            if (password != confirmPassword)
                return (isNew ? "New password" : "Password") + " and confirmation do not match.";

            return null;
        }

        private static string DisplayNameProblem(string displayName)
        {
            if (displayName.Length < DisplayNameMin)
                return "Your display name needs at least " + DisplayNameMin + " characters.";

            if (displayName.Length > DisplayNameMax)
                return "Your display name can be at most " + DisplayNameMax + " characters.";

            // Sign-in tries typed text as an address before a display name, so
            // a name containing @ could never be signed in with.
            if (displayName.IndexOf('@') >= 0)
                return "Your display name cannot contain the @ sign.";

            return null;
        }

        /// <summary>
        /// Something before an @, something after it, and no spaces.
        ///
        /// That catches the mistakes people actually make - no @ at all, a stray
        /// space, or a display name typed into the email box - without judging the
        /// domain, which the server does not do either: "name@localhost" is valid
        /// to it, so it is valid here.
        ///
        /// The one place this is stricter is a quoted local part containing a
        /// space, such as "john doe"@example.com, which the server's validator
        /// technically allows. No real mailbox a player would sign up with looks
        /// like that, and rejecting spaces catches a far more common typo.
        /// </summary>
        public static bool LooksLikeEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            for (int i = 0; i < email.Length; i++)
            {
                if (char.IsWhiteSpace(email[i]))
                    return false;
            }

            int at = email.LastIndexOf('@');
            return at > 0 && at < email.Length - 1;
        }
    }
}
