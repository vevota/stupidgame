using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StupidGame
{
    /// <summary>
    /// The magneto stick. Only enabled while the magneto item is equipped.
    /// LMB grab/drop, RMB freeze/unfreeze, F throw, Q/E spin the held prop.
    ///
    /// Grip model is deliberately Source-like rather than rigid:
    ///  - the point you clicked stays the pivot, so grabs are not all identical
    ///  - position follows on a spring, so the prop lags and has weight
    ///  - rotation eases toward a level carry pose instead of being welded to the camera,
    ///    so a badly-angled grab self-corrects instead of ruining a crossing
    /// </summary>
    public class MagnetoStick : MonoBehaviour
    {
        public float range = 7f;
        public float holdDistance = 2.4f;
        public float holdDrop = 0.12f;
        public float positionSharpness = 11f;
        public float levelSharpness = 5f;
        public float throwSpeed = 12f;
        public float aimAssistRadius = 0.18f;
        public float spinSpeed = 120f;
        public float inertiaLag = 0.025f;
        public float maxInertiaLag = 0.25f;

        public Prop Held { get; private set; }
        public Prop Aimed { get; private set; }

        /// <summary>Distance from the prop's origin to the point you grabbed. Shown in the HUD.</summary>
        public float LastGrabOffset { get; private set; }

        /// <summary>Fired when the stick grabs, freezes or throws — used for viewmodel kick.</summary>
        public event Action Used;

        Camera _cam;
        PlayerMotor _player;
        Collider _selfCollider;

        // Grab state, captured at pickup.
        Vector3 _localAnchor;
        Vector3 _anchorScale;
        float _grabYawOffset;
        float _yawOffset;

        Vector3 _aimPoint;
        bool _hasAimPoint;

        public void Init(Camera cam)
        {
            _cam = cam;
            _player = GetComponent<PlayerMotor>();
            _selfCollider = GetComponent<Collider>();
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
            if (kb == null) return;

            if (kb.fKey.wasPressedThisFrame && Held != null) ThrowHeld();

            if (Held != null)
            {
                if (kb.qKey.isPressed) _yawOffset -= spinSpeed * Time.deltaTime;
                if (kb.eKey.isPressed) _yawOffset += spinSpeed * Time.deltaTime;
            }
        }

        void FixedUpdate()
        {
            if (Held == null) return;

            // Level carry pose: horizontal, yaw follows the view plus whatever offset the
            // prop had when grabbed, plus manual spin.
            float carryYaw = _cam.transform.eulerAngles.y + _grabYawOffset + _yawOffset;
            Quaternion wantRot = Quaternion.Euler(0f, carryYaw, 0f);

            // Keep the clicked point exactly on the hold point, whatever the rotation is.
            Vector3 anchorOffset = wantRot * Vector3.Scale(_localAnchor, _anchorScale);
            Vector3 anchorPos = _cam.transform.TransformPoint(new Vector3(0f, -holdDrop, holdDistance));

            // Trailing weight: the prop lags behind when you walk or sprint.
            if (_player != null && inertiaLag > 0f)
            {
                Vector3 lag = -_player.Velocity * inertiaLag;
                anchorPos += Vector3.ClampMagnitude(lag, maxInertiaLag);
            }

            Vector3 wantPos = anchorPos - anchorOffset;

            float tp = 1f - Mathf.Exp(-positionSharpness * Time.fixedDeltaTime);
            float tr = 1f - Mathf.Exp(-levelSharpness * Time.fixedDeltaTime);

            Held.transform.position = Vector3.Lerp(Held.transform.position, wantPos, tp);
            Held.transform.rotation = Quaternion.Slerp(Held.transform.rotation, wantRot, tr);
        }

        /// <summary>Camera forward with the pitch clamped — used for throwing only.</summary>
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
            Vector3 bestPoint = Vector3.zero;
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
                bestPoint = h.point;
            }

            if (best != null)
            {
                _aimPoint = bestPoint;
                _hasAimPoint = true;
                return best;
            }

            if (Physics.SphereCast(origin, aimAssistRadius, dir, out var soft, range, mask, QueryTriggerInteraction.Ignore))
            {
                if (IsValidTarget(soft.collider))
                {
                    var p = soft.collider.GetComponentInParent<Prop>();
                    if (p != null && p.magnetic)
                    {
                        _aimPoint = soft.point;
                        _hasAimPoint = true;
                        return p;
                    }
                }
            }

            _hasAimPoint = false;
            return null;
        }

        bool IsValidTarget(Collider col)
        {
            if (col.transform.IsChildOf(transform)) return false;
            if (Held != null && col.transform.IsChildOf(Held.transform)) return false;

            // Do NOT exclude Aimed here. Rejecting the prop we are already aiming at makes
            // the aim latch flip on/off every frame (flickering highlight) and leaves Aimed
            // null on half the frames, so clicking does nothing.
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

            Vector3 hit = _hasAimPoint ? _aimPoint : Held.transform.position;
            _localAnchor = Held.transform.InverseTransformPoint(hit);
            _anchorScale = Held.transform.lossyScale;
            _grabYawOffset = Mathf.DeltaAngle(_cam.transform.eulerAngles.y, Held.transform.eulerAngles.y);
            _yawOffset = 0f;

            LastGrabOffset = (hit - Held.transform.position).magnitude;

            Held.BeginHold();
            SetIgnoreCollision(true);
            Used?.Invoke();

            Debug.Log($"[stick] grab {Held.name} offset={LastGrabOffset:0.00}m " +
                      $"localAnchor=({_localAnchor.x:0.00}, {_localAnchor.y:0.00}, {_localAnchor.z:0.00})");
        }

        void Drop()
        {
            if (Held == null) return;
            SetIgnoreCollision(false);
            Held.Drop();
            Held = null;
            Used?.Invoke();
        }

        void FreezeHeld()
        {
            if (Held == null) return;
            SetIgnoreCollision(false);
            Held.Freeze();
            Held = null;
            Used?.Invoke();
        }

        void ThrowHeld()
        {
            if (Held == null) return;
            SetIgnoreCollision(false);
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
            SetIgnoreCollision(false);
            Held.Drop();
            Held = null;
            SetAim(null);
        }

        /// <summary>Held props overlap your capsule when grabbed off-centre; without this the
        /// kinematic plank shoves you around while you carry it.</summary>
        void SetIgnoreCollision(bool ignore)
        {
            if (_selfCollider == null) _selfCollider = GetComponent<Collider>();
            if (_selfCollider == null || Held == null) return;

            var propCollider = Held.GetComponent<Collider>();
            if (propCollider != null) Physics.IgnoreCollision(_selfCollider, propCollider, ignore);
        }
    }
}
