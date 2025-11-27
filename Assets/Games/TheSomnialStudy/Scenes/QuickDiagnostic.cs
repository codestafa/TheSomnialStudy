using UnityEngine;

/// <summary>
/// Quick diagnostic - add to your Camera, press F1 to see what's wrong
/// </summary>
public class QuickDiagnostic : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("=== DIAGNOSTIC ===");
            Debug.Log($"1. Cursor Lock: {Cursor.lockState}");
            Debug.Log($"2. Mouse Y Input: {Input.GetAxisRaw("Mouse Y")}");
            Debug.Log($"3. Mouse X Input: {Input.GetAxisRaw("Mouse X")}");
            
            PlayerLook pl = GetComponent<PlayerLook>();
            if (pl != null)
            {
                Debug.Log($"4. PlayerLook Enabled: {pl.enabled}");
                Debug.Log($"5. PlayerLook Active: {pl.gameObject.activeInHierarchy}");
            }
            
            Debug.Log($"6. Camera Rotation X: {transform.localRotation.eulerAngles.x}");
            Debug.Log($"7. TimeScale: {Time.timeScale}");
            
            Debug.Log("=== Now move your mouse UP and press F1 again to see if Mouse Y changes ===");
        }
    }
}
