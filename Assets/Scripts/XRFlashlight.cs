using UnityEngine;
using Oculus.Interaction;

public class XRFlashlight : MonoBehaviour
{
    [SerializeField] private Light flashlightLight;
    [SerializeField] private Grabbable grabbable;

    private bool isLightOn = false;

    private void Start()
    {
        if (flashlightLight != null)
        {
            flashlightLight.enabled = false;
        }
        if (grabbable == null)
        {
            grabbable = GetComponent<Grabbable>();
        }

    }

    private void Update()
    {

        if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
        {
            ToggleFlashlight();
        }

        if (OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger))
        {
            ToggleFlashlight();
        }
    }

    private void ToggleFlashlight()
    {
        isLightOn = !isLightOn;
        if (flashlightLight != null)
        {
            flashlightLight.enabled = isLightOn;
        }
    }
}
