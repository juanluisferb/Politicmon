using Politicmon.Audio;
using Politicmon.UI;
using UnityEngine;

namespace Politicmon.Core
{
    /// <summary>
    /// Entry point. Lives in Scenes/Main.unity, but if you press Play in any other scene it is
    /// created automatically. Sets up the camera, managers and shows the title screen.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrap : MonoBehaviour
    {
        static GameBootstrap instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureBootstrap()
        {
            if (instance == null && FindAnyObjectByType<GameBootstrap>() == null)
                new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
        }

        void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            GameData.Load();
            ScreenScaler.Refresh(true);

            var cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
            }
            if (cam.GetComponent<PixelCamera>() == null)
                cam.gameObject.AddComponent<PixelCamera>();
            DontDestroyOnLoad(cam.gameObject);

            CreateManager<AudioManager>("Audio");
            CreateManager<UIManager>("UI");
            CreateManager<TouchControls>("TouchControls");
            GameFlow.Initialize(transform);
        }

        void CreateManager<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<T>();
        }

        void Start() => GameFlow.ShowTitle();

        void Update()
        {
            ScreenScaler.Refresh();
            GameInput.Update(Time.unscaledDeltaTime);
            GameState.Tick(Time.unscaledDeltaTime);
        }
    }
}
