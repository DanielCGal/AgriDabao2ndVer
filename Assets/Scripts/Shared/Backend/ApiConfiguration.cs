using UnityEngine;

namespace AgriDabao3D
{
    public class ApiConfiguration : MonoBehaviour
    {
        public static ApiConfiguration Instance { get; private set; }

        [Header("Backend URLs")]
        [Tooltip("Used while running the game inside the Unity Editor on the same PC as Spring Boot.")]
        public string editorBaseUrl = "http://localhost:8080";

        [Tooltip("Used by the standard Android emulator.")]
        public string androidEmulatorBaseUrl = "http://10.0.2.2:8080";

        [Tooltip("Your PC's LAN address, e.g. http://192.168.1.5:8080. Only reachable " +
                 "while the phone is on the same Wi-Fi as the PC.")]
        public string physicalAndroidBaseUrl = "http://192.168.1.5:8080";

        [Header("Public Hosting (works on mobile data, anywhere)")]
        [Tooltip("Public HTTPS address of the deployed backend. When set, this wins " +
                 "over every address above on a device build.\n\n" +
                 "NOTE: this component is created at runtime by BackendRuntimeBootstrap, " +
                 "so there is no Inspector slot to edit - the value below is what ships. " +
                 "Change it here if the deployment URL ever changes.")]
        public string publicBaseUrl = "https://agridabao-api-production.up.railway.app";

        [Tooltip("Also route the Editor through publicBaseUrl. Handy for testing the " +
                 "deployed server from the Editor; leave off to keep using localhost.")]
        public bool usePublicUrlInEditor = true;

        public bool usePhysicalAndroidAddress;

        public string BaseUrl
        {
            get
            {
                bool hasPublic = !string.IsNullOrWhiteSpace(publicBaseUrl);

#if UNITY_EDITOR
                if (hasPublic && usePublicUrlInEditor)
                    return publicBaseUrl.TrimEnd('/');
                return editorBaseUrl.TrimEnd('/');
#elif UNITY_ANDROID
                if (hasPublic)
                    return publicBaseUrl.TrimEnd('/');
                return (usePhysicalAndroidAddress ? physicalAndroidBaseUrl : androidEmulatorBaseUrl).TrimEnd('/');
#else
                if (hasPublic)
                    return publicBaseUrl.TrimEnd('/');
                return editorBaseUrl.TrimEnd('/');
#endif
            }
        }

        public bool IsCleartext =>
            BaseUrl.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("[Api] Base URL: " + BaseUrl);

#if !UNITY_EDITOR && UNITY_ANDROID
            if (IsCleartext)
            {
                Debug.LogWarning(
                    "[Api] Using a plain http:// address on Android. Android 9+ blocks " +
                    "cleartext traffic by default, so requests will fail unless " +
                    "'Allow downloads over HTTP' is set to Always in Player Settings. " +
                    "Deploy the backend behind HTTPS and set publicBaseUrl instead.");
            }
#endif
        }
    }
}
