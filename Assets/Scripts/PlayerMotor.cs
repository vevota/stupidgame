using UnityEngine;
using UnityEngine.InputSystem;

namespace StupidGame
{
    /// <summary>
    /// Deliberately stiff, low-air-control platformer body. Foddian feel comes from
    /// committed jumps: once you are airborne there is very little you can do.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerMotor : MonoBehaviour
    {
        public float moveSpeed = 6.5f;
        public float groundAccel = 80f;
        public float airControl = 0.25f;
        public float jumpHeight = 2.1f;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.12f;
        public float fallMultiplier = 1.7f;

        public Transform CameraTransform { get; set; }
        public bool Grounded { get; private set; }
        public Vector3 Velocity => _rb.linearVelocity;

        Rigidbody _rb;
        CapsuleCollider _col;
        float _coyote;
        float _buffer;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<CapsuleCollider>();
            _rb.mass = 1f;
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.spaceKey.wasPressedThisFrame) _buffer = jumpBufferTime;
            _buffer -= Time.deltaTime;
            _coyote -= Time.deltaTime;
        }

        void FixedUpdate()
        {
            Grounded = ProbeGround();
            if (Grounded) _coyote = coyoteTime;

            Vector3 wish = WishDirection();
            Vector3 vel = _rb.linearVelocity;
            Vector3 horiz = new Vector3(vel.x, 0f, vel.z);

            float accel = Grounded ? groundAccel : groundAccel * airControl;
            horiz = Vector3.MoveTowards(horiz, wish * moveSpeed, accel * Time.fixedDeltaTime);

            float y = vel.y;

            if (_buffer > 0f && _coyote > 0f)
            {
                _buffer = 0f;
                _coyote = 0f;
                y = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);
            }
            else if (y < 0f)
            {
                // Heavier fall than rise: jumps feel snappy instead of floaty.
                _rb.AddForce(Vector3.up * Physics.gravity.y * (fallMultiplier - 1f), ForceMode.Acceleration);
            }

            _rb.linearVelocity = new Vector3(horiz.x, y, horiz.z);

            if (wish.sqrMagnitude > 0.01f)
            {
                var face = Quaternion.LookRotation(wish, Vector3.up);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, face, 14f * Time.fixedDeltaTime));
            }
        }

        Vector3 WishDirection()
        {
            var kb = Keyboard.current;
            if (kb == null) return Vector3.zero;

            float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            if (x == 0f && z == 0f) return Vector3.zero;

            Transform cam = CameraTransform != null ? CameraTransform : (Camera.main != null ? Camera.main.transform : null);
            if (cam == null) return new Vector3(x, 0f, z).normalized;

            Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            return (forward * z + right * x).normalized;
        }

        bool ProbeGround()
        {
            Bounds b = _col.bounds;
            float half = b.extents.y;
            float r = Mathf.Min(b.extents.x, b.extents.z) * 0.75f;
            Vector3 origin = b.center - Vector3.up * (half - 0.06f);

            if (RayDown(origin)) return true;
            if (RayDown(origin + new Vector3(r, 0f, 0f))) return true;
            if (RayDown(origin + new Vector3(-r, 0f, 0f))) return true;
            if (RayDown(origin + new Vector3(0f, 0f, r))) return true;
            if (RayDown(origin + new Vector3(0f, 0f, -r))) return true;
            return false;
        }

        bool RayDown(Vector3 origin)
        {
            var hits = Physics.RaycastAll(origin, Vector3.down, 0.45f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h.collider == _col) continue;
                if (h.collider.transform.IsChildOf(transform)) continue;
                if (h.normal.y >= 0.5f) return true;
            }
            return false;
        }

        public void Teleport(Vector3 position)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.position = position;
            transform.position = position;
        }
    }
}
