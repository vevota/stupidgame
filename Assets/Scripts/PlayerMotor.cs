using UnityEngine;
using UnityEngine.InputSystem;

namespace StupidGame
{
    /// <summary>
    /// Stiff first-person platformer body. Foddian feel comes from committed jumps:
    /// once airborne there is almost nothing you can do about it.
    /// Shift sprints, Ctrl crouches.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerMotor : MonoBehaviour
    {
        public float walkSpeed = 6.0f;
        public float sprintSpeed = 9.5f;
        public float crouchSpeed = 3.0f;
        public float groundAccel = 80f;
        public float airControl = 0.25f;
        public float jumpHeight = 2.1f;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.12f;
        public float fallMultiplier = 1.7f;

        public float standHeight = 1.8f;
        public float crouchHeight = 1.2f;
        public float standEye = 1.55f;
        public float crouchEye = 1.05f;
        public float eyeLerp = 14f;

        public Transform CameraTransform { get; set; }
        public bool Grounded { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsSprinting { get; private set; }
        public float EyeHeight { get; private set; }
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

            _col.radius = 0.4f;
            _col.height = standHeight;
            _col.center = Vector3.zero;

            EyeHeight = standEye;

            var mesh = GetComponent<Renderer>();
            if (mesh != null) mesh.enabled = false;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // Bunny hop: holding jump re-arms the buffer every frame, so the moment you
            // touch ground the next jump fires. No double jumps, because jumping zeroes
            // coyote and coyote is only refilled while grounded.
            if (kb.spaceKey.isPressed) _buffer = jumpBufferTime;
            _buffer -= Time.deltaTime;
            _coyote -= Time.deltaTime;

            bool wantsCrouch = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
            if (wantsCrouch) SetCrouch(true);
            else if (IsCrouching && CanStand()) SetCrouch(false);

            IsSprinting = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;

            float targetEye = IsCrouching ? crouchEye : standEye;
            EyeHeight = Mathf.Lerp(EyeHeight, targetEye, 1f - Mathf.Exp(-eyeLerp * Time.deltaTime));
        }

        void FixedUpdate()
        {
            Grounded = ProbeGround();
            Vector3 vel = _rb.linearVelocity;

            // Refill coyote only when grounded AND not rising. With bunny hop the jump buffer
            // is re-armed every frame, so without this the held key re-jumps during the first
            // frames of ascent while the ground probe still reports contact.
            if (Grounded && vel.y <= 0.01f) _coyote = coyoteTime;

            Vector3 wish = WishDirection();
            Vector3 horiz = new Vector3(vel.x, 0f, vel.z);

            float speed = IsCrouching ? crouchSpeed : (IsSprinting ? sprintSpeed : walkSpeed);
            float accel = Grounded ? groundAccel : groundAccel * airControl;
            horiz = Vector3.MoveTowards(horiz, wish * speed, accel * Time.fixedDeltaTime);

            float y = vel.y;

            if (_buffer > 0f && _coyote > 0f && !IsCrouching)
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
        }

        void SetCrouch(bool on)
        {
            if (on == IsCrouching) return;
            IsCrouching = on;

            float h = on ? crouchHeight : standHeight;
            _col.height = h;
            // Keep the feet planted: the capsule bottom must not move.
            _col.center = new Vector3(0f, -(standHeight - h) * 0.5f, 0f);
        }

        bool CanStand()
        {
            float r = _col.radius * 0.9f;
            Vector3 bottom = transform.position + Vector3.up * (-standHeight * 0.5f + r);
            Vector3 top = transform.position + Vector3.up * (standHeight * 0.5f - r);

            _col.enabled = false;
            bool free = !Physics.CheckCapsule(bottom, top, r, ~0, QueryTriggerInteraction.Ignore);
            _col.enabled = true;
            return free;
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
