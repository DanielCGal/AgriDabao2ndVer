using UnityEngine;

namespace AgriDabao3D
{
    public static class SavedSession
    {
        private const string KeyToken = "session.accessToken";
        private const string KeyUser = "session.user";

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

        public static bool TryLoad(out string accessToken, out UserResponseDto user)
        {
            accessToken = PlayerPrefs.GetString(KeyToken, string.Empty);
            user = null;

            if (string.IsNullOrWhiteSpace(accessToken))
                return false;

            string json = PlayerPrefs.GetString(KeyUser, string.Empty);
            if (!string.IsNullOrWhiteSpace(json))
            {
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

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(KeyToken);
            PlayerPrefs.DeleteKey(KeyUser);
            PlayerPrefs.Save();
        }

        public static string RememberedEmail()
        {
            return TryLoad(out _, out UserResponseDto user) && user != null
                ? user.email
                : string.Empty;
        }
    }
}
