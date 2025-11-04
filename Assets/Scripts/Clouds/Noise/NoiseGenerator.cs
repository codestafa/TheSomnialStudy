using System.Collections.Generic;
using UnityEngine;

public class NoiseGenerator : MonoBehaviour
{
    const int computeThreadGroupSize = 8;
    public const string detailNoiseName = "DetailNoise";
    public const string shapeNoiseName = "ShapeNoise";

    public enum CloudNoiseType { Shape, Detail }
    public enum TextureChannel { R, G, B, A }

    [Header("Editor Settings")]
    public CloudNoiseType activeTextureType;
    public TextureChannel activeChannel;
    public bool autoUpdate;
    public bool logComputeTime;

    [Header("Noise Settings")]
    public int shapeResolution = 132;
    public int detailResolution = 32;

    public WorleyNoiseSettings[] shapeSettings;
    public WorleyNoiseSettings[] detailSettings;
    public ComputeShader noiseCompute;
    public ComputeShader copy;

    [Header("Viewer Settings")]
    public bool viewerEnabled;
    public bool viewerGreyscale = true;
    public bool viewerShowAllChannels;
    [Range(0, 1)] public float viewerSliceDepth;
    [Range(1, 5)] public float viewerTileAmount = 1;
    [Range(0, 1)] public float viewerSize = 1;

    // Internal
    List<ComputeBuffer> buffersToRelease;
    bool updateNoise;

    [HideInInspector] public bool showSettingsEditor = true;
    [SerializeField, HideInInspector] public RenderTexture shapeTexture;
    [SerializeField, HideInInspector] public RenderTexture detailTexture;

    // ────────────────────────────────────────────────
    // PUBLIC METHODS
    // ────────────────────────────────────────────────

    public void ManualUpdate()
    {
        updateNoise = true;
        UpdateNoise();
    }

    public void UpdateNoise()
    {
        ValidateParamaters();
        CreateTexture(ref shapeTexture, shapeResolution, shapeNoiseName);
        CreateTexture(ref detailTexture, detailResolution, detailNoiseName);

        if (updateNoise && noiseCompute)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            updateNoise = false;

            WorleyNoiseSettings activeSettings = ActiveSettings;
            if (activeSettings == null)
                return;

            buffersToRelease = new List<ComputeBuffer>();

            int activeTextureResolution = ActiveTexture.width;

            // Setup constants
            noiseCompute.SetFloat("persistence", activeSettings.persistence);
            noiseCompute.SetInt("resolution", activeTextureResolution);
            noiseCompute.SetVector("channelMask", ChannelMask);

            // Kernel 0: noise generation
            noiseCompute.SetTexture(0, "Result", ActiveTexture);
            var minMaxBuffer = CreateBuffer(new int[] { int.MaxValue, 0 }, sizeof(int), "minMax", 0);
            UpdateWorley(activeSettings);

            int numThreadGroups = Mathf.CeilToInt(activeTextureResolution / (float)computeThreadGroupSize);
            noiseCompute.Dispatch(0, numThreadGroups, numThreadGroups, numThreadGroups);

            // Kernel 1: normalization
            noiseCompute.SetBuffer(1, "minMax", minMaxBuffer);
            noiseCompute.SetTexture(1, "Result", ActiveTexture);
            noiseCompute.Dispatch(1, numThreadGroups, numThreadGroups, numThreadGroups);

            if (logComputeTime)
            {
                var minMax = new int[2];
                minMaxBuffer.GetData(minMax);
                Debug.Log($"Noise Generation: {timer.ElapsedMilliseconds}ms");
            }

            foreach (var buffer in buffersToRelease)
                buffer.Release();
        }
    }

    public void Load(string saveName, RenderTexture target)
    {
        if (target == null)
        {
            Debug.LogWarning("Load() called with null target RenderTexture.");
            return;
        }

        // Ensure UAV flag is set and texture created
        if (!target.enableRandomWrite || !target.IsCreated())
        {
            Debug.LogWarning($"{target.name} not created with UAV usage; recreating...");
            int res = target.width > 0 ? target.width : 32;
            var format = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm;

            target.Release();
            target.enableRandomWrite = true;
            target.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            target.graphicsFormat = format;
            target.volumeDepth = res;
            target.Create();
        }

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        saveName = sceneName + "_" + saveName;
        Texture3D savedTex = (Texture3D)Resources.Load(saveName);

        if (savedTex != null && savedTex.width == target.width)
        {
            int numThreadGroups = Mathf.CeilToInt(savedTex.width / 8f);
            copy.SetTexture(0, "tex", savedTex);
            copy.SetTexture(0, "renderTex", target);
            copy.Dispatch(0, numThreadGroups, numThreadGroups, numThreadGroups);
        }
    }

    // ────────────────────────────────────────────────
    // PROPERTIES
    // ────────────────────────────────────────────────

    public RenderTexture ActiveTexture =>
        (activeTextureType == CloudNoiseType.Shape) ? shapeTexture : detailTexture;

    public WorleyNoiseSettings ActiveSettings
    {
        get
        {
            WorleyNoiseSettings[] settings = (activeTextureType == CloudNoiseType.Shape)
                ? shapeSettings
                : detailSettings;

            int activeChannelIndex = (int)activeChannel;
            if (activeChannelIndex >= settings.Length)
                return null;

            return settings[activeChannelIndex];
        }
    }

    public Vector4 ChannelMask => new Vector4(
        (activeChannel == TextureChannel.R) ? 1 : 0,
        (activeChannel == TextureChannel.G) ? 1 : 0,
        (activeChannel == TextureChannel.B) ? 1 : 0,
        (activeChannel == TextureChannel.A) ? 1 : 0
    );

    // ────────────────────────────────────────────────
    // PRIVATE HELPERS
    // ────────────────────────────────────────────────

    void UpdateWorley(WorleyNoiseSettings settings)
    {
        var prng = new System.Random(settings.seed);
        CreateWorleyPointsBuffer(prng, settings.numDivisionsA, "pointsA");
        CreateWorleyPointsBuffer(prng, settings.numDivisionsB, "pointsB");
        CreateWorleyPointsBuffer(prng, settings.numDivisionsC, "pointsC");

        noiseCompute.SetInt("numCellsA", settings.numDivisionsA);
        noiseCompute.SetInt("numCellsB", settings.numDivisionsB);
        noiseCompute.SetInt("numCellsC", settings.numDivisionsC);
        noiseCompute.SetBool("invertNoise", settings.invert);
        noiseCompute.SetInt("tile", settings.tile);
    }

    void OnEnable()
    {
        // Ensure textures exist and are created before Load() is called
        CreateTexture(ref shapeTexture, shapeResolution, shapeNoiseName);
        CreateTexture(ref detailTexture, detailResolution, detailNoiseName);
    }

    void CreateWorleyPointsBuffer(System.Random prng, int numCellsPerAxis, string bufferName)
    {
        var points = new Vector3[numCellsPerAxis * numCellsPerAxis * numCellsPerAxis];
        float cellSize = 1f / numCellsPerAxis;

        for (int x = 0; x < numCellsPerAxis; x++)
            for (int y = 0; y < numCellsPerAxis; y++)
                for (int z = 0; z < numCellsPerAxis; z++)
                {
                    float randomX = (float)prng.NextDouble();
                    float randomY = (float)prng.NextDouble();
                    float randomZ = (float)prng.NextDouble();
                    Vector3 randomOffset = new Vector3(randomX, randomY, randomZ) * cellSize;
                    Vector3 cellCorner = new Vector3(x, y, z) * cellSize;
                    int index = x + numCellsPerAxis * (y + z * numCellsPerAxis);
                    points[index] = cellCorner + randomOffset;
                }

        CreateBuffer(points, sizeof(float) * 3, bufferName);
    }

    ComputeBuffer CreateBuffer(System.Array data, int stride, string bufferName, int kernel = 0)
    {
        var buffer = new ComputeBuffer(data.Length, stride, ComputeBufferType.Structured);
        buffersToRelease.Add(buffer);
        buffer.SetData(data);
        noiseCompute.SetBuffer(kernel, bufferName, buffer);
        return buffer;
    }

    void CreateTexture(ref RenderTexture texture, int resolution, string name)
    {
        var format = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm;
        bool needsRecreate = (texture == null || !texture.IsCreated() ||
                              texture.width != resolution || texture.height != resolution ||
                              texture.volumeDepth != resolution || texture.graphicsFormat != format);

        if (needsRecreate)
        {
            if (texture != null)
                texture.Release();

            texture = new RenderTexture(resolution, resolution, 0)
            {
                graphicsFormat = format,
                volumeDepth = resolution,
                enableRandomWrite = true,
                dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
                name = name
            };
            texture.Create();
        }

        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        // Load saved data safely after creation
        Load(name, texture);
    }

    void ValidateParamaters()
    {
        detailResolution = Mathf.Max(1, detailResolution);
        shapeResolution = Mathf.Max(1, shapeResolution);
    }

    public void ActiveNoiseSettingsChanged()
    {
        if (autoUpdate)
            updateNoise = true;
    }
}
