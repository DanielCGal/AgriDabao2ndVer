using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace AgriDabao3D
{
    public class ApiClient : MonoBehaviour
    {
        public static ApiClient Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public IEnumerator PostJson<TRequest, TResponse>(
            string path,
            TRequest body,
            string bearerToken,
            Action<TResponse> onSuccess,
            Action<string, long> onError)
        {
            yield return SendJson("POST", path, body, bearerToken, onSuccess, onError);
        }

        public IEnumerator PutJson<TRequest, TResponse>(
            string path,
            TRequest body,
            string bearerToken,
            Action<TResponse> onSuccess,
            Action<string, long> onError)
        {
            yield return SendJson("PUT", path, body, bearerToken, onSuccess, onError);
        }

        public IEnumerator GetJson<TResponse>(
            string path,
            string bearerToken,
            Action<TResponse> onSuccess,
            Action<string, long> onError)
        {
            string url = GetUrl(path);
            using UnityWebRequest request = UnityWebRequest.Get(url);
            ApplyHeaders(request, bearerToken);

            yield return request.SendWebRequest();
            HandleResponse(request, onSuccess, onError);
        }

        public IEnumerator PostWithoutBody(
            string path,
            string bearerToken,
            Action onSuccess,
            Action<string, long> onError)
        {
            string url = GetUrl(path);
            using UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.uploadHandler = new UploadHandlerRaw(Array.Empty<byte>());
            ApplyHeaders(request, bearerToken);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke();
            else
                onError?.Invoke(ExtractError(request), request.responseCode);
        }

        private IEnumerator SendJson<TRequest, TResponse>(
            string method,
            string path,
            TRequest body,
            string bearerToken,
            Action<TResponse> onSuccess,
            Action<string, long> onError)
        {
            string url = GetUrl(path);
            string json = JsonConvert.SerializeObject(body);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            using UnityWebRequest request = new UnityWebRequest(url, method);
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            ApplyHeaders(request, bearerToken);

            yield return request.SendWebRequest();
            HandleResponse(request, onSuccess, onError);
        }

        private static void HandleResponse<TResponse>(
            UnityWebRequest request,
            Action<TResponse> onSuccess,
            Action<string, long> onError)
        {
            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(ExtractError(request), request.responseCode);
                return;
            }

            try
            {
                TResponse response = JsonConvert.DeserializeObject<TResponse>(request.downloadHandler.text);
                onSuccess?.Invoke(response);
            }
            catch (Exception ex)
            {
                onError?.Invoke("Could not read the server response: " + ex.Message, request.responseCode);
            }
        }

        private static string ExtractError(UnityWebRequest request)
        {
            string raw = request.downloadHandler != null ? request.downloadHandler.text : "";
            if (!string.IsNullOrWhiteSpace(raw))
            {
                try
                {
                    ApiErrorDto error = JsonConvert.DeserializeObject<ApiErrorDto>(raw);
                    if (error != null && !string.IsNullOrWhiteSpace(error.message))
                        return error.message;
                }
                catch
                {
                    // Fall back to the raw response below.
                }

                return raw;
            }

            return string.IsNullOrWhiteSpace(request.error)
                ? "The backend request failed."
                : request.error;
        }

        private static void ApplyHeaders(UnityWebRequest request, string bearerToken)
        {
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            if (!string.IsNullOrWhiteSpace(bearerToken))
                request.SetRequestHeader("Authorization", "Bearer " + bearerToken);
        }

        private static string GetUrl(string path)
        {
            if (ApiConfiguration.Instance == null)
                throw new InvalidOperationException("ApiConfiguration is missing from the bootstrap object.");

            return ApiConfiguration.Instance.BaseUrl + "/" + path.TrimStart('/');
        }
    }
}
