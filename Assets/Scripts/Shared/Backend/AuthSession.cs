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

        private void RestoreSavedSession()
        {
            if (!SavedSession.TryLoad(out string token, out UserResponseDto user))
                return;

            AccessToken = token;
            CurrentUser = user;

            HasFarm = false;
            SessionChanged?.Invoke();

            StartCoroutine(VerifyRestoredSession());
        }

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

        public void EndSessionLocally()
        {
            ClearSession();
        }

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

            SavedSession.Save(AccessToken, CurrentUser);

            SessionChanged?.Invoke();

            StartCoroutine(LoadPlayerSettings());
        }

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
