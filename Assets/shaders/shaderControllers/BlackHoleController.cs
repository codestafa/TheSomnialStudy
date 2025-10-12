using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
public class BlackHoleController : MonoBehaviour
{
    public Material blackHoleMaterial;
    public float schwarzschildRadius = 1.0f;
    public float ringRadius = 8.0f;

    void Update()
    {
        if (blackHoleMaterial != null)
        {
            blackHoleMaterial.SetFloat("_a", schwarzschildRadius);
            blackHoleMaterial.SetFloat("_RingRadius", ringRadius);
        }
    }
}
