// SUPERSEDED: this forced every held prop's long axis along the aim direction, which made
// planks look like they were always grabbed by the short end and snapped into place.
// MagnetoStick now anchors at the clicked point and follows yaw only.
// Kept (not deleted) in case we add an explicit "align to aim" key later.
using UnityEngine;

namespace StupidGame
{
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
