using UnityEngine;
using UnityEngine.SceneManagement;

namespace AgriDabao3D
{
    public static class SessionTakeover
    {
        private const string MainMenuScene = "MainMenu";

        private static bool pending;
        private static string message;

        public static void Raise(string reason)
        {
            if (pending)
                return;

            pending = true;
            message = reason;

            Debug.LogWarning("[Session] The account was claimed by another device; leaving the farm.");

            FarmLoadContext.Clear();

            SceneManager.LoadScene(MainMenuScene);
        }

        public static void RaiseSignedOut(string reason)
        {
            if (pending)
                return;

            pending = true;
            message = reason;

            Debug.LogWarning("[Session] This device's login was refused; signing out.");

            AuthSession.Instance?.EndSessionLocally();
            SceneManager.LoadScene(MainMenuScene);
        }

        public static bool Consume(out string reason)
        {
            reason = message;

            if (!pending)
                return false;

            pending = false;
            message = null;
            return true;
        }
    }
}
