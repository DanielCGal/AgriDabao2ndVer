using UnityEngine;
using UnityEngine.InputSystem;

namespace AgriDabao3D
{
    public class FreeCameraController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 25f;
        public float fastMoveMultiplier = 2.5f;
        public float lookSpeed = 2f;
        public float climbSpeed = 12f;

        [Header("Mouse Lock")]
        public bool lockCursorOnStart = true;

        private float yaw;
        private float pitch;

        private void Start()
        {
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = euler.x;

#if UNITY_EDITOR || UNITY_STANDALONE
            if (lockCursorOnStart)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
#endif
        }

        private void Update()
        {
#if !UNITY_EDITOR && !UNITY_STANDALONE
            return;
#endif

            HandleLook();
            HandleMove();

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void HandleLook()
        {
            if (Mouse.current == null)
                return;

            Vector2 delta = Mouse.current.delta.ReadValue() * lookSpeed * Time.deltaTime * 60f;

            yaw += delta.x;
            pitch -= delta.y;
            pitch = Mathf.Clamp(pitch, -80f, 80f);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void HandleMove()
        {
            if (Keyboard.current == null)
                return;

            float speed = moveSpeed;
            if (Keyboard.current.leftShiftKey.isPressed)
                speed *= fastMoveMultiplier;

            Vector3 move = Vector3.zero;

            if (Keyboard.current.wKey.isPressed) move += transform.forward;
            if (Keyboard.current.sKey.isPressed) move -= transform.forward;
            if (Keyboard.current.dKey.isPressed) move += transform.right;
            if (Keyboard.current.aKey.isPressed) move -= transform.right;
            if (Keyboard.current.eKey.isPressed) move += Vector3.up;
            if (Keyboard.current.qKey.isPressed) move -= Vector3.up;

            transform.position += move.normalized * speed * Time.deltaTime;
        }
    }
}
