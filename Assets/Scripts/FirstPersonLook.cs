using UnityEngine;
using UnityEngine.InputSystem;

namespace StupidGame
{
    /// <summary>
    /// First-person look. Yaw drives the player body, pitch drives the camera.
    /// The camera is a child of the player, so this owns the camera's local rotation.
    /// </summary>
    public class FirstPersonLook : MonoBehaviour
    {
        public Transform player;
        public float sensitivity = 0.12f;
        public float minPitch = -85f;
        public float maxPitch = 85f;

        float _yaw;
        float _pitch;

        public float Yaw => _yaw;

        public void Init(Transform playerTransform)
        {
            player = playerTransform;
            _yaw = player.eulerAngles.y;
            _pitch = 0f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                var m0 = Mouse.current;
                if (m0 != null && m0.leftButton.wasPressedThisFrame)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                return;
            }

            var m = Mouse.current;
            if (m == null) return;

            Vector2 d = m.delta.ReadValue();
            _yaw += d.x * sensitivity;
            _pitch = Mathf.Clamp(_pitch - d.y * sensitivity, minPitch, maxPitch);
        }

        void LateUpdate()
        {
            if (player != null) player.rotation = Quaternion.Euler(0f, _yaw, 0f);
            transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
    }
}
