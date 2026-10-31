using UnityEngine;
using UnityEngine.InputSystem;

namespace StupidGame
{
    /// <summary>
    /// First-person look. The camera is NOT a child of the player: it is positioned every
    /// frame from the player's interpolated position. Parenting it to an interpolated
    /// Rigidbody causes physics interpolation to fight the yaw, and the camera stops turning.
    /// </summary>
    public class FirstPersonLook : MonoBehaviour
    {
        public PlayerMotor motor;
        public Transform player;
        public float sensitivity = 0.12f;
        public float minPitch = -85f;
        public float maxPitch = 85f;

        float _yaw = 90f;
        float _pitch;

        public void Init(PlayerMotor playerMotor, float startYaw)
        {
            motor = playerMotor;
            player = playerMotor != null ? playerMotor.transform : null;
            _yaw = startYaw;
            _pitch = 0f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Apply();
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
            Apply();
        }

        void Apply()
        {
            if (player == null) return;

            float eye = motor != null ? motor.EyeHeight : 1.55f;
            transform.position = player.position + Vector3.up * eye;
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
    }
}
