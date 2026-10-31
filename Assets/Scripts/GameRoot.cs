using UnityEngine;
using UnityEngine.InputSystem;

namespace StupidGame
{
    /// <summary>
    /// Makes Play work in any scene without touching scene files.
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (!Application.isPlaying) return;
            if (Object.FindFirstObjectByType<GameRoot>() != null) return;
            new GameObject("StupidGame").AddComponent<GameRoot>();
        }
    }

    public class GameRoot : MonoBehaviour
    {
        public LevelData level;
        public PlayerMotor player;
        public int falls;
        public bool reachedGoal;
        public float runTime;

        Hud _hud;

        void Awake()
        {
            Time.fixedDeltaTime = 0.02f;
            Application.targetFrameRate = 120;

            var asset = Resources.Load<TextAsset>("levels/lvl01");
            if (asset == null)
            {
                Debug.LogError("[game] missing Assets/Resources/levels/lvl01.json");
                enabled = false;
                return;
            }

            level = JsonUtility.FromJson<LevelData>(asset.text);
            if (level == null)
            {
                Debug.LogError("[game] could not parse lvl01.json");
                enabled = false;
                return;
            }

            Physics.gravity = new Vector3(0f, level.gravity, 0f);

            LevelLoader.Build(level);

            player = SpawnPlayer(level.start.V);
            var cam = EnsureCamera(player.transform);
            player.CameraTransform = cam.transform;

            var stick = player.gameObject.AddComponent<MagnetoStick>();
            stick.Init(cam, player);

            _hud = gameObject.AddComponent<Hud>();
            _hud.Init(this, player, stick);

            BuildGoal(level.goal.V);
            gameObject.AddComponent<ScreenshotCapture>();

            Debug.Log($"[game] level '{level.name}' start={level.start} goal={level.goal} gravity={level.gravity}");
        }

        void Update()
        {
            if (player == null) return;

            if (!reachedGoal) runTime += Time.deltaTime;

            if (player.transform.position.y < level.killY) Respawn();

            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) Respawn();
        }

        public void Respawn()
        {
            player.Teleport(level.start.V);
            falls++;
            if (_hud != null) _hud.Flash("fell - climb back up");
        }

        public void OnGoalReached()
        {
            if (reachedGoal) return;
            reachedGoal = true;
            if (_hud != null) _hud.Flash($"GOAL in {runTime:0.0}s with {falls} falls");
            Debug.Log($"[game] GOAL reached in {runTime:0.0}s, falls={falls}");
        }

        PlayerMotor SpawnPlayer(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player";
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            go.GetComponent<Renderer>().sharedMaterial = Materials.Get("player");

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 1f;

            return go.AddComponent<PlayerMotor>();
        }

        Camera EnsureCamera(Transform target)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }

            var follow = cam.GetComponent<FollowCamera>();
            if (follow == null) follow = cam.gameObject.AddComponent<FollowCamera>();
            follow.Init(target);
            return cam;
        }

        void BuildGoal(Vector3 pos)
        {
            var go = new GameObject("Goal");
            go.transform.position = pos;

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(2.5f, 3f, 2.5f);

            var trigger = go.AddComponent<GoalTrigger>();
            trigger.root = this;
        }
    }

    public class GoalTrigger : MonoBehaviour
    {
        public GameRoot root;

        void OnTriggerEnter(Collider other)
        {
            if (root == null) return;
            if (other.GetComponentInParent<PlayerMotor>() != null) root.OnGoalReached();
        }
    }
}
