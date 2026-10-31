using UnityEngine;
using UnityEngine.InputSystem;

namespace StupidGame
{
    /// <summary>
    /// The magneto stick. Left click grabs/drops, right click freezes the held prop in
    /// midair (or unfreezes whatever you are aiming at), F throws.
    /// </summary>
    public class MagnetoStick : MonoBehaviour
    {
        public float range = 7f;
        public float holdDistance = 2.6f;
        public float holdHeight = 0.9f;
        public float followSharpness = 14f;
        public float throwSpeed = 12f;

        public Prop Held { get; private set; }
        public Prop Aimed { get; private set; }

        Camera _cam;
        PlayerMotor _player;

        public void Init(Camera cam, PlayerMotor player)
        {
            _cam = cam;
            _player = player;
        }

        void Update()
        {
            if (_cam == null)
            {
                _cam = Camera.main;
                if (_cam == null) return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                UpdateAim(null);
                return;
            }

            UpdateAim(_cam);

            var m = Mouse.current;
            if (m == null) return;

            if (m.leftButton.wasPressedThisFrame)
            {
                if (Held != null) ReleaseHeld();
                else TryGrab();
            }

            if (m.rightButton.wasPressedThisFrame)
            {
                if (Held != null) FreezeHeld();
                else TryToggleStasis();
            }

            var kb = Keyboard.current;
            if (kb != null && kb.fKey.wasPressedThisFrame && Held != null) ThrowHeld();
        }

        void FixedUpdate()
        {
            if (Held == null) return;

            Vector3 want = transform.position + Vector3.up * holdHeight + AimDir() * holdDistance;
            float t = 1f - Mathf.Exp(-followSharpness * Time.fixedDeltaTime);

            Held.transform.position = Vector3.Lerp(Held.transform.position, want, t);
            Vector3 dir = AimDir();
            if (dir.sqrMagnitude > 0.001f)
            {
                Held.transform.rotation = Quaternion.Slerp(
                    Held.transform.rotation,
                    Quaternion.LookRotation(dir, Vector3.up),
                    t);
            }
        }

        Vector3 AimDir()
        {
            if (_cam == null) return transform.forward;
            Vector3 d = _cam.transform.forward;
            d.y = Mathf.Clamp(d.y, -0.35f, 0.35f);
            if (d.sqrMagnitude < 0.001f) d = transform.forward;
            return d.normalized;
        }

        void UpdateAim(Camera cam)
        {
            Prop found = null;

            if (cam != null)
            {
                float best = float.MaxValue;
                var hits = Physics.RaycastAll(cam.transform.position, cam.transform.forward, range, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < hits.Length; i++)
                {
                    var h = hits[i];
                    if (h.collider.transform.IsChildOf(transform)) continue;
                    if (Held != null && h.collider.transform.IsChildOf(Held.transform)) continue;

                    var p = h.collider.GetComponentInParent<Prop>();
                    if (p == null || !p.magnetic) continue;
                    if (h.distance < best) { best = h.distance; found = p; }
                }
            }

            if (found != Aimed)
            {
                if (Aimed != null) Aimed.SetHighlight(false);
                Aimed = found;
                if (Aimed != null) Aimed.SetHighlight(true);
            }
        }

        void TryGrab()
        {
            if (Aimed == null) return;
            Held = Aimed;
            Aimed = null;
            if (Held.IsFrozen) Held.Unfreeze();
            Held.BeginHold();
        }

        void ReleaseHeld()
        {
            Held.Drop();
            Held = null;
        }

        void FreezeHeld()
        {
            Held.Freeze();
            Held = null;
        }

        void ThrowHeld()
        {
            Held.Throw(AimDir() * throwSpeed + Vector3.up * 2f);
            Held = null;
        }

        void TryToggleStasis()
        {
            if (Aimed == null) return;
            if (Aimed.IsFrozen) Aimed.Unfreeze();
            else Aimed.Freeze();
        }
    }
}
