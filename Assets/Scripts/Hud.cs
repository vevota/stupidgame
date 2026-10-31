using UnityEngine;

namespace StupidGame
{
    public class Hud : MonoBehaviour
    {
        GameRoot _root;
        PlayerMotor _player;
        MagnetoStick _stick;
        ItemController _items;

        GUIStyle _style;
        GUIStyle _small;
        string _flash;
        float _flashUntil;

        public void Init(GameRoot root, PlayerMotor player, MagnetoStick stick, ItemController items)
        {
            _root = root;
            _player = player;
            _stick = stick;
            _items = items;
        }

        public void Flash(string message)
        {
            _flash = message;
            _flashUntil = Time.time + 2.5f;
        }

        void OnGUI()
        {
            if (_root == null || _root.level == null) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 18 };
                _style.normal.textColor = Color.white;
                _small = new GUIStyle(GUI.skin.label) { fontSize = 13 };
                _small.normal.textColor = new Color(1f, 1f, 1f, 0.65f);
            }

            string held = "-";
            if (_stick != null)
            {
                if (_stick.Held != null) held = _stick.Held.name;
                else if (_stick.Aimed != null) held = "aim: " + _stick.Aimed.name;
            }

            string item = _items != null ? _items.CurrentName : "-";
            bool lockFail = Cursor.lockState != CursorLockMode.Locked;

            string text =
                $"{_root.level.name}   t={_root.runTime:0.0}s   falls={_root.falls}\n" +
                $"item={item}   held={held}   grounded={_player.Grounded}" +
                (lockFail ? "\n\n[click to capture mouse]" : "");

            if (_root.reachedGoal) text += "\n\nGOAL REACHED";
            if (!string.IsNullOrEmpty(_flash) && Time.time < _flashUntil) text += "\n\n" + _flash;

            GUI.Label(new Rect(16f, 12f, 760f, 140f), text, _style);

            if (!lockFail)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.75f);
                GUI.DrawTexture(new Rect(Screen.width * 0.5f - 2f, Screen.height * 0.5f - 2f, 4f, 4f), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            GUI.Label(
                new Rect(16f, Screen.height - 34f, 1100f, 28f),
                "WASD move   Space jump   1 fists   2 magneto   wheel cycle   LMB grab/drop   RMB freeze   F throw   R respawn   Esc free cursor",
                _small);
        }
    }
}
