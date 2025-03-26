using UnityEngine;
using UnityEngine.InputSystem;


public class XRFlashlight : MonoBehaviour
{
    [SerializeField] private Light flashlight;
    [SerializeField] private float maxBatteryLife = 100f;
    [SerializeField] private float batteryDrainRate = 5f;
    [SerializeField] private float batteryRechargeAmount = 25f;

    private float currentBattery;
    private bool isOn = true;
    
    private InputAction flashlightToggleAction;

    void Start()
    {
        currentBattery = maxBatteryLife;
        if (flashlight == null)
        {
            Debug.LogError("Flashlight light component not assigned!");
        }

        // Set up flashlight toggle action
        var actionMap = new InputActionMap("FlashlightControls");
        flashlightToggleAction = actionMap.AddAction("ToggleFlashlight", binding: "<XRController>/gripPressed");
        flashlightToggleAction.performed += ctx => ToggleFlashlight();
        flashlightToggleAction.Enable();
    }

    void Update()
    {
        if (isOn)
        {
            DrainBattery();
        }
        else
        {
            RechargeBattery();
        }
    }

private void RechargeBattery()
{
    if (currentBattery < maxBatteryLife)
    {
        currentBattery += batteryRechargeAmount * Time.deltaTime;
        if (currentBattery > maxBatteryLife)
        {
            currentBattery = maxBatteryLife;
        }
    }
}


    private void DrainBattery()
    {
        currentBattery -= batteryDrainRate * Time.deltaTime;
        if (currentBattery <= 0)
        {
            flashlight.enabled = false;
            isOn = false;
        }
    }

    private void ToggleFlashlight()
    {
        isOn = !isOn;
        flashlight.enabled = isOn;
    }
}
