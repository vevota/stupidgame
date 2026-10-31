using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StupidGame
{
    /// <summary>
    /// The magneto stick. Only enabled while the magneto item is equipped.
    /// LMB grab/drop, RMB freeze/unfreeze, F throw.
    /// </summary>
    public class MagnetoStick : MonoBehaviour
    {
        public float range = 7f;
        public float holdDistance = 2.4f;
        public float followSharpness = 16f;
        public float throwSpeed = 12f;
        public float aimAssistRadius = 0.18f;

        public Prop Held { get; private set; }
        public Prop Aimed { get; private set; }

        /// <summary>Fired whenever the stick grabs, freezes or throws — used for viewmodel kick.</summary>
        public event Action Used;

        Camera _cam;

        public void Init(Camera cam)
        {
            _cam = cam;
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
                SetAim(null);
                return;
            }

            SetAim(FindAimedProp());

            var m = Mouse.current;
            if (m == null) return;

            if (m.leftButton.wasPressedThisFrame)
            {
                if (Held != null) Drop();
                else Grab();
            }

            if (m.rightButton.wasPressedThisFrame)
            {
                if (Held != null) FreezeHeld();
                else ToggleStasis();
            }

            var kb = Keyboard.current;
            if (kb != null && kb.fKey.wasPressedThisFrame && Held != null) ThrowHeld();
        }

        void FixedUpdate()
        {
            if (Held == null) return;

            Vector3 aim = AimDirection();
            Vector3 want = _cam.transform.position + aim * holdDistance;
            float t = 1f - Mathf.Exp(-followSharpness * Time.fixedDeltaTime);

            Held.transform.position = Vector3.Lerp(Held.transform.position, want, t);
            Held.transform.rotation = Quaternion.Slerp(Held.transform.rotation, HoldOrientation.For(aim), t);
        }

        /// <summary>Camera forward, with the pitch clamped so you can never place a prop
        /// straight up or straight down into your own feet.</summary>
        public Vector3 AimDirection()
        {
            Vector3 d = _cam != null ? _cam.transform.forward : transform.forward;
            d.y = Mathf.Clamp(d.y, -0.75f, 0.45f);
            if (d.sqrMagnitude < 0.001f) d = transform.forward;
            return d.normalized;
        }

        Prop FindAimedProp()
        {
            int mask = ~(1 << ItemController.ViewmodelLayer);
            Vector3 origin = _cam.transform.position;
            Vector3 dir = _cam.transform.forward;

            var hits = Physics.RaycastAll(origin, dir, range, mask, QueryTriggerInteraction.Ignore);
            Prop best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (!IsValidTarget(h.collider)) continue;
                if (h.distance >= bestDist) continue;

                var p = h.collider.GetComponentInParent<Prop>();
                if (p == null || !p.magnetic) continue;

                bestDist = h.distance;
                best = p;
            }

            if (best != null) return best;

            // Aim assist: a thin ray is unforgiving on small props.
            if (Physics.SphereCast(origin, aimAssistRadius, dir, out var soft, range, mask, QueryTriggerInteraction.Ignore))
            {
                if (IsValidTarget(soft.collider))
                {
                    var p = soft.collider.GetComponentInParent<Prop>();
                    if (p != null && p.magnetic) return p;
                }
            }

            return null;
        }

        bool IsValidTarget(Collider col)
        {
            if (col.transform.IsChildOf(transform)) return false;
            if (Held != null && col.transform.IsChildOf(Held.transform)) return false;
            if (Aimed != null && col.transform.IsChildOf(Aimed.transform)) return false;
            return true;
        }

        void SetAim(Prop prop)
        {
            if (prop == Aimed) return;
            if (Aimed != null) Aimed.SetHighlight(false);
            Aimed = prop;
            if (Aimed != null) Aimed.SetHighlight(true);
        }

        void Grab()
        {
            if (Aimed == null) return;

            Held = Aimed;
            Aimed = null;
            if (Held.IsFrozen) Held.Unfreeze();
            Held.BeginHold();
            Used?.Invoke();
        }

        void Drop()
        {
            if (Held == null) return;
            Held.Drop();
            Held = null;
            Used?.Invoke();
        }

        void FreezeHeld()
        {
            if (Held == null) return;
            Held.Freeze();
            Held = null;
            Used?.Invoke();
        }

        void ThrowHeld()
        {
            if (Held == null) return;
            Held.Throw(AimDirection() * throwSpeed + Vector3.up * 2f);
            Held = null;
            Used?.Invoke();
        }

        void ToggleStasis()
        {
            if (Aimed == null) return;
            if (Aimed.IsFrozen) Aimed.Unfreeze();
            else Aimed.Freeze();
            Used?.Invoke();
        }

        /// <summary>Drops whatever is held without throwing it. Used when switching items.</summary>
        public void CancelHold()
        {
            if (Held == null) return;
            Held.Drop();
            Held = null;
            SetAim(null);
        }
    }
}
