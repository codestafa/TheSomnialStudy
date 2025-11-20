using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Custom Timeline clip for camera shake effects
/// This allows you to add camera shake directly in Timeline with full control
/// </summary>
[System.Serializable]
public class CameraShakeClip : PlayableAsset, ITimelineClipAsset
{
    [Header("Shake Settings")]
    public float intensity = 1f;
    [Range(0.1f, 2f)]
    public float magnitude = 0.5f;
    [Range(1f, 50f)]
    public float roughness = 10f;
    
    [Header("Fade Settings")]
    public bool fadeIn = false;
    public bool fadeOut = true;

    public ClipCaps clipCaps
    {
        get { return ClipCaps.Blending; }
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<CameraShakeBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();

        behaviour.intensity = intensity;
        behaviour.magnitude = magnitude;
        behaviour.roughness = roughness;
        behaviour.fadeIn = fadeIn;
        behaviour.fadeOut = fadeOut;

        return playable;
    }
}

/// <summary>
/// Behaviour for the camera shake clip
/// Handles the actual shake logic during Timeline playback
/// </summary>
public class CameraShakeBehaviour : PlayableBehaviour
{
    public float intensity;
    public float magnitude;
    public float roughness;
    public bool fadeIn;
    public bool fadeOut;

    private Vector3 originalPosition;
    private Transform cameraTransform;
    private float currentIntensity;

    public override void OnPlayableCreate(Playable playable)
    {
        currentIntensity = 0f;
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (cameraTransform == null)
        {
            var camera = Camera.main;
            if (camera != null)
            {
                cameraTransform = camera.transform;
                originalPosition = cameraTransform.localPosition;
            }
        }
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (cameraTransform == null) return;

        float time = (float)playable.GetTime();
        float duration = (float)playable.GetDuration();
        float normalizedTime = time / duration;

        // Calculate intensity with fade in/out
        currentIntensity = intensity;

        if (fadeIn && normalizedTime < 0.2f)
        {
            currentIntensity *= (normalizedTime / 0.2f);
        }

        if (fadeOut && normalizedTime > 0.8f)
        {
            currentIntensity *= (1f - (normalizedTime - 0.8f) / 0.2f);
        }

        // Apply shake
        Vector3 shakeOffset = new Vector3(
            Mathf.PerlinNoise(time * roughness, 0f) - 0.5f,
            Mathf.PerlinNoise(0f, time * roughness) - 0.5f,
            Mathf.PerlinNoise(time * roughness, time * roughness) - 0.5f
        ) * magnitude * currentIntensity;

        cameraTransform.localPosition = originalPosition + shakeOffset;
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (cameraTransform != null)
        {
            cameraTransform.localPosition = originalPosition;
        }
    }
}

/// <summary>
/// Custom Timeline track for camera shake
/// Add this track to your Timeline to create camera shake clips
/// </summary>
[TrackColor(0.9f, 0.2f, 0.2f)]
[TrackClipType(typeof(CameraShakeClip))]
[TrackBindingType(typeof(Camera))]
public class CameraShakeTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<CameraShakeMixerBehaviour>.Create(graph, inputCount);
    }
}

/// <summary>
/// Mixer for blending multiple camera shake clips
/// </summary>
public class CameraShakeMixerBehaviour : PlayableBehaviour
{
    private Vector3 originalPosition;
    private Transform cameraTransform;
    private bool firstFrameHappened;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        Camera trackBinding = playerData as Camera;

        if (trackBinding == null)
            return;

        if (!firstFrameHappened)
        {
            cameraTransform = trackBinding.transform;
            originalPosition = cameraTransform.localPosition;
            firstFrameHappened = true;
        }

        Vector3 accumulatedShake = Vector3.zero;
        float totalWeight = 0f;

        int inputCount = playable.GetInputCount();

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            if (inputWeight > 0)
            {
                ScriptPlayable<CameraShakeBehaviour> inputPlayable = 
                    (ScriptPlayable<CameraShakeBehaviour>)playable.GetInput(i);
                CameraShakeBehaviour input = inputPlayable.GetBehaviour();

                // Calculate shake for this input
                float time = (float)inputPlayable.GetTime();
                Vector3 shake = new Vector3(
                    Mathf.PerlinNoise(time * input.roughness, 0f) - 0.5f,
                    Mathf.PerlinNoise(0f, time * input.roughness) - 0.5f,
                    Mathf.PerlinNoise(time * input.roughness, time * input.roughness) - 0.5f
                ) * input.magnitude * input.intensity;

                accumulatedShake += shake * inputWeight;
                totalWeight += inputWeight;
            }
        }

        if (totalWeight > 0)
        {
            cameraTransform.localPosition = originalPosition + accumulatedShake;
        }
        else
        {
            cameraTransform.localPosition = originalPosition;
        }
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        if (cameraTransform != null)
        {
            cameraTransform.localPosition = originalPosition;
        }
        firstFrameHappened = false;
    }
}
