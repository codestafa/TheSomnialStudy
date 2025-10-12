using UnityEngine;
using UnityEditor;

public class Noise3DGenerator
{
    [MenuItem("Tools/Generate 3D Noise Texture")]
    public static void Generate()
    {
        int size = 64;
        Texture3D tex = new Texture3D(size, size, size, TextureFormat.R8, false);
        Color[] colors = new Color[size * size * size];

        for (int z = 0; z < size; z++)
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float nx = x / (float)size;
                    float ny = y / (float)size;
                    float nz = z / (float)size;

                    float noise = Mathf.PerlinNoise(nx * 4, ny * 4) * Mathf.PerlinNoise(ny * 4, nz * 4);
                    colors[x + y * size + z * size * size] = new Color(noise, noise, noise, 1);
                }

        tex.SetPixels(colors);
        tex.Apply();

        AssetDatabase.CreateAsset(tex, "Assets/CloudNoise3D.asset");
        AssetDatabase.SaveAssets();

        Debug.Log("3D Noise Texture Generated!");
    }
}
