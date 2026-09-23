using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// The login this phone remembers between launches.
    ///
    /// Holding the access token means a returning player lands on the menu
    /// instead of the login form. It is written when a login succeeds and erased
    /// by Logout, which is deliberately the only way to change accounts on a
    /// device - there is no "switch account" path that leaves the old one behind.
    ///
    /// PlayerPrefs is not a secure store: on Android it is an XML file in the
    /// app's private directory, readable on a rooted phone or through a backup.
    /// The token is therefore treated as something that can be lost rather than
    /// something that cannot - it expires on its own, and the server checks it on
    /// every call. Anything genuinely secret, the password included, is never
    /// written here.
    /// </summary>
    public static class SavedSession
    {
        private const string KeyToken = "session.accessToken";
        private const string KeyUser = "session.user";

        /// <summary>Remembers this login on this phone, replacing any earlier one.</summary>
        public static void Save(string accessToken, UserResponseDto user)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                Clear();
                return;
            }

            PlayerPrefs.SetString(KeyToken, accessToken);
            PlayerPrefs.SetString(KeyUser, user != null ? JsonUtility.ToJson(user) : string.Empty);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// The remembered login, if there is one. The token is returned unchecked -
        /// only the server can say whether it is still good, and AuthSession asks
        /// it in the background.
        /// </summary>
        public static bool TryLoad(out string accessToken, out UserResponseDto user)
        {
            accessToken = PlayerPrefs.GetString(KeyToken, string.Empty);
            user = null;

            if (string.IsNullOrWhiteSpace(accessToken))
                return false;

            string json = PlayerPrefs.GetString(KeyUser, string.Empty);
            if (!string.IsNullOrWhiteSpace(json))
            {
                // A half-written or older payload should cost the player the cached
                // name, not the whole session - the server sends it again anyway.
                try
                {
                    user = JsonUtility.FromJson<UserResponseDto>(json);
                }
                catch
                {
                    user = null;
                }
            }

            return true;
        }

        /// <summary>Forgets the login, so the next launch asks for one.</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(KeyToken);
            PlayerPrefs.DeleteKey(KeyUser);
            PlayerPrefs.Save();
        }

        /// <summary>The email of the remembered account, for the logout prompt.</summary>
        public static string RememberedEmail()
        {
            return TryLoad(out _, out UserResponseDto user) && user != null
                ? user.email
                : string.Empty;
        }
    }
}
