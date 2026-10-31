using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace StupidGame
{
    /// <summary>
    /// Headless/automated capture. Works in a built player:
    ///   -screenshot /path/frame.png -screenshotDelay 3 -quitAfter 6
    /// </summary>
    public class ScreenshotCapture : MonoBehaviour
    {
        string _path;
        float _delay = 3f;
        float _quitAfter = 0f;

        void Awake()
        {
            var args = Environment.GetCommandLineArgs();
            _path = Arg(args, "-screenshot");
            _delay = ParseFloat(Arg(args, "-screenshotDelay"), 3f);
            _quitAfter = ParseFloat(Arg(args, "-quitAfter"), 0f);

            if (!string.IsNullOrEmpty(_path)) StartCoroutine(Capture());
            else if (_quitAfter > 0f) StartCoroutine(QuitLater(_quitAfter));
        }

        static string Arg(string[] args, string key)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == key) return args[i + 1];
            }
            return null;
        }

        static float ParseFloat(string s, float fallback)
        {
            if (string.IsNullOrEmpty(s)) return fallback;
            return float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : fallback;
        }

        IEnumerator Capture()
        {
            yield return new WaitForSecondsRealtime(_delay);

            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            ScreenCapture.CaptureScreenshot(_path);
            Debug.Log($"[game] screenshot -> {_path}");

            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit();
        }

        IEnumerator QuitLater(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            Application.Quit();
        }
    }
}
