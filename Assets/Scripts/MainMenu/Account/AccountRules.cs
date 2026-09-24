namespace AgriDabao3D
{
    public static class AccountRules
    {
        public const int EmailMax = 320;
        public const int PasswordMin = 8;
        public const int PasswordMax = 72;
        public const int DisplayNameMin = 3;
        public const int DisplayNameMax = 80;

        public static string SignupProblem(string email, string password,
            string confirmPassword, string displayName)
        {
            displayName = displayName ?? string.Empty;

            return EmailProblem(email, "Enter your email address.")
                ?? PasswordProblem(password, confirmPassword, false)
                ?? (displayName.Length > 0 ? DisplayNameProblem(displayName) : null);
        }

        public static string ResetPasswordProblem(string newPassword, string confirmPassword)
        {
            return PasswordProblem(newPassword, confirmPassword, true);
        }

        public static string ChangeEmailProblem(string newEmail)
        {
            return EmailProblem(newEmail, "Enter your new email.");
        }

        public static string ChangePasswordProblem(string currentPassword,
            string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword))
                return "Enter your current password.";

            return PasswordProblem(newPassword, confirmPassword, true);
        }

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

            if (displayName.IndexOf('@') >= 0)
                return "Your display name cannot contain the @ sign.";

            return null;
        }

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
