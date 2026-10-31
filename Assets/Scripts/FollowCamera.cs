// SUPERSEDED: this was the third-person orbit camera for slice 1.
// The game is first person now — see FirstPersonLook.cs.
// Kept (not deleted) in case we want a third-person toggle later.
using UnityEngine;

namespace StupidGame
{
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
            return Pivot() + Quaternion.Euler(pitch, yaw, 0f) * Vector3.back * distance;
        }

        void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref _vel, smoothTime);
            transform.rotation = Quaternion.LookRotation(Pivot() - transform.position, Vector3.up);
        }
    }
}
