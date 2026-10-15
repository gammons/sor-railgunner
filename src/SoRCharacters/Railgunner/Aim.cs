using UnityEngine;

namespace SoRCharacters.Railgunner
{
    internal static class Aim
    {
        public static Vector2 Direction(Agent a)
        {
            var gc = GameController.gameController;
            var origin = a.tr.position;
            var d = Vector2.zero;

            if (a.controllerType == "Keyboard" && !gc.sessionDataBig.trackpadMode)
            {
                var camera = a.agentCamera?.actualCamera;
                if (camera != null)
                {
                    var w = camera.ScreenCamera.ScreenToWorldPoint(Input.mousePosition);
                    d = new Vector2(w.x - origin.x, w.y - origin.y);
                }
            }
            else if (a.target.AttackTowardTarget())
            {
                d = a.target.tr.up;
            }
            else
            {
                var r = a.gun.FindWeaponAngleGamepad() * Mathf.Deg2Rad;
                d = new Vector2(Mathf.Cos(r), Mathf.Sin(r));
            }

            return d.sqrMagnitude < 1e-6f ? (Vector2)a.tr.right : d.normalized;
        }
    }
}
