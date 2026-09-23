using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace AgriDabao3D
{
    /// <summary>
    /// A video that fills the screen behind a menu and keeps looping.
    ///
    /// Unity's VideoPlayer does not reliably keep running on its own. It stops at
    /// the loop point on some devices, loses its render texture when the app is
    /// backgrounded or the on-screen keyboard opens and closes, and can sit
    /// "playing" while the picture is frozen. Setting isLooping and walking away
    /// was the cause of the main menu background stopping after a while.
    ///
    /// So the looping is watched rather than trusted. The player is re-asserted at
    /// every loop point, restarted on an error, restarted when the app comes back
    /// to the foreground or the keyboard closes, and checked a few times a second
    /// for a frame counter that has stopped moving. The render texture is rebuilt
    /// if it was lost. This is the same treatment the main menu background needed,
    /// kept in one place so a second screen does not have to rediscover it.
    ///
    /// Audio is switched off at the source - a background loop is decoration, and
    /// whatever music is baked into the clip would fight the game's own.
    /// </summary>
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

        /// <summary>
        /// Builds the picture and its player under <paramref name="parent"/>, filling
        /// it. Returns null when there is no clip to play, so the caller can fall
        /// back to a still background.
        /// </summary>
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

            // Hidden until the first frame lands, so the fallback behind it shows
            // instead of one frame of whatever the texture happened to contain.
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

            // The clip's own soundtrack is discarded here rather than turned down,
            // so it can never be heard however the game's volume is set.
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

        /// <summary>
        /// True when the player claims to be playing but the picture has not moved
        /// for a while - the failure that looks like the video simply stopping.
        /// </summary>
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

            // Some devices stop at the loop point instead of wrapping, so the wrap
            // is done by hand when that happens.
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
