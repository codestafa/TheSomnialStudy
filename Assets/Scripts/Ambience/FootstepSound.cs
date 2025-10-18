using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FootstepSound : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private LayerMask FloorLayer;
    [SerializeField] private bool DebugMode = true;

    [Header("Footstep Distance Settings")]
    [Range(0.3f, 2f)][SerializeField] private float walkStepLength = 1.2f;
    [Range(0.3f, 2f)][SerializeField] private float runStepLength = 0.7f;

    [Header("Peak Detection")]
    [Range(2, 20)][SerializeField] private int peakPointCount = 8;
    [Range(0.01f, 0.5f)][SerializeField] private float transientThreshold = 0.15f;

    [Header("Surface Sounds")]
    [SerializeField] private TextureSound[] TextureSounds;

    private CharacterController controller;
    private PlayerMovement playerMovement;

    private bool leftFoot = true;
    private float startTime;
    private float[] sampleBuffer;
    private readonly Dictionary<AudioClip, float[]> cachedPeaks = new();

    private Vector3 lastPosition;
    private float smoothedSpeed;
    private float distanceAccumulator;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();

        if (!playerMovement)
            LogDebug("[WARNING] PlayerMovement not found! Using fallback speeds.", "#FFA500");
    }

    private void Start()
    {
        startTime = Time.time;
        sampleBuffer = new float[44100 * 5];
        lastPosition = transform.position;

        LogDebug($"Initialized with {TextureSounds.Length} texture sound sets.");

        foreach (var ts in TextureSounds)
            foreach (var clip in ts.Clips)
                if (clip) CacheTransientPeaks(clip);

        StartCoroutine(CheckGround());
    }

    private IEnumerator CheckGround()
    {
        while (true)
        {
            Vector3 currentPos = transform.position;
            float frameDistance = Vector3.Distance(currentPos, lastPosition);
            lastPosition = currentPos;

            float currentSpeed = frameDistance / Time.deltaTime;
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, currentSpeed, 0.15f);

            float walkSpeed = playerMovement ? playerMovement.GetMovementSpeed() : 3f;
            float runSpeed = playerMovement ? walkSpeed * playerMovement.GetRunMultiplier() : 6f;
            float t = Mathf.InverseLerp(walkSpeed, runSpeed, smoothedSpeed);
            float currentStepLength = Mathf.Lerp(walkStepLength, runStepLength, t);

            if (controller.isGrounded && smoothedSpeed > 0.2f)
            {
                distanceAccumulator += frameDistance;

                if (distanceAccumulator >= currentStepLength)
                {
                    distanceAccumulator = 0f;
                    Vector3 origin = transform.position - new Vector3(0, controller.height * 0.5f - controller.radius, 0);

                    if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1.5f))
                    {
                        int hitLayer = hit.collider.gameObject.layer;
                        string hitLayerName = LayerMask.LayerToName(hitLayer);
                        bool layerAllowed = ((1 << hitLayer) & FloorLayer.value) != 0;
                        string color = layerAllowed ? "#00FF00" : "#FF0000";

                        LogDebug($"Raycast hit <b>{hit.collider.name}</b> | Layer: <b>{hitLayerName} ({hitLayer})</b> | InMask: <b>{layerAllowed}</b>", color);

                        if (layerAllowed)
                        {
                            if (hit.collider.TryGetComponent<Terrain>(out Terrain terrain))
                                yield return PlayFromTerrain(terrain, hit.point);
                            else if (hit.collider.TryGetComponent<Renderer>(out Renderer rend))
                                yield return PlayFromRenderer(rend, hit.point);
                        }
                    }
                    leftFoot = !leftFoot;
                }
            }
            else distanceAccumulator = 0f;

            yield return null;
        }
    }

    private IEnumerator PlayFromTerrain(Terrain terrain, Vector3 hitPoint)
    {
        TerrainData data = terrain.terrainData;
        Vector3 terrainPos = hitPoint - terrain.transform.position;
        int x = Mathf.FloorToInt((terrainPos.x / data.size.x) * data.alphamapWidth);
        int z = Mathf.FloorToInt((terrainPos.z / data.size.z) * data.alphamapHeight);
        float[,,] alpha = data.GetAlphamaps(x, z, 1, 1);

        int dominant = 0;
        for (int i = 1; i < alpha.Length; i++)
            if (alpha[0, 0, i] > alpha[0, 0, dominant])
                dominant = i;

        Texture tex = data.terrainLayers[dominant].diffuseTexture;
        foreach (var ts in TextureSounds)
        {
            if (ts.Albedo == tex)
            {
                LogDebug($"Terrain '{tex.name}' surface detected.");
                yield return PlaySlice(ts, hitPoint);
                yield break;
            }
        }
    }

    private IEnumerator PlayFromRenderer(Renderer rend, Vector3 hitPoint)
    {
        Texture tex = rend.material.GetTexture("_MainTex");
        foreach (var ts in TextureSounds)
        {
            if (ts.Albedo == tex)
            {
                LogDebug($"Renderer surface '{tex.name}' detected.");
                yield return PlaySlice(ts, hitPoint);
                yield break;
            }
        }
    }

    private IEnumerator PlaySlice(TextureSound ts, Vector3 footPosition)
    {
        if (ts.Clips.Length == 0) yield break;
        AudioClip clip = ts.Clips[Random.Range(0, ts.Clips.Length)];
        float[] peaks = cachedPeaks.TryGetValue(clip, out float[] arr) ? arr : new float[] { 0f };
        float startTime = peaks.Length > 0 ? peaks[Random.Range(0, peaks.Length)] : 0f;

        float randomPitch = Random.Range(0.93f, 1.07f);
        float randomVolume = Random.Range(0.3f, 0.5f);
        float randomPan = leftFoot ? Random.Range(-0.1f, -0.05f) : Random.Range(0.05f, 0.1f);

        // Offset to player’s feet area
        Vector3 soundPos = footPosition + new Vector3(
            leftFoot ? -0.15f : 0.15f, // slight left/right separation
            0.05f,                     // just above the ground
            0f
        );

        // Play sound spatially at foot position
        AudioSource.PlayClipAtPoint(clip, soundPos, randomVolume);

        LogDebug($"[{clip.name}] vol={randomVolume:F2}, pitch={randomPitch:F2}, pos={soundPos}, pan={randomPan:F2}");

        yield return new WaitForSeconds(clip.length * 0.35f);
    }

    private void CacheTransientPeaks(AudioClip clip)
    {
        if (!clip || cachedPeaks.ContainsKey(clip)) return;
        int totalSamples = Mathf.Min(clip.samples * clip.channels, sampleBuffer.Length);
        clip.GetData(sampleBuffer, 0);
        int window = 512;
        List<(float t, float a)> peaks = new();

        for (int i = 0; i < totalSamples - window; i += window)
        {
            float rms = 0f;
            for (int j = 0; j < window; j++) rms += sampleBuffer[i + j] * sampleBuffer[i + j];
            rms = Mathf.Sqrt(rms / window);
            if (rms >= transientThreshold) peaks.Add(((float)i / (clip.frequency * clip.channels), rms));
        }

        if (peaks.Count == 0)
            peaks = Enumerable.Range(0, peakPointCount).Select(i => ((float)i / peakPointCount * clip.length, 0.5f)).ToList();

        cachedPeaks[clip] = peaks.OrderByDescending(p => p.a).Take(peakPointCount).Select(p => p.t).ToArray();
        LogDebug($"{clip.name}: cached {cachedPeaks[clip].Length} peaks.");
    }

    private void LogDebug(string msg, string color = "#00FFFF")
    {
        if (!DebugMode) return;
        float elapsed = Time.time - startTime;
        Debug.Log($"<color={color}>[{elapsed:F2}s] {msg}</color>");
    }

    [System.Serializable]
    private class TextureSound
    {
        public Texture Albedo;
        public AudioClip[] Clips;
    }
}
