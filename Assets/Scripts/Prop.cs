using UnityEngine;

namespace StupidGame
{
    /// <summary>
    /// A physics prop. Magnetic props can be grabbed and frozen in midair by the
    /// magneto stick; non-magnetic props obey gravity forever.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    public class Prop : MonoBehaviour
    {
        public float mass = 7f;
        public bool magnetic = true;
        public Color baseColor = Color.grey;

        public bool IsFrozen { get; private set; }
        public bool IsHeld { get; private set; }

        static readonly Color FrozenTint = new Color(0.55f, 0.86f, 1.00f);
        static readonly Color HighlightTint = new Color(1.00f, 0.84f, 0.35f);

        Rigidbody _rb;
        Renderer _renderer;
        MaterialPropertyBlock _mpb;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _renderer = GetComponent<Renderer>();
            _mpb = new MaterialPropertyBlock();
        }

        public void Configure(float m, bool isMagnetic, Color color)
        {
            mass = m;
            magnetic = isMagnetic;
            baseColor = color;

            if (_rb == null) _rb = GetComponent<Rigidbody>();
            _rb.mass = m;
            _rb.linearDamping = 0.05f;
            _rb.angularDamping = 0.4f;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Tint(color);
        }

        public void SetHighlight(bool on)
        {
            if (IsFrozen || IsHeld) { Tint(IsFrozen ? FrozenTint : baseColor); return; }
            Tint(on ? HighlightTint : baseColor);
        }

        public void BeginHold()
        {
            IsHeld = true;
            IsFrozen = false;
            _rb.isKinematic = true;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            Tint(baseColor);
        }

        public void EndHold()
        {
            IsHeld = false;
            _rb.isKinematic = false;
        }

        public void Throw(Vector3 velocity)
        {
            EndHold();
            _rb.linearVelocity = velocity;
            _rb.angularVelocity = Random.insideUnitSphere * 1.5f;
        }

        public void Freeze()
        {
            IsHeld = false;
            IsFrozen = true;
            _rb.isKinematic = true;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            Tint(FrozenTint);
        }

        public void Unfreeze()
        {
            IsFrozen = false;
            _rb.isKinematic = false;
            Tint(baseColor);
        }

        /// <summary>Drops the prop without throwing it.</summary>
        public void Drop()
        {
            IsHeld = false;
            _rb.isKinematic = false;
        }

        void Tint(Color c)
        {
            if (_renderer == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", c);
            _mpb.SetColor("_Color", c);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
