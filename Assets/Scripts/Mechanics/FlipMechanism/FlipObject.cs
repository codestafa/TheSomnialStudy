using UnityEngine;
using System.Collections;

public class FlipObject : MonoBehaviour, IInteractable
{
    [Header("Throw Settings")]
    [SerializeField] float throwPower = 5f;   // <-- main knob for distance
    [SerializeField] float arcHeight = 0.25f;
    [SerializeField] int spins = 2;
    [SerializeField] Vector3 spinAxis = new Vector3(1f, 0.2f, 0f);

    [Header("Final Open Pose (relative)")]
    [SerializeField] Vector3 openLocalEuler = new Vector3(0f, 180f, 0f);
    [SerializeField] bool returnToSameSpot = false;

    Rigidbody rb;
    bool open = false;
    bool busy = false;
    bool alreadyFlipped = false;
    Vector3 restingPos;
    Quaternion restingRot;

    public bool CanInteract { get { return !alreadyFlipped && !busy; } }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        restingPos = transform.position;
        restingRot = transform.rotation;
        if (spins <= 0) spins = 2;
    }

    public void Interact()
    {
        if (alreadyFlipped) return;

        if (rb != null && !rb.isKinematic)
        {
            PhysicsToss();
            open = !open;
            alreadyFlipped = true;
            return;
        }

        if (busy) return;
        StopAllCoroutines();
        StartCoroutine(TossAndFlip());
    }

    void PhysicsToss()
    {
        Vector3 fwd = (Camera.main ? Camera.main.transform.forward : transform.forward);
        fwd.y = 0f; fwd.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, fwd).normalized;

        // 🚀 push scaled by throwPower
        Vector3 push = fwd * (0.5f * throwPower) + side * (1.0f * throwPower);

        rb.AddForce(Vector3.up * (0.3f * throwPower) + push, ForceMode.Impulse);

        Vector3 axis = (Vector3.right + 0.25f * Vector3.up + 0.2f * Vector3.forward).normalized;
        rb.AddTorque(axis * throwPower, ForceMode.Impulse);

        CancelInvoke(nameof(SettlePose));
        Invoke(nameof(SettlePose), 0.7f);
    }

    void SettlePose()
    {
        if (rb == null) return;
        rb.angularVelocity = Vector3.zero;
        Quaternion target = open ? restingRot * Quaternion.Euler(openLocalEuler) : restingRot;
        transform.rotation = target;
    }

    IEnumerator TossAndFlip()
    {
        busy = true;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 fwd = (Camera.main ? Camera.main.transform.forward : transform.forward);
        fwd.y = 0f; fwd.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, fwd).normalized;

        // 🚀 offset scaled by throwPower
        Vector3 offset = fwd * (0.5f * throwPower) + side * (1.0f * throwPower);
        Vector3 endPos = returnToSameSpot ? startPos : startPos + offset;

        Quaternion endRot = open ? restingRot : restingRot * Quaternion.Euler(openLocalEuler);
        Vector3 axis = (spinAxis.sqrMagnitude < 1e-4f ? Vector3.right : spinAxis.normalized);

        float t = 0f;
        float dur = Mathf.Max(0.05f, 0.6f);

        while (t < 1f)
        {
            t += Time.deltaTime / dur;

            // Position along arc
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            pos += Vector3.up * (4f * arcHeight * t * (1f - t));

            // Rotation with spin
            float spinAngle = (open ? -spins : spins) * 360f * t;
            Quaternion spin = Quaternion.AngleAxis(spinAngle, axis);
            Quaternion rot = Quaternion.Slerp(startRot * spin, endRot, Mathf.SmoothStep(0f, 1f, t));

            transform.position = pos;
            transform.rotation = rot;

            yield return null;
        }

        transform.position = endPos;
        transform.rotation = endRot;

        open = !open;
        busy = false;
        alreadyFlipped = true;
    }
}
