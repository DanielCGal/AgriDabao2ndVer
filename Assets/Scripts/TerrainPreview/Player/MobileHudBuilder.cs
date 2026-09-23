using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace AgriDabao3D
{
    public class MobileHudBuilder : MonoBehaviour
    {
        /// <summary>
        /// The scene's one active joystick/jump HUD. FirstPersonTerrainController
        /// reads MoveInput/jumpPressed from a specific instance found via the
        /// Inspector or FindFirstObjectByType, so a second, unwired copy left in
        /// the scene would silently eat touch input without moving the player -
        /// this guard makes any duplicate destroy itself instead.
        /// </summary>
        public static MobileHudBuilder Instance { get; private set; }

        public Vector2 MoveInput { get; private set; }

        /// <summary>
        /// How many times jump has been pressed this session.
        ///
        /// Watchers such as the beginner guide need to know a jump happened
        /// without stealing it: ConsumeJumpPressed clears the flag, so anything
        /// polling that would swallow the input before the player controller saw
        /// it and the player would never leave the ground.
        /// </summary>
        public int JumpPressCount { get; private set; }

        private bool jumpPressed;
        private RectTransform joystickRoot;
        private RectTransform jumpButtonRect;
        private RectTransform joystickHandle;
        private Canvas canvas;

        public bool ConsumeJumpPressed()
        {
            bool value = jumpPressed;
            jumpPressed = false;
            return value;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    "Duplicate MobileHudBuilder found in the scene ('" + gameObject.name +
                    "'); destroying it so only one joystick/jump HUD is active. " +
                    "Remove the extra GameObject from the scene to clear this warning.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        [Tooltip("Build the joystick and jump button in the Editor too. Off by " +
                 "default, matching how this has always worked - the Editor uses " +
                 "keyboard and mouse. Turn it on to test the on-screen controls, " +
                 "or the beginner guide's movement lesson, without deploying.")]
        public bool alsoBuildInEditor;

        private void Start()
        {
            if (!Application.isMobilePlatform && !alsoBuildInEditor)
            {
                gameObject.SetActive(false);
                return;
            }

            EnsureEventSystem();
            BuildHud();
        }

        private void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private void BuildHud()
        {
            canvas = Object.FindFirstObjectByType<Canvas>();

            if (canvas == null)
            {
                var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 1f; // landscape: scale by height
            }

            CreateJoystick();
            CreateJumpButton();
        }

        private void CreateJoystick()
        {
            var root = new GameObject("MoveJoystick", typeof(RectTransform), typeof(Image), typeof(VirtualJoystick));
            root.transform.SetParent(canvas.transform, false);

            joystickRoot = root.GetComponent<RectTransform>();
            joystickRoot.anchorMin = new Vector2(0f, 0f);
            joystickRoot.anchorMax = new Vector2(0f, 0f);
            joystickRoot.pivot = new Vector2(0.5f, 0.5f);
            joystickRoot.sizeDelta = new Vector2(220f, 220f);
            joystickRoot.anchoredPosition = new Vector2(170f, 170f);

            UIThemeSprites theme = UIThemeSprites.Instance;

            var bg = root.GetComponent<Image>();
            if (theme?.joystickBase != null)
            {
                bg.sprite = theme.joystickBase;
                bg.preserveAspect = true;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(1f, 1f, 1f, 0.18f);
            }

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(root.transform, false);

            joystickHandle = handleGo.GetComponent<RectTransform>();
            joystickHandle.sizeDelta = new Vector2(100f, 100f);
            joystickHandle.anchoredPosition = Vector2.zero;

            var handleImage = handleGo.GetComponent<Image>();
            if (theme?.joystickHandle != null)
            {
                handleImage.sprite = theme.joystickHandle;
                handleImage.preserveAspect = true;
                handleImage.color = Color.white;
            }
            else
            {
                handleImage.color = new Color(1f, 1f, 1f, 0.38f);
            }

            var joystick = root.GetComponent<VirtualJoystick>();
            joystick.Setup(joystickRoot, joystickHandle, this);

            HudRegistry.RegisterPiece(HudPiece.Joystick, root);
        }

        private void CreateJumpButton()
        {
            var go = new GameObject("JumpButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(HoldButton));
            go.transform.SetParent(canvas.transform, false);

            HudRegistry.RegisterPiece(HudPiece.JumpButton, go);

            jumpButtonRect = go.GetComponent<RectTransform>();
            jumpButtonRect.anchorMin = new Vector2(1f, 0f);
            jumpButtonRect.anchorMax = new Vector2(1f, 0f);
            jumpButtonRect.pivot = new Vector2(0.5f, 0.5f);
            jumpButtonRect.sizeDelta = new Vector2(170f, 170f);
            jumpButtonRect.anchoredPosition = new Vector2(-170f, 170f);

            Sprite jumpArt = UIThemeSprites.Instance?.jumpButton;

            var image = go.GetComponent<Image>();
            if (jumpArt != null)
            {
                // The word is painted into the art, so no Text child is added.
                image.sprite = jumpArt;
                image.preserveAspect = true;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(1f, 1f, 1f, 0.22f);

                var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                textGo.transform.SetParent(go.transform, false);

                var textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                var text = textGo.GetComponent<Text>();
                text.text = "JUMP";
                text.font = GameFonts.Primary;
                text.fontSize = 30;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
            }

            var hold = go.GetComponent<HoldButton>();
            hold.onPressed = () =>
            {
                jumpPressed = true;
                JumpPressCount++;
            };
        }

        public void SetMoveInput(Vector2 value)
        {
            MoveInput = Vector2.ClampMagnitude(value, 1f);
        }

        public bool IsInJoystickZone(Vector2 screenPos)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(joystickRoot, screenPos, null);
        }

        public bool IsInJumpZone(Vector2 screenPos)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(jumpButtonRect, screenPos, null);
        }
    }
}
