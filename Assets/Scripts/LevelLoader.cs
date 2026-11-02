using UnityEngine;

namespace StupidGame
{
    /// <summary>Turns a LevelData into scene objects. Everything is spawned at runtime.</summary>
    public static class LevelLoader
    {
        public static GameObject Build(LevelData level)
        {
            var root = new GameObject("Level");

            foreach (var b in level.boxes)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = b.name;
                go.transform.SetParent(root.transform, false);
                go.transform.position = b.pos.V;
                go.transform.localScale = b.size.V;
                go.GetComponent<Renderer>().sharedMaterial = Materials.Get(b.color);
            }

            foreach (var p in level.props)
            {
                SpawnProp(root.transform, p);
            }

            return root;
        }

        static void SpawnProp(Transform parent, PropDef def)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = def.name;
            go.transform.SetParent(parent, false);
            go.transform.position = def.pos.V;
            go.transform.localScale = def.size.V;
            go.transform.rotation = Quaternion.Euler(0f, def.rotY, 0f);

            go.AddComponent<Rigidbody>();

            var prop = go.AddComponent<Prop>();
            prop.Configure(def.mass, def.magnetic, Materials.ColorFor(def.magnetic ? "magnetic" : "wood"));
        }
    }
}
