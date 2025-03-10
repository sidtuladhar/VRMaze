using UnityEngine;

public class BatteryPickup : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FlashlightSystem flashlight = other.GetComponent<FlashlightSystem>();
            
            if (flashlight != null)
            {
                flashlight.AddBattery();
                Destroy(gameObject);
            } else {
                Debug.LogWarning("FlashlightSystem not found on player");
            }
        }
    }
}