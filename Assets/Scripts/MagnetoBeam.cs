using UnityEngine;
using UnityEngine.Rendering;

namespace StupidGame
{
    /// <summary>
    /// World-space beam from the magneto stick to its target, with a bright tip at the
    /// impact point. Rendered by the main camera, so level geometry occludes it.
    /// </summary>
    public class MagnetoBeam : MonoBehaviour
    {
        static readonly Color HoldColor = new Color(0.45f, 0.92f, 1f);
        static readonly Color AimColor = new Color(0.30f, 0.62f, 0.78f);

        LineRenderer _line;
        Transform _tip;
        Material _mat;

        public void Init()
        {
            _mat = Materials.Unlit(HoldColor);

            _line = gameObject.AddComponent<LineRenderer>();
            _line.positionCount = 2;
            _line.useWorldSpace = true;
            _line.numCapVertices = 4;
            _line.textureMode = LineTextureMode.Stretch;
            _line.shadowCastingMode = ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.sharedMaterial = _mat;

            var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.name = "BeamTip";
            dot.transform.SetParent(transform, false);
            dot.transform.localScale = Vector3.one * 0.13f;

            var col = dot.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            var rend = dot.GetComponent<Renderer>();
            rend.sharedMaterial = _mat;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;

            _tip = dot.transform;
            Hide();
        }

        public void Show(Vector3 from, Vector3 to, float width, bool holding)
        {
            Color c = holding ? HoldColor : AimColor;
            _mat.SetColor("_BaseColor", c);
            _mat.SetColor("_Color", c);

            _line.enabled = true;
            _line.startWidth = width;
            _line.endWidth = width * 0.55f;
            _line.SetPosition(0, from);
            _line.SetPosition(1, to);

            _tip.gameObject.SetActive(true);
            _tip.position = to;
        }

        public void Hide()
        {
            if (_line != null) _line.enabled = false;
            if (_tip != null) _tip.gameObject.SetActive(false);
        }
    }
}
