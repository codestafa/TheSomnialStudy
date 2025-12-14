using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates a dreamy, atmospheric background effect for the menu.
/// Generates floating particles and subtle color shifts.
/// </summary>
public class MenuBackground : MonoBehaviour
{
    [Header("Background Colors")]
    [SerializeField] private Color backgroundColor1 = new Color(0.05f, 0.02f, 0.1f, 1f);
    [SerializeField] private Color backgroundColor2 = new Color(0.08f, 0.04f, 0.15f, 1f);
    [SerializeField] private float colorCycleSpeed = 0.2f;

    [Header("Floating Particles")]
    [SerializeField] private int particleCount = 30;
    [SerializeField] private float particleMinSize = 2f;
    [SerializeField] private float particleMaxSize = 8f;
    [SerializeField] private float particleSpeed = 20f;
    [SerializeField] private Color particleColor = new Color(0.6f, 0.5f, 0.9f, 0.3f);

    [Header("Vignette")]
    [SerializeField] private bool enableVignette = true;
    [SerializeField] private float vignetteIntensity = 0.4f;

    private Camera menuCamera;
    private Canvas canvas;
    private Image backgroundImage;
    private Image vignetteImage;
    private RectTransform[] particles;
    private Vector2[] particleVelocities;
    private float[] particlePhases;

    private void Start()
    {
        menuCamera = Camera.main;
        SetupBackground();
        SetupParticles();
        if (enableVignette) SetupVignette();
    }

    private void SetupBackground()
    {
        // Find or create canvas
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Create background image
        GameObject bgObj = new GameObject("MenuBackgroundGradient");
        bgObj.transform.SetParent(canvas.transform, false);
        bgObj.transform.SetAsFirstSibling(); // Put behind everything

        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = backgroundColor1;
        backgroundImage.raycastTarget = false;

        RectTransform rt = bgObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Also set camera background
        if (menuCamera != null)
        {
            menuCamera.backgroundColor = backgroundColor1;
        }
    }

    private void SetupParticles()
    {
        if (canvas == null) return;

        // Create particle container
        GameObject particleContainer = new GameObject("MenuParticles");
        particleContainer.transform.SetParent(canvas.transform, false);
        particleContainer.transform.SetSiblingIndex(1); // Above background, below UI

        RectTransform containerRT = particleContainer.AddComponent<RectTransform>();
        containerRT.anchorMin = Vector2.zero;
        containerRT.anchorMax = Vector2.one;
        containerRT.offsetMin = Vector2.zero;
        containerRT.offsetMax = Vector2.zero;

        particles = new RectTransform[particleCount];
        particleVelocities = new Vector2[particleCount];
        particlePhases = new float[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            GameObject particle = new GameObject($"Particle_{i}");
            particle.transform.SetParent(particleContainer.transform, false);

            Image img = particle.AddComponent<Image>();
            img.color = particleColor;
            img.raycastTarget = false;

            // Make it circular by using a soft gradient
            // Since we don't have a sprite, we'll use the default and rely on small size
            float size = Random.Range(particleMinSize, particleMaxSize);

            RectTransform rt = particle.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);

            // Random starting position
            float x = Random.Range(-Screen.width * 0.6f, Screen.width * 0.6f);
            float y = Random.Range(-Screen.height * 0.6f, Screen.height * 0.6f);
            rt.anchoredPosition = new Vector2(x, y);

            // Random velocity (mostly upward, slight horizontal drift)
            float vx = Random.Range(-0.3f, 0.3f);
            float vy = Random.Range(0.3f, 1f);
            particleVelocities[i] = new Vector2(vx, vy).normalized * particleSpeed * Random.Range(0.5f, 1.5f);

            // Random phase for sine wave movement
            particlePhases[i] = Random.Range(0f, Mathf.PI * 2f);

            particles[i] = rt;

            // Vary alpha based on size (larger = more transparent)
            float alpha = Mathf.Lerp(0.4f, 0.1f, (size - particleMinSize) / (particleMaxSize - particleMinSize));
            img.color = new Color(particleColor.r, particleColor.g, particleColor.b, alpha);
        }
    }

    private void SetupVignette()
    {
        if (canvas == null) return;

        GameObject vignetteObj = new GameObject("MenuVignette");
        vignetteObj.transform.SetParent(canvas.transform, false);
        vignetteObj.transform.SetAsLastSibling();

        vignetteImage = vignetteObj.AddComponent<Image>();
        vignetteImage.raycastTarget = false;

        // Create vignette gradient texture
        Texture2D vignetteTex = CreateVignetteTexture(256);
        vignetteImage.sprite = Sprite.Create(vignetteTex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
        vignetteImage.color = new Color(0, 0, 0, vignetteIntensity);
        vignetteImage.type = Image.Type.Sliced;

        RectTransform rt = vignetteObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private Texture2D CreateVignetteTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                float alpha = Mathf.Pow(dist, 2f); // Quadratic falloff
                alpha = Mathf.Clamp01(alpha);
                tex.SetPixel(x, y, new Color(0, 0, 0, alpha));
            }
        }

        tex.Apply();
        return tex;
    }

    private void Update()
    {
        UpdateBackgroundColor();
        UpdateParticles();
    }

    private void UpdateBackgroundColor()
    {
        if (backgroundImage == null) return;

        float t = (Mathf.Sin(Time.time * colorCycleSpeed) + 1f) / 2f;
        Color currentColor = Color.Lerp(backgroundColor1, backgroundColor2, t);
        backgroundImage.color = currentColor;

        if (menuCamera != null)
        {
            menuCamera.backgroundColor = currentColor;
        }
    }

    private void UpdateParticles()
    {
        if (particles == null) return;

        float halfWidth = Screen.width * 0.6f;
        float halfHeight = Screen.height * 0.6f;

        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null) continue;

            // Update position with sine wave for dreamy movement
            float sineOffset = Mathf.Sin(Time.time + particlePhases[i]) * 10f;
            Vector2 pos = particles[i].anchoredPosition;
            pos += particleVelocities[i] * Time.deltaTime;
            pos.x += sineOffset * Time.deltaTime;

            // Wrap around screen
            if (pos.y > halfHeight + 50) pos.y = -halfHeight - 50;
            if (pos.y < -halfHeight - 50) pos.y = halfHeight + 50;
            if (pos.x > halfWidth + 50) pos.x = -halfWidth - 50;
            if (pos.x < -halfWidth - 50) pos.x = halfWidth + 50;

            particles[i].anchoredPosition = pos;

            // Subtle alpha pulsing
            Image img = particles[i].GetComponent<Image>();
            if (img != null)
            {
                float baseAlpha = img.color.a;
                float pulse = 1f + Mathf.Sin(Time.time * 2f + particlePhases[i]) * 0.2f;
                // Keep alpha stable, just slight variation
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up created textures
        if (vignetteImage != null && vignetteImage.sprite != null)
        {
            if (vignetteImage.sprite.texture != null)
            {
                Destroy(vignetteImage.sprite.texture);
            }
            Destroy(vignetteImage.sprite);
        }
    }
}
