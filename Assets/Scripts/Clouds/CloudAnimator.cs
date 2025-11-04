using System.Collections;
using UnityEngine;

[ExecuteAlways]
public class CloudAnimator : MonoBehaviour
{
    [Header("References")]
    public WeatherMap weatherMap;
    public NoiseGenerator noiseGenerator;

    [Header("Wind Settings")]
    public Vector3 windDirection = new Vector3(1, 0, 0);
    public float windSpeed = 10f;        // units per second
    public float verticalDrift = 0.1f;   // gentle lift/sink movement

    [Header("Weather Evolution")]
    public float weatherUpdateInterval = 5f;  // seconds between weather map updates
    public float weatherDriftSpeed = 0.005f;  // speed at which weather noise offset moves

    [Header("Cloud Dissipation")]
    [Range(0, 1)]
    public float baseDensity = 0.8f;
    [Range(0, 1)]
    public float targetDensity = 0.8f;
    public float densityChangeSpeed = 0.05f;

    // Internal state
    private Vector3 shapeOffsetAccum;
    private Vector3 detailOffsetAccum;
    private Vector2 weatherOffsetAccum;

    private float lastWeatherUpdateTime;

    void Start()
    {
        if (weatherMap == null) weatherMap = GetComponentInChildren<WeatherMap>();
        if (noiseGenerator == null) noiseGenerator = GetComponentInChildren<NoiseGenerator>();

        // Initialize offsets
        shapeOffsetAccum = Vector3.zero;
        detailOffsetAccum = Vector3.zero;
        weatherOffsetAccum = Vector2.zero;

        lastWeatherUpdateTime = -weatherUpdateInterval; // force initial update
    }

    void Update()
    {
        if (weatherMap == null || noiseGenerator == null)
            return;

        // Use real time even in Edit mode
        float time = Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;
        float dt = Application.isPlaying ? Time.deltaTime : 0.016f; // simulate 60 FPS in editor

        // --- Animate Noise Offsets ---
        Vector3 motion = windDirection.normalized * windSpeed * dt;
        motion.y += Mathf.Sin(time * 0.1f) * verticalDrift * dt;

        shapeOffsetAccum += motion * 0.02f;
        detailOffsetAccum += motion * 0.2f;

        UpdateNoiseOffsets();

        // --- Animate Weather Map ---
        if (time - lastWeatherUpdateTime >= weatherUpdateInterval)
        {
            weatherOffsetAccum += new Vector2(windDirection.x, windDirection.z) * weatherDriftSpeed;
            weatherMap.noiseSettings.offset = weatherOffsetAccum;
            weatherMap.UpdateMap();
            lastWeatherUpdateTime = time;
        }

        // --- Density Animation ---
        baseDensity = Mathf.Lerp(baseDensity, targetDensity, densityChangeSpeed * dt);
        Shader.SetGlobalFloat("_CloudBaseDensity", baseDensity);
    }


    void UpdateNoiseOffsets()
    {
        // Update offsets in shape and detail settings so compute shader sees movement
        foreach (var settings in noiseGenerator.shapeSettings)
        {
            if (settings == null) continue;
            settings.seed += 0; // keep same seed
            settings.tile = 1;
        }
        foreach (var settings in noiseGenerator.detailSettings)
        {
            if (settings == null) continue;
            settings.tile = 1;
        }

        // Tell NoiseGenerator to update GPU textures when parameters change
        noiseGenerator.ActiveNoiseSettingsChanged();
    }

    // Optional: Smooth density target changes
    public void SetTargetDensity(float newTarget)
    {
        targetDensity = Mathf.Clamp01(newTarget);
    }
}
