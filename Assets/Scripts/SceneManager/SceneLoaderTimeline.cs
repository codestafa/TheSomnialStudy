using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

namespace SceneManagement.Timeline
{
    /// <summary>
    /// Signal Receiver for loading scenes from Timeline Signals
    /// Add this to any GameObject that has a Signal Receiver component
    /// </summary>
    public class SceneLoaderSignalReceiver : MonoBehaviour
    {
        [Header("Scene Loader Reference")]
        [SerializeField] private SceneLoader sceneLoader;

        [Header("Optional: Direct Scene Settings")]
        [SerializeField] private bool useDirectLoading = false;
        [SerializeField] private string sceneName;

        private void Awake()
        {
            // If no scene loader is assigned, try to find one on this GameObject
            if (sceneLoader == null)
            {
                sceneLoader = GetComponent<SceneLoader>();
            }
        }

        /// <summary>
        /// Load scene - call this from a Timeline Signal
        /// </summary>
        public void LoadSceneFromTimeline()
        {
            if (useDirectLoading && !string.IsNullOrEmpty(sceneName))
            {
                SceneLoader.LoadSceneStatic(sceneName);
            }
            else if (sceneLoader != null)
            {
                sceneLoader.LoadScene();
            }
            else
            {
                Debug.LogError($"No SceneLoader assigned and useDirectLoading is false on {gameObject.name}!");
            }
        }

        /// <summary>
        /// Load a specific scene by name - call this from a Timeline Signal
        /// </summary>
        public void LoadSceneByName(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                SceneLoader.LoadSceneStatic(name);
            }
            else
            {
                Debug.LogError("Scene name is empty!");
            }
        }

        /// <summary>
        /// Reload current scene - call this from a Timeline Signal
        /// </summary>
        public void ReloadCurrentScene()
        {
            if (sceneLoader != null)
            {
                sceneLoader.ReloadCurrentScene();
            }
            else
            {
                SceneLoader.LoadSceneStatic(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }
    }

    /// <summary>
    /// Custom Timeline Track for scene loading
    /// Allows you to schedule scene loads directly in Timeline
    /// </summary>
    [TrackColor(0.2f, 0.8f, 0.2f)]
    [TrackClipType(typeof(SceneLoaderClip))]
    [TrackBindingType(typeof(SceneLoader))]
    public class SceneLoaderTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<SceneLoaderMixerBehaviour>.Create(graph, inputCount);
        }
    }

    /// <summary>
    /// Clip for the SceneLoaderTrack
    /// </summary>
    public class SceneLoaderClip : PlayableAsset
    {
        public SceneLoaderBehaviour template = new SceneLoaderBehaviour();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<SceneLoaderBehaviour>.Create(graph, template);
            return playable;
        }
    }

    /// <summary>
    /// Behaviour for the SceneLoaderClip
    /// </summary>
    [System.Serializable]
    public class SceneLoaderBehaviour : PlayableBehaviour
    {
        public string sceneName;
        public bool loadOnStart = true;
        public bool loadOnEnd = false;

        private bool hasLoadedOnStart = false;
        private bool hasLoadedOnEnd = false;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (loadOnStart && !hasLoadedOnStart && !string.IsNullOrEmpty(sceneName))
            {
                SceneLoader.LoadSceneStatic(sceneName);
                hasLoadedOnStart = true;
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (loadOnEnd && !hasLoadedOnEnd && !string.IsNullOrEmpty(sceneName))
            {
                SceneLoader.LoadSceneStatic(sceneName);
                hasLoadedOnEnd = true;
            }
        }

        public override void OnGraphStop(Playable playable)
        {
            hasLoadedOnStart = false;
            hasLoadedOnEnd = false;
        }
    }

    /// <summary>
    /// Mixer behaviour for the SceneLoaderTrack
    /// </summary>
    public class SceneLoaderMixerBehaviour : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            SceneLoader trackBinding = playerData as SceneLoader;

            if (trackBinding == null)
                return;

            int inputCount = playable.GetInputCount();

            for (int i = 0; i < inputCount; i++)
            {
                float inputWeight = playable.GetInputWeight(i);
                ScriptPlayable<SceneLoaderBehaviour> scriptPlayable = (ScriptPlayable<SceneLoaderBehaviour>)playable.GetInput(i);
                SceneLoaderBehaviour input = scriptPlayable.GetBehaviour();

                // You can add additional processing here if needed
            }
        }
    }
}
