using UnityEngine;
using UnityEngine.InputSystem;

namespace StupidGame
{
    /// <summary>Boots the game in any scene without touching scene files.</summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (!Application.isPlaying) return;
            if (Object.FindAnyObjectByType<GameRoot>() != null) return;
            new GameObject("StupidGame").AddComponent<GameRoot>();
        }
    }

    public class GameRoot : MonoBehaviour
    {
        public LevelData level;
        public PlayerMotor player;
        public ItemController items;
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
            var cam = SetupFirstPersonCamera(player.transform);
            player.CameraTransform = cam.transform;

            var stick = player.gameObject.AddComponent<MagnetoStick>();
            stick.Init(cam);

            items = player.gameObject.AddComponent<ItemController>();
            items.Init(cam, stick);
            stick.Used += () => items.Kick(1f);

            _hud = gameObject.AddComponent<Hud>();
            _hud.Init(this, player, stick, items);

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
            // Face the canyon (+X) so the first thing you see is the gap.
            go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 1f;

            return go.AddComponent<PlayerMotor>();
        }

        Camera SetupFirstPersonCamera(Transform playerTransform)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }

            cam.transform.SetParent(playerTransform, false);
            cam.transform.localPosition = new Vector3(0f, 0.68f, 0f);
            cam.transform.localRotation = Quaternion.identity;
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.03f;
            cam.cullingMask = ~(1 << ItemController.ViewmodelLayer);

            var look = cam.GetComponent<FirstPersonLook>();
            if (look == null) look = cam.gameObject.AddComponent<FirstPersonLook>();
            look.Init(playerTransform);

            // Second camera draws only the viewmodel layer, clearing depth so it sits on top
            // of the world without ever clipping into it.
            var vmGo = new GameObject("ViewModelCamera");
            vmGo.transform.SetParent(cam.transform, false);
            var vm = vmGo.AddComponent<Camera>();
            vm.clearFlags = CameraClearFlags.Depth;
            vm.cullingMask = 1 << ItemController.ViewmodelLayer;
            vm.depth = 1;
            vm.fieldOfView = 55f;
            vm.nearClipPlane = 0.01f;
            vm.farClipPlane = 5f;

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
