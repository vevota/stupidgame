using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace StupidGame
{
    public enum ItemKind
    {
        Fists = 0,
        Magneto = 1,
    }

    /// <summary>
    /// Item slots plus their viewmodels. 1 = fists, 2 = magneto stick, wheel cycles.
    /// Viewmodels live on their own layer and are drawn by a second camera so they can
    /// never clip through level geometry.
    /// </summary>
    public class ItemController : MonoBehaviour
    {
        public const int ViewmodelLayer = 8;

        public ItemKind Current { get; private set; } = ItemKind.Magneto;
        public string CurrentName => Current == ItemKind.Magneto ? "magneto stick" : "fists";

        MagnetoStick _stick;
        Transform _vmRoot;
        GameObject _fistsVm;
        GameObject _magnetoVm;
        float _kick;

        public void Init(Camera cam, MagnetoStick stick)
        {
            _stick = stick;

            _vmRoot = new GameObject("ViewmodelRoot").transform;
            _vmRoot.SetParent(cam.transform, false);

            // Fists are intentionally an empty viewmodel for now.
            _fistsVm = new GameObject("Fists");
            _fistsVm.transform.SetParent(_vmRoot, false);

            _magnetoVm = BuildMagneto();
            _magnetoVm.transform.SetParent(_vmRoot, false);

            Select(ItemKind.Magneto);
        }

        public void Select(ItemKind kind)
        {
            if (_stick != null)
            {
                _stick.CancelHold();
                _stick.enabled = kind == ItemKind.Magneto;
            }

            Current = kind;
            if (_magnetoVm != null) _magnetoVm.SetActive(kind == ItemKind.Magneto);
            if (_fistsVm != null) _fistsVm.SetActive(kind == ItemKind.Fists);
        }

        public void Kick(float amount)
        {
            _kick = Mathf.Clamp(_kick + amount, 0f, 1f);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) Select(ItemKind.Fists);
                if (kb.digit2Key.wasPressedThisFrame) Select(ItemKind.Magneto);
            }

            var m = Mouse.current;
            if (m != null && Cursor.lockState == CursorLockMode.Locked)
            {
                // While a prop is held the wheel is push/pull, not item cycling.
                if (_stick != null && _stick.Held != null) return;

                float scroll = m.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    Select(Current == ItemKind.Fists ? ItemKind.Magneto : ItemKind.Fists);
                }
            }
        }

        void LateUpdate()
        {
            if (_magnetoVm == null || !_magnetoVm.activeSelf) return;

            _kick = Mathf.Lerp(_kick, 0f, 10f * Time.deltaTime);
            BasePose(out Vector3 basePos, out Quaternion baseRot);
            _magnetoVm.transform.localPosition = basePos + new Vector3(0f, -_kick * 0.02f, -_kick * 0.05f);
            _magnetoVm.transform.localRotation = baseRot * Quaternion.Euler(-_kick * 8f, 0f, 0f);
        }

        static void BasePose(out Vector3 pos, out Quaternion rot)
        {
            pos = new Vector3(0.26f, -0.24f, 0.42f);
            rot = Quaternion.Euler(8f, -16f, 20f);
        }

        static GameObject BuildMagneto()
        {
            var root = new GameObject("MagnetoStickVm");

            // Shaft: cylinder primitive is 2 units tall on Y, radius 0.5.
            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "shaft";
            shaft.transform.SetParent(root.transform, false);
            shaft.transform.localScale = new Vector3(0.030f, 0.21f, 0.030f);
            DisableCollider(shaft);
            Paint(shaft, new Color(0.30f, 0.28f, 0.26f));

            var grip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            grip.name = "grip";
            grip.transform.SetParent(root.transform, false);
            grip.transform.localPosition = new Vector3(0f, -0.30f, 0f);
            grip.transform.localScale = new Vector3(0.038f, 0.09f, 0.038f);
            DisableCollider(grip);
            Paint(grip, new Color(0.16f, 0.15f, 0.14f));

            var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = "tip";
            tip.transform.SetParent(root.transform, false);
            tip.transform.localPosition = new Vector3(0f, 0.44f, 0f);
            tip.transform.localScale = new Vector3(0.055f, 0.055f, 0.055f);
            DisableCollider(tip);
            Paint(tip, new Color(0.35f, 0.90f, 1.00f));

            return root;
        }

        static void DisableCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        static void Paint(GameObject go, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;

            SetLayerRecursive(go, ViewmodelLayer);
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
        }
    }
}
