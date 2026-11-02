using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StupidGame
{
    /// <summary>
    /// The magneto stick. Only enabled while the magneto item is equipped.
    /// LMB grab/drop, RMB freeze/unfreeze, F throw, Q/E spin the held prop, V levels it.
    ///
    /// The clicked point is the pivot. The prop is rigidly attached to the camera by that
    /// point, so grabbing a plank near its end swings very differently from grabbing it in
    /// the middle. Nothing is re-centred and no axis is forced to the aim direction.
    /// </summary>
    public class MagnetoStick : MonoBehaviour
    {
        public float range = 7f;
        public float holdDistance = 2.4f;
        public float holdDrop = 0.12f;
        public float followSharpness = 16f;
        public float throwSpeed = 12f;
        public float aimAssistRadius = 0.18f;
        public float spinSpeed = 120f;

        public Prop Held { get; private set; }
        public Prop Aimed { get; private set; }

        /// <summary>"level" keeps the held prop horizontal; rigid lets pitch tilt it.</summary>
        public bool LevelGrip { get; private set; }

        /// <summary>Distance from the prop's origin to the point you grabbed. Shown in the HUD.</summary>
        public float LastGrabOffset { get; private set; }

        /// <summary>Fired when the stick grabs, freezes or throws — used for viewmodel kick.</summary>
        public event Action Used;

        Camera _cam;
        Collider _selfCollider;

        // Grab state, captured at pickup.
        Vector3 _localAnchor;
        Vector3 _anchorScale;
        Quaternion _grabRotInCam;
        float _grabWorldYaw;
        float _grabCamYaw;
        float _yawOffset;

        Vector3 _aimPoint;
        bool _hasAimPoint;

        public void Init(Camera cam)
        {
            _cam = cam;
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
            if (kb.vKey.wasPressedThisFrame) LevelGrip = !LevelGrip;

            if (Held != null)
            {
                if (kb.qKey.isPressed) _yawOffset -= spinSpeed * Time.deltaTime;
                if (kb.eKey.isPressed) _yawOffset += spinSpeed * Time.deltaTime;
            }
        }

        void FixedUpdate()
        {
            if (Held == null) return;

            Quaternion wantRot;
            if (LevelGrip)
            {
                // Horizontal only: the grab point still pivots, but the plank stays flat.
                float yaw = _grabWorldYaw
                            + Mathf.DeltaAngle(_grabCamYaw, _cam.transform.eulerAngles.y)
                            + _yawOffset;
                wantRot = Quaternion.Euler(0f, yaw, 0f);
            }
            else
            {
                // Rigid: the grabbed point is a real pivot, pitch included.
                wantRot = Quaternion.Euler(0f, _yawOffset, 0f)
                          * (_cam.transform.rotation * _grabRotInCam);
            }

            // Rotate the local anchor into world space at the prop's current size, so the
            // pivot stays exactly under the point we clicked.
            Vector3 anchorOffset = wantRot * Vector3.Scale(_localAnchor, _anchorScale);
            Vector3 anchorPos = _cam.transform.TransformPoint(new Vector3(0f, -holdDrop, holdDistance));
            Vector3 wantPos = anchorPos - anchorOffset;

            float t = 1f - Mathf.Exp(-followSharpness * Time.fixedDeltaTime);
            Held.transform.position = Vector3.Lerp(Held.transform.position, wantPos, t);
            Held.transform.rotation = Quaternion.Slerp(Held.transform.rotation, wantRot, t);
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
            _grabRotInCam = Quaternion.Inverse(_cam.transform.rotation) * Held.transform.rotation;
            _grabWorldYaw = Held.transform.eulerAngles.y;
            _grabCamYaw = _cam.transform.eulerAngles.y;
            _yawOffset = 0f;

            LastGrabOffset = (hit - Held.transform.position).magnitude;

            Held.BeginHold();
            SetIgnoreCollision(true);
            Used?.Invoke();

            Debug.Log($"[stick] grab {Held.name} offset={LastGrabOffset:0.00}m " +
                      $"localAnchor=({_localAnchor.x:0.00}, {_localAnchor.y:0.00}, {_localAnchor.z:0.00}) " +
                      $"level={LevelGrip}");
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
