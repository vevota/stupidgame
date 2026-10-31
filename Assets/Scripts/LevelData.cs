using System;
using UnityEngine;

namespace StupidGame
{
    [Serializable]
    public struct Vec3
    {
        public float x, y, z;

        public Vec3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3 V => new Vector3(x, y, z);

        public override string ToString() => $"({x}, {y}, {z})";
    }

    [Serializable]
    public class BoxDef
    {
        public string name = "box";
        public Vec3 pos;
        public Vec3 size = new Vec3(1f, 1f, 1f);
        public string color = "ground";
    }

    [Serializable]
    public class PropDef
    {
        public string name = "prop";
        public Vec3 pos;
        public Vec3 size = new Vec3(3f, 0.25f, 0.9f);
        public float mass = 7f;
        public bool magnetic = true;
    }

    [Serializable]
    public class LevelData
    {
        public string name = "level";
        public float gravity = -30f;
        public float killY = -40f;
        public Vec3 start;
        public Vec3 goal;
        public BoxDef[] boxes = Array.Empty<BoxDef>();
        public PropDef[] props = Array.Empty<PropDef>();
    }
}
