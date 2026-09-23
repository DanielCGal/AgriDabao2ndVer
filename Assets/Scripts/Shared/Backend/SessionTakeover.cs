using UnityEngine;
using UnityEngine.SceneManagement;

namespace AgriDabao3D
{
    /// <summary>
    /// Ends this device's play session when another device takes the account.
    ///
    /// The farm heartbeat is what notices: it claims the account every fifteen
    /// seconds, and the server refuses with 409 once a different device holds it.
    /// That only happens if this phone went quiet for longer than the online
    /// window - backgrounded, locked, or out of signal - long enough for the
    /// other phone to claim it legitimately.
    ///
    /// The farm is deliberately NOT saved on the way out. The other device owns
    /// the account now, so a save here would be rejected by the revision check
    /// anyway, and trying would only replace one clear explanation with two
    /// confusing ones. Anything since the last save is lost, which is the honest
    /// outcome: this device stopped being the one playing some time ago.
    /// </summary>
    public static class SessionTakeover
    {
        private const string MainMenuScene = "MainMenu";

        private static bool pending;
        private static string message;

        /// <summary>
        /// Leaves the farm at once and arranges for the menu to explain why.
        /// Safe to call more than once; only the first call does anything.
        /// </summary>
        public static void Raise(string reason)
        {
            if (pending)
                return;

            pending = true;
            message = reason;

            Debug.LogWarning("[Session] The account was claimed by another device; leaving the farm.");

            // Dropped so the farm being abandoned cannot be restored into the next
            // scene load, and so a later Start fetches the account fresh from the
            // server rather than trusting what this device had in memory.
            FarmLoadContext.Clear();

            SceneManager.LoadScene(MainMenuScene);
        }

        /// <summary>
        /// Leaves the farm because this device's login is no longer accepted -
        /// which today means the account's password was changed somewhere else.
        ///
        /// Unlike a takeover, the saved login is thrown away as well. The token
        /// on this phone can never work again, so keeping it would only produce a
        /// menu that looks signed in and refuses every button; clearing it sends
        /// the player to the login form, which is where they can actually fix it.
        ///
        /// The farm is not saved on the way out, for the same reason as a
        /// takeover: the server would refuse the write anyway.
        /// </summary>
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

        /// <summary>
        /// True once, for the menu to show its account-in-use prompt. Clearing on
        /// read stops the prompt reappearing every time the player returns to the
        /// menu afterwards.
        /// </summary>
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
