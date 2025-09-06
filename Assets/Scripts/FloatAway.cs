using UnityEngine;

public class FloatAway : MonoBehaviour
{
    public Vector3 direction = new Vector3(0, 1, 0); // default = upward
    public float speed = 1f; // movement speed

    void Update()
    {
        transform.Translate(direction.normalized * speed * Time.deltaTime);
    }
}
