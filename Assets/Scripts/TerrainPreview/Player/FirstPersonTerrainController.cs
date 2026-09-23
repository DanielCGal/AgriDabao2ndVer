using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.EventSystems;
namespace AgriDabao3D
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonTerrainController : MonoBehaviour
    {
        [Header("References")]
        public Camera playerCamera;
        public SoilAwareTerrainGenerator soilInspector;
        public MobileHudBuilder mobileHud;
        [Header("Movement")]
        public float moveSpeed = 6f;
        public float jumpHeight = 1.4f;
        public float gravity = -20f;
        [Header("Look")]
        public float mouseLookSensitivity = 0.12f;
        public float touchLookSensitivity = 0.18f;
        public float minPitch = -75f;
        public float maxPitch = 75f;
        [Header("Tap Inspect")]
        public float tapMoveThreshold = 18f;
        private CharacterController controller;
        private float verticalVelocity;
        private float yaw;
        private float pitch;
        private int activeLookTouchId = -1;
        private Vector2 lastLookTouchPos;
        private Vector2 lookTouchStartPos;
        private bool lookTouchMoved;
        private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
        public FarmingInteractionSystem farmingSystem;
        public ClimateMaintenanceInteractionSystem climateMaintenanceSystem;
        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();
            if (soilInspector == null)
                soilInspector = Object.FindFirstObjectByType<SoilAwareTerrainGenerator>();
            if (mobileHud == null)
                mobileHud = Object.FindFirstObjectByType<MobileHudBuilder>();
            if (farmingSystem == null)
                farmingSystem = Object.FindFirstObjectByType<FarmingInteractionSystem>();
            if (climateMaintenanceSystem == null)
                climateMaintenanceSystem = Object.FindFirstObjectByType<ClimateMaintenanceInteractionSystem>();
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            if (playerCamera != null)
                pitch = playerCamera.transform.localEulerAngles.x;
        }
        private void Update()
        {
            HandleLook();
            HandleMovement();
            HandleInspectClick();
            HandleMobileTouchLookAndTap();
        }
        private void HandleMovement()
        {
            Vector2 moveInput = Vector2.zero;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) moveInput.y += 1f;
                if (Keyboard.current.sKey.isPressed) moveInput.y -= 1f;
                if (Keyboard.current.dKey.isPressed) moveInput.x += 1f;
                if (Keyboard.current.aKey.isPressed) moveInput.x -= 1f;
            }
            if (mobileHud != null)
                moveInput += mobileHud.MoveInput;
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
            Vector3 move =
                transform.right * moveInput.x +
                transform.forward * moveInput.y;
            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            bool jumpPressed =
                (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
                (mobileHud != null && mobileHud.ConsumeJumpPressed());
            if (jumpPressed && controller.isGrounded)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;
            controller.Move(move * moveSpeed * Time.deltaTime);
        }
        private void HandleLook()
        {
            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                ApplyLook(delta * mouseLookSensitivity);
            }
        }
        private void ApplyLook(Vector2 delta)
        {
            yaw += delta.x;
            pitch -= delta.y;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (playerCamera != null)
                playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
        private void HandleInspectClick()
        {
            if (Mouse.current == null)
                return;
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 screenPos = Mouse.current.position.ReadValue();
                if (IsPointerOverUI(screenPos))
                    return;
                bool handled = false;
                if (climateMaintenanceSystem != null)
                    handled = climateMaintenanceSystem.HandleInteractAtScreenPosition(screenPos);
                if (!handled && farmingSystem != null)
                    handled = farmingSystem.HandleInteractAtScreenPosition(screenPos);
                if (!handled && soilInspector != null)
                    soilInspector.InspectAtScreenPosition(screenPos);
            }
        }
        private void HandleMobileTouchLookAndTap()
        {
            if (Touchscreen.current == null || soilInspector == null || mobileHud == null)
                return;
            var touches = Touchscreen.current.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (!touch.press.isPressed && !touch.press.wasReleasedThisFrame)
                    continue;
                int id = touch.touchId.ReadValue();
                Vector2 pos = touch.position.ReadValue();
                if (touch.press.wasPressedThisFrame)
                {
                    if (IsPointerOverUI(pos))
                        continue;
                    if (mobileHud.IsInJoystickZone(pos) || mobileHud.IsInJumpZone(pos))
                        continue;
                    if (pos.x >= Screen.width * 0.5f)
                    {
                        activeLookTouchId = id;
                        lastLookTouchPos = pos;
                        lookTouchStartPos = pos;
                        lookTouchMoved = false;
                    }
                }
                if (id == activeLookTouchId && touch.press.isPressed)
                {
                    Vector2 delta = pos - lastLookTouchPos;
                    if (delta.sqrMagnitude > 1f)
                    {
                        lookTouchMoved = lookTouchMoved ||
                                         Vector2.Distance(pos, lookTouchStartPos) > tapMoveThreshold;
                        ApplyLook(delta * touchLookSensitivity * Time.deltaTime * 60f);
                    }
                    lastLookTouchPos = pos;
                }
                if (id == activeLookTouchId && touch.press.wasReleasedThisFrame)
                {
                    if (!lookTouchMoved)
                    {
                        bool handled = false;
                        if (climateMaintenanceSystem != null)
                            handled = climateMaintenanceSystem.HandleInteractAtScreenPosition(pos);
                        if (!handled && farmingSystem != null)
                            handled = farmingSystem.HandleInteractAtScreenPosition(pos);
                        if (!handled && soilInspector != null)
                            soilInspector.InspectAtScreenPosition(pos);
                    }
                    activeLookTouchId = -1;
                }
            }
        }
        public void SnapToTerrain(Terrain terrain, float extraHeight = 2f)
        {
            if (terrain == null) return;
            Vector3 pos = transform.position;
            float terrainY = terrain.SampleHeight(pos) + terrain.transform.position.y;
            pos.y = terrainY + extraHeight;
            controller.enabled = false;
            transform.position = pos;
            controller.enabled = true;
            verticalVelocity = 0f;
        }
        private bool IsPointerOverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null)
                return false;
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPos
            };
            uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(pointerData, uiRaycastResults);
            return uiRaycastResults.Count > 0;
        }
    }
}
