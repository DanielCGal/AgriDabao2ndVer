using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace AgriDabao3D
{
    public class LoopingVideoBackground : MonoBehaviour
    {
        private VideoPlayer player;
        private RawImage rawImage;
        private RenderTexture renderTexture;
        private VideoClip clip;

        private int textureWidth = 1920;
        private int textureHeight = 1080;

        private bool shouldRun;
        private bool needsRestart;
        private float nextCheck;
        private bool keyboardWasVisible;
        private long lastFrame = -1;
        private float lastFrameTime;

        public static LoopingVideoBackground Create(
            string objectName, RectTransform parent, VideoClip clip, int width, int height)
        {
            if (parent == null || clip == null)
                return null;

            var go = new GameObject(objectName, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var raw = go.GetComponent<RawImage>();
            raw.color = Color.white;
            raw.raycastTarget = false;

            raw.enabled = false;

            var background = go.AddComponent<LoopingVideoBackground>();
            background.Setup(raw, clip, width, height);
            return background;
        }

        private void Setup(RawImage image, VideoClip videoClip, int width, int height)
        {
            rawImage = image;
            clip = videoClip;
            textureWidth = Mathf.Max(16, width);
            textureHeight = Mathf.Max(16, height);

            RecreateRenderTexture();
            rawImage.texture = renderTexture;

            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = true;
            player.skipOnDrop = false;
            player.waitForFirstFrame = true;
            player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = renderTexture;

            player.audioOutputMode = VideoAudioOutputMode.None;
            player.clip = clip;

            player.loopPointReached += OnLoopPoint;
            player.errorReceived += OnError;
            player.started += OnStarted;

            shouldRun = true;
            Restore(true);
        }

        private void Update()
        {
            if (!shouldRun)
                return;

            bool keyboardVisible = TouchScreenKeyboard.visible;
            if (keyboardWasVisible && !keyboardVisible)
                needsRestart = true;
            keyboardWasVisible = keyboardVisible;

            if (Time.unscaledTime < nextCheck && !needsRestart)
                return;

            nextCheck = Time.unscaledTime + 0.35f;
            Restore(needsRestart);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                needsRestart = true;
            else
                Restore(true);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                needsRestart = true;
            else
                Restore(true);
        }

        private void Restore(bool force)
        {
            if (!shouldRun || player == null || clip == null)
                return;

            player.isLooping = true;
            player.skipOnDrop = false;

            bool textureLost = renderTexture == null || !renderTexture.IsCreated();
            bool notPlaying = !player.isPlaying;
            bool stalled = IsStalled();

            if (!force && !needsRestart && !textureLost && !notPlaying && !stalled)
                return;

            needsRestart = false;

            bool hardRestart = force || textureLost || stalled;
            if (textureLost || stalled)
                RecreateRenderTexture();

            player.clip = clip;
            player.isLooping = true;
            player.skipOnDrop = false;
            player.targetTexture = renderTexture;

            if (rawImage != null)
            {
                rawImage.texture = renderTexture;
                if (textureLost)
                    rawImage.enabled = false;
            }

            if (hardRestart && player.isPlaying)
                player.Stop();

            player.Play();
            lastFrame = player.frame;
            lastFrameTime = Time.unscaledTime;
        }

        private bool IsStalled()
        {
            if (player == null || !player.isPlaying)
                return false;

            long frame = player.frame;
            if (frame != lastFrame)
            {
                lastFrame = frame;
                lastFrameTime = Time.unscaledTime;
                if (rawImage != null)
                    rawImage.enabled = true;
                return false;
            }

            return Time.unscaledTime - lastFrameTime > 1.5f;
        }

        private void RecreateRenderTexture()
        {
            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(textureWidth, textureHeight, 0);
                renderTexture.name = name + "_RT";
            }
            else
            {
                renderTexture.Release();
            }

            renderTexture.Create();
        }

        private void OnLoopPoint(VideoPlayer source)
        {
            source.isLooping = true;

            if (!source.isPlaying)
            {
                source.time = 0d;
                source.Play();
            }
        }

        private void OnError(VideoPlayer source, string message)
        {
            needsRestart = true;
        }

        private void OnStarted(VideoPlayer source)
        {
            if (rawImage != null)
                rawImage.enabled = true;

            lastFrame = source.frame;
            lastFrameTime = Time.unscaledTime;
        }

        private void OnDestroy()
        {
            shouldRun = false;

            if (player != null)
            {
                player.loopPointReached -= OnLoopPoint;
                player.errorReceived -= OnError;
                player.started -= OnStarted;
                player.Stop();
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }
        }
    }
}
