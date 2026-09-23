using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AgriDabao3D
{
    public class AuthSession : MonoBehaviour
    {
        public static AuthSession Instance { get; private set; }

        public string AccessToken { get; private set; }
        public UserResponseDto CurrentUser { get; private set; }
        public bool HasFarm { get; private set; }
        public FarmSaveResponseDto LoadedFarm { get; private set; }
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);
        public bool IsBusy { get; private set; }

        public event Action SessionChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            RestoreSavedSession();
        }

        /// <summary>
        /// Brings back the login stored on this phone, so a returning player goes
        /// straight to the menu instead of signing in again.
        ///
        /// Restored synchronously from PlayerPrefs and then checked against the
        /// server in the background. Waiting for that check first would leave the
        /// menu showing a login form for the length of a round trip and then
        /// snatch it away, so the token is trusted up front and revoked later if
        /// the server disagrees - the only cost of being wrong is one failed call.
        /// </summary>
        private void RestoreSavedSession()
        {
            if (!SavedSession.TryLoad(out string token, out UserResponseDto user))
                return;

            AccessToken = token;
            CurrentUser = user;

            // Unknown rather than false: hasFarm only ever comes back on a login
            // response, and this session did not go through one. StartGame asks the
            // server instead of reading this, so the value below is a placeholder
            // until the first farm load fills it in.
            HasFarm = false;
            SessionChanged?.Invoke();

            StartCoroutine(VerifyRestoredSession());
        }

        /// <summary>
        /// Confirms the restored token is still good. A rejected token is cleared
        /// and the player is asked to sign in; any other failure - no signal, the
        /// server down - is left alone, since throwing the session away over a
        /// dropped connection would be worse than keeping it.
        /// </summary>
        private IEnumerator VerifyRestoredSession()
        {
            yield return ApiClient.Instance.GetJson<UserResponseDto>(
                "/api/users/me",
                AccessToken,
                user =>
                {
                    CurrentUser = user;
                    SavedSession.Save(AccessToken, user);
                    StartCoroutine(LoadPlayerSettings());
                    SessionChanged?.Invoke();
                },
                (_, code) =>
                {
                    if (code == 401 || code == 403)
                    {
                        Debug.Log("[Auth] Saved login is no longer valid; signing out.");
                        ClearSession();
                        AuthUIBuilder.Instance?.ShowLogin();
                    }
                });
        }

        /// <summary>
        /// Claims this account's play session for this device.
        ///
        /// <paramref name="onBusy"/> carries the server's wording for the case
        /// where another phone is already playing on the account.
        /// </summary>
        public IEnumerator ClaimPlaySession(
            Action onSuccess,
            Action<string> onBusy,
            Action<string> onError)
        {
            if (ApiClient.Instance == null || !IsAuthenticated)
            {
                onError?.Invoke("You must be logged in to play.");
                yield break;
            }

            yield return ApiClient.Instance.PostJson<PresenceHeartbeatRequestDto, object>(
                "/api/presence/heartbeat",
                new PresenceHeartbeatRequestDto { deviceId = DeviceIdentity.Current },
                AccessToken,
                _ => onSuccess?.Invoke(),
                (message, code) =>
                {
                    if (code == 409)
                        onBusy?.Invoke(message);
                    else
                        onError?.Invoke(message);
                });
        }

        // Step 1 of sign-up: the backend validates the form and emails a code.
        public IEnumerator RequestRegister(
            RegisterRequestDto request,
            Action<CodeRequestResponseDto> onSuccess,
            Action<string> onError)
        {
            if (IsBusy)
                yield break;

            IsBusy = true;
            yield return ApiClient.Instance.PostJson<RegisterRequestDto, CodeRequestResponseDto>(
                "/api/auth/register/request",
                request,
                null,
                response =>
                {
                    IsBusy = false;
                    onSuccess?.Invoke(response);
                },
                (message, _) =>
                {
                    IsBusy = false;
                    onError?.Invoke(message);
                });
        }

        // Step 2 of sign-up: confirm the code, then the account is created and a token issued.
        public IEnumerator VerifyRegister(
            string email,
            string code,
            Action onSuccess,
            Action<string> onError)
        {
            if (IsBusy)
                yield break;

            IsBusy = true;
            yield return ApiClient.Instance.PostJson<VerifyCodeRequestDto, AuthResponseDto>(
                "/api/auth/register/verify",
                new VerifyCodeRequestDto { email = email, code = code },
                null,
                response =>
                {
                    ApplyAuth(response);
                    IsBusy = false;
                    onSuccess?.Invoke();
                },
                (message, _) =>
                {
                    IsBusy = false;
                    onError?.Invoke(message);
                });
        }

        // Step 1 of login: the backend validates credentials and emails a 2FA code.
        public IEnumerator RequestLogin(
            LoginRequestDto request,
            Action<CodeRequestResponseDto> onSuccess,
            Action<string> onError)
        {
            if (IsBusy)
                yield break;

            IsBusy = true;
            yield return ApiClient.Instance.PostJson<LoginRequestDto, CodeRequestResponseDto>(
                "/api/auth/login/request",
                request,
                null,
                response =>
                {
                    IsBusy = false;
                    onSuccess?.Invoke(response);
                },
                (message, _) =>
                {
                    IsBusy = false;
                    onError?.Invoke(message);
                });
        }

        // Step 2 of login: confirm the 2FA code, then a token is issued.
        public IEnumerator VerifyLogin(
            string email,
            string code,
            Action onSuccess,
            Action<string> onError)
        {
            if (IsBusy)
                yield break;

            IsBusy = true;
            yield return ApiClient.Instance.PostJson<VerifyCodeRequestDto, AuthResponseDto>(
                "/api/auth/login/verify",
                new VerifyCodeRequestDto { email = email, code = code },
                null,
                response =>
                {
                    ApplyAuth(response);
                    IsBusy = false;
                    onSuccess?.Invoke();
                },
                (message, _) =>
                {
                    IsBusy = false;
                    onError?.Invoke(message);
                });
        }

        // ------------------------------------------------- password recovery

        /// <summary>
        /// Step 1 of recovery: email a code to the account behind an address or
        /// a display name. Nothing is signed in and nothing is written to the
        /// phone - the account is only remembered once the reset finishes.
        /// </summary>
        public IEnumerator RequestPasswordReset(
            string identifier,
            Action<CodeRequestResponseDto> onSuccess,
            Action<string> onError)
        {
            yield return ApiClient.Instance.PostJson<ForgotPasswordRequestDto, CodeRequestResponseDto>(
                "/api/auth/password/forgot/request",
                new ForgotPasswordRequestDto { identifier = identifier },
                null,
                response => onSuccess?.Invoke(response),
                (message, _) => onError?.Invoke(message));
        }

        /// <summary>
        /// Step 2: confirm the emailed code. What comes back is a reset ticket,
        /// not a login - the API refuses it as a bearer token - so it is handed
        /// to the caller to spend on the next call rather than stored here.
        /// </summary>
        public IEnumerator VerifyPasswordReset(
            string identifier,
            string code,
            Action<PasswordResetTicketResponseDto> onSuccess,
            Action<string> onError)
        {
            yield return ApiClient.Instance.PostJson<ForgotPasswordVerifyRequestDto, PasswordResetTicketResponseDto>(
                "/api/auth/password/forgot/verify",
                new ForgotPasswordVerifyRequestDto { identifier = identifier, code = code },
                null,
                response => onSuccess?.Invoke(response),
                (message, _) => onError?.Invoke(message));
        }

        /// <summary>
        /// Step 3: set the new password. The server signs the player in as part
        /// of the same call, so this is the first moment the account is written
        /// to the phone.
        ///
        /// Deliberately does not raise IsBusy. These coroutines are started by
        /// the login board, which is an ordinary scene object, so one that is
        /// cut short by a scene change would leave the flag raised forever - and
        /// StartGame refuses to open the farm while it is. Double submits are
        /// held off by the board itself, which is the thing that gets destroyed.
        /// </summary>
        public IEnumerator CompletePasswordReset(
            string resetToken,
            string newPassword,
            string confirmPassword,
            Action onSuccess,
            Action<string> onError)
        {
            yield return ApiClient.Instance.PostJson<ResetPasswordRequestDto, AuthResponseDto>(
                "/api/auth/password/forgot/reset",
                new ResetPasswordRequestDto
                {
                    resetToken = resetToken,
                    newPassword = newPassword,
                    confirmPassword = confirmPassword
                },
                null,
                response =>
                {
                    ApplyAuth(response);
                    onSuccess?.Invoke();
                },
                (message, _) => onError?.Invoke(message));
        }

        // ---------------------------------------------------- account panel

        /// <summary>Step 1 of moving the account: email a code to the new address.</summary>
        public IEnumerator RequestEmailChange(
            string newEmail,
            Action<CodeRequestResponseDto> onSuccess,
            Action<string> onError)
        {
            yield return ApiClient.Instance.PostJson<ChangeEmailRequestDto, CodeRequestResponseDto>(
                "/api/users/me/email/request",
                new ChangeEmailRequestDto { newEmail = newEmail },
                AccessToken,
                response => onSuccess?.Invoke(response),
                (message, _) => onError?.Invoke(message));
        }

        /// <summary>
        /// Step 2: confirm the code. A fresh token comes back with the moved
        /// account, and both replace what the phone had - otherwise the saved
        /// login would keep showing the old address after the change.
        /// </summary>
        public IEnumerator ConfirmEmailChange(
            string code,
            Action onSuccess,
            Action<string> onError)
        {
            yield return ApiClient.Instance.PostJson<ConfirmEmailChangeRequestDto, AuthResponseDto>(
                "/api/users/me/email/verify",
                new ConfirmEmailChangeRequestDto { code = code },
                AccessToken,
                response =>
                {
                    ApplyAuth(response);
                    onSuccess?.Invoke();
                },
                (message, _) => onError?.Invoke(message));
        }

        /// <summary>
        /// Changes the password, which also ends every other session on the
        /// account - any phone still signed in is refused from that moment on.
        ///
        /// That includes the one making the change, so the server hands back a
        /// replacement token with the response and it is adopted here. Without
        /// adopting it the player would be signed out by their own password
        /// change on the very next request.
        /// </summary>
        public IEnumerator ChangePassword(
            string currentPassword,
            string newPassword,
            string confirmPassword,
            Action<string> onSuccess,
            Action<string> onError)
        {
            yield return ApiClient.Instance.PostJson<ChangePasswordRequestDto, AuthResponseDto>(
                "/api/users/me/password",
                new ChangePasswordRequestDto
                {
                    currentPassword = currentPassword,
                    newPassword = newPassword,
                    confirmPassword = confirmPassword
                },
                AccessToken,
                response =>
                {
                    ApplyAuth(response);
                    onSuccess?.Invoke(
                        "Your password has been changed. Any other device signed " +
                        "in to this account has been signed out.");
                },
                (message, _) => onError?.Invoke(message));
        }

        public IEnumerator ChangeDisplayName(
            string displayName,
            Action onSuccess,
            Action<string> onError)
        {
            yield return ApiClient.Instance.PostJson<ChangeDisplayNameRequestDto, UserResponseDto>(
                "/api/users/me/display-name",
                new ChangeDisplayNameRequestDto { displayName = displayName },
                AccessToken,
                user =>
                {
                    ApplyUserUpdate(user);
                    onSuccess?.Invoke();
                },
                (message, _) => onError?.Invoke(message));
        }

        // --------------------------------------------- developer tools only

        /// <summary>
        /// Asks whether an address belongs to an account, so the dev tools can
        /// name what they are about to delete before deleting it.
        /// </summary>
        public IEnumerator LookupAccount(
            string email,
            Action<AdminAccountLookupDto> onSuccess,
            Action<string> onError)
        {
            yield return ApiClient.Instance.PostJson<AdminEmailRequestDto, AdminAccountLookupDto>(
                "/api/admin/accounts/lookup",
                new AdminEmailRequestDto { email = email },
                AccessToken,
                response => onSuccess?.Invoke(response),
                (message, _) => onError?.Invoke(message));
        }

        /// <summary>
        /// Deletes an account and everything belonging to it, freeing the email
        /// for a new sign-up. Irreversible, and refused by the server unless
        /// this account is on its admin list.
        /// </summary>
        public IEnumerator DeleteAccount(
            string email,
            Action<string> onSuccess,
            Action<string> onError)
        {
            yield return ApiClient.Instance.PostJson<AdminEmailRequestDto, AdminDeleteResponseDto>(
                "/api/admin/accounts/delete",
                new AdminEmailRequestDto { email = email },
                AccessToken,
                response => onSuccess?.Invoke(
                    response != null && !string.IsNullOrWhiteSpace(response.message)
                        ? response.message
                        : "Account deleted."),
                (message, _) => onError?.Invoke(message));
        }

        /// <summary>
        /// Asks the server what this account may do with the developer tools.
        /// Answers honestly for a non-admin rather than refusing, so the game can
        /// simply not draw the button.
        /// </summary>
        public IEnumerator GetAdminCapability(
            Action<AdminCapabilityDto> onSuccess,
            Action<string> onError)
        {
            yield return ApiClient.Instance.PostJson<object, AdminCapabilityDto>(
                "/api/admin/capability",
                null,
                AccessToken,
                response => onSuccess?.Invoke(response),
                (message, _) => onError?.Invoke(message));
        }

        /// <summary>Queues an event or a payment for one online player. Admin only.</summary>
        public IEnumerator SendAdminCommand(
            AdminCommandRequestDto request,
            Action<string> onSuccess,
            Action<string> onError)
        {
            yield return ApiClient.Instance.PostJson<AdminCommandRequestDto, AdminCommandQueuedDto>(
                "/api/admin/commands",
                request,
                AccessToken,
                response => onSuccess?.Invoke(
                    response != null && !string.IsNullOrWhiteSpace(response.message)
                        ? response.message
                        : "Sent."),
                (message, _) => onError?.Invoke(message));
        }

        /// <summary>
        /// Collects anything a developer has queued for this player. Scoped to
        /// the caller on the server, so it can only ever return their own.
        /// </summary>
        public IEnumerator CollectAdminCommands(
            Action<System.Collections.Generic.List<AdminCommandDto>> onSuccess,
            Action<string> onError)
        {
            yield return ApiClient.Instance
                .PostJson<object, System.Collections.Generic.List<AdminCommandDto>>(
                    "/api/admin/commands/pending",
                    null,
                    AccessToken,
                    response => onSuccess?.Invoke(response),
                    (message, _) => onError?.Invoke(message));
        }

        /// <summary>
        /// Takes an edited profile everywhere it is held: in memory, on the
        /// phone, and anywhere listening for a session change. Every screen that
        /// shows a name or an address reads CurrentUser live, so this is all it
        /// takes for the pause menu, the tutorial and the logout prompt to catch
        /// up at once.
        /// </summary>
        private void ApplyUserUpdate(UserResponseDto user)
        {
            if (user == null || !IsAuthenticated)
                return;

            CurrentUser = user;
            SavedSession.Save(AccessToken, user);
            SessionChanged?.Invoke();
        }

        public void StartGame()
        {
            if (!IsAuthenticated || IsBusy)
                return;

            // Always ask the server whether this account has a farm rather than
            // trusting HasFarm.
            //
            // That flag arrives on the login response and nowhere else, so a
            // session restored from the phone has never been told either way. It
            // used to short-circuit to area selection whenever the flag was false,
            // which meant every returning player was sent off to pick land they
            // already owned. Their save survived - the revision check refused the
            // overwrite - but they could not reach it.
            //
            // LoadFarmAndOpenWorld already treats a 404 as "no farm yet" and routes
            // to AreaSelection itself, so one request answers both cases and there
            // is no cached answer left to go stale.
            StartCoroutine(LoadFarmAndOpenWorld());
        }

        public IEnumerator RefreshFarm(
            Action<FarmSaveResponseDto> onSuccess,
            Action<string, long> onError)
        {
            yield return ApiClient.Instance.GetJson<FarmSaveResponseDto>(
                "/api/farms/me",
                AccessToken,
                response =>
                {
                    LoadedFarm = response;
                    HasFarm = true;
                    SessionChanged?.Invoke();
                    onSuccess?.Invoke(response);
                },
                onError);
        }

        public void ApplySavedFarm(FarmSaveResponseDto response)
        {
            LoadedFarm = response;
            HasFarm = response != null;
            SessionChanged?.Invoke();
        }

        public void Logout()
        {
            if (IsAuthenticated && ApiClient.Instance != null)
                StartCoroutine(RecordLogoutBestEffort());

            ClearSession();
        }

        /// <summary>
        /// Ends the session without telling the server, for when the server is
        /// the one that ended it.
        ///
        /// Used when a request comes back refused because the account's password
        /// was changed elsewhere. Logout would be wrong here: it posts to the
        /// server first, and that post carries the very token that was just
        /// rejected, so it can only fail.
        /// </summary>
        public void EndSessionLocally()
        {
            ClearSession();
        }

        /// <summary>Drops the session from memory and from this phone.</summary>
        private void ClearSession()
        {
            AccessToken = null;
            CurrentUser = null;
            LoadedFarm = null;
            HasFarm = false;
            FarmLoadContext.Clear();
            SavedSession.Clear();
            SessionChanged?.Invoke();
        }

        private IEnumerator LoadFarmAndOpenWorld()
        {
            IsBusy = true;
            string errorMessage = null;
            long errorCode = 0;

            yield return RefreshFarm(
                _ => { },
                (message, code) =>
                {
                    errorMessage = message;
                    errorCode = code;
                });

            IsBusy = false;

            if (errorCode == 404)
            {
                HasFarm = false;
                LoadedFarm = null;
                FarmLoadContext.Clear();
                SceneManager.LoadScene("AreaSelection");
                yield break;
            }

            // A refused token means this login is finished - the account's
            // password was changed somewhere else, which retires every token
            // issued before it. Without this the menu would look signed in and
            // Start would quietly do nothing, because the message below is
            // written onto a login board that is not currently on screen.
            if (errorCode == 401 || errorCode == 403)
            {
                ClearSession();
                AuthUIBuilder.Instance?.ShowLogin();
                AuthUIBuilder.Instance?.ShowStatus(
                    "You have been signed out. This account's password may have been " +
                    "changed on another device, or the account may no longer exist. Please sign in again.");
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                AuthUIBuilder.Instance?.ShowStatus(errorMessage);
                yield break;
            }

            PrepareLoadedFarmForScene();
            SceneManager.LoadScene("TerrainPreview");
        }

        private void PrepareLoadedFarmForScene()
        {
            FarmSnapshotDto snapshot = LoadedFarm != null ? LoadedFarm.snapshot : null;
            if (snapshot == null || snapshot.area == null)
            {
                FarmLoadContext.Clear();
                return;
            }

            SelectedAreaState.NormalizedRect = new Rect(
                snapshot.area.x,
                snapshot.area.y,
                snapshot.area.width,
                snapshot.area.height
            );
            SelectedAreaState.SelectedDistrictName = snapshot.area.districtName ?? "";
            FarmLoadContext.PendingSnapshot = snapshot;
        }

        private IEnumerator RecordLogoutBestEffort()
        {
            string token = AccessToken;

            yield return ApiClient.Instance.PostWithoutBody(
                "/api/farms/me/logout",
                token,
                () => { },
                (_, _) => { });

            // Hand the play session back, or this account stays claimed by this
            // phone for the length of the online window and cannot be played
            // anywhere else in the meantime.
            yield return ApiClient.Instance.PostJson<PresenceHeartbeatRequestDto, object>(
                "/api/presence/offline",
                new PresenceHeartbeatRequestDto { deviceId = DeviceIdentity.Current },
                token,
                _ => { },
                (_, __) => { });
        }

        private void ApplyAuth(AuthResponseDto response)
        {
            AccessToken = response.accessToken;
            CurrentUser = response.user;
            HasFarm = response.hasFarm;
            LoadedFarm = null;

            // Kept on the phone so the next launch skips the login screen. Cleared
            // only by Logout, which is the one way to change accounts on a device.
            SavedSession.Save(AccessToken, CurrentUser);

            SessionChanged?.Invoke();

            // Pull this account's saved settings and apply audio/render distance.
            StartCoroutine(LoadPlayerSettings());
        }

        /// <summary>
        /// Writes the player's settings to their account.
        ///
        /// Runs here rather than on the Settings panel because that panel is an
        /// ordinary scene object: pressing Save and then starting the game
        /// destroyed it mid-request, so the write was abandoned and the account
        /// kept its old values. ApplyFromDto then restored those old values on the
        /// next launch, quietly undoing the change. AuthSession is
        /// DontDestroyOnLoad, so the request finishes wherever the player goes.
        /// </summary>
        public void SaveSettingsToAccount()
        {
            if (ApiClient.Instance == null || !IsAuthenticated)
                return;

            StartCoroutine(PushPlayerSettings());
        }

        private IEnumerator PushPlayerSettings()
        {
            yield return ApiClient.Instance.PutJson<SettingsDto, SettingsDto>(
                "/api/settings/me",
                GameSettings.ToDto(),
                AccessToken,
                _ => { },
                // Logged rather than swallowed. A silent failure here looks like the
                // setting simply not working, which is exactly how it was found.
                (message, _) => Debug.LogWarning(
                    "[Settings] Could not save settings to the account: " + message));
        }

        private IEnumerator LoadPlayerSettings()
        {
            if (ApiClient.Instance == null || string.IsNullOrWhiteSpace(AccessToken))
                yield break;

            yield return ApiClient.Instance.GetJson<SettingsDto>(
                "/api/settings/me",
                AccessToken,
                dto => GameSettings.ApplyFromDto(dto),
                (_, _) => { });
        }
    }
}
