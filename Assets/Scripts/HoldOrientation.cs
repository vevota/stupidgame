using UnityEngine;

namespace StupidGame
{
    /// <summary>
    /// Held props are oriented by convention: the prop's local +X runs along the aim
    /// direction and its local +Y is "up". Level props are authored with their long axis
    /// on X and their thin axis on Y, which is what makes a plank standable.
    /// </summary>
    public static class HoldOrientation
    {
        const float PlankAxisFix = -90f;

        public static Quaternion For(Vector3 aim)
        {
            Vector3 up = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(aim, up)) > 0.95f) up = Vector3.forward;
            return Quaternion.LookRotation(aim, up) * Quaternion.Euler(0f, PlankAxisFix, 0f);
        }
    }
}
