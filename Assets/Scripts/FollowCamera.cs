using UnityEngine;
using UnityEngine.InputSystem;

namespace StupidGame
{
    /// <summary>Third-person orbit camera. Mouse look is only active when the cursor is locked.</summary>
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 7.5f;
        public float pivotHeight = 1.4f;
        public float yaw = 90f;
        public float pitch = 16f;
        public float sensitivity = 0.12f;
        public float smoothTime = 0.08f;
        public float minPitch = -15f;
        public float maxPitch = 65f;

        Vector3 _vel;

        public void Init(Transform t)
        {
            target = t;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Snap();
        }

        void Snap()
        {
            transform.position = DesiredPosition();
        }

        Vector3 Pivot()
        {
            return target != null ? target.position + Vector3.up * pivotHeight : transform.position;
        }

        Vector3 DesiredPosition()
        {
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            return Pivot() + rot * Vector3.back * distance;
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
                if (m0 != null && (m0.leftButton.wasPressedThisFrame || m0.rightButton.wasPressedThisFrame))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                return;
            }

            var m = Mouse.current;
            if (m == null) return;
            Vector2 d = m.delta.ReadValue();
            yaw += d.x * sensitivity;
            pitch = Mathf.Clamp(pitch - d.y * sensitivity, minPitch, maxPitch);
        }

        void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref _vel, smoothTime);
            transform.rotation = Quaternion.LookRotation(Pivot() - transform.position, Vector3.up);
        }
    }
}
