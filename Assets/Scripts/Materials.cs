using System.Collections.Generic;
using UnityEngine;

namespace StupidGame
{
    /// <summary>Flat greybox materials, created at runtime and cached by name.</summary>
    public static class Materials
    {
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        public static Color ColorFor(string key)
        {
            switch (key)
            {
                case "ground":    return new Color(0.20f, 0.21f, 0.24f);
                case "edge":      return new Color(0.31f, 0.33f, 0.37f);
                case "magnetic":  return new Color(0.56f, 0.64f, 0.74f);
                case "wood":      return new Color(0.45f, 0.33f, 0.22f);
                case "player":    return new Color(0.92f, 0.55f, 0.22f);
                case "goal":      return new Color(0.22f, 0.80f, 0.48f);
                default:          return Color.magenta;
            }
        }

        public static Material Get(string key)
        {
            if (Cache.TryGetValue(key, out var mat) && mat != null) return mat;
            mat = Make(ColorFor(key));
            Cache[key] = mat;
            return mat;
        }

        public static Material Unlit(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            return mat;
        }

        public static Material Make(Color c)
        {
            // URP project: this is the shader that exists. "Standard" is only a fallback
            // for the case where someone opens this in a built-in pipeline project.
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.12f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            return mat;
        }
    }
}
