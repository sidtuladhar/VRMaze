using UnityEngine;
using Oculus.Interaction;

public class XRFlashlight : MonoBehaviour
{
    [SerializeField] private Light flashlightLight;

    private GrabInteractable grabInteractable;

    private bool isLightOn = false;

    private void Start()
    {
        if (flashlightLight != null)
        {
            flashlightLight.enabled = false;
        }
        if (grabInteractable == null)
        {
            grabInteractable = GetComponentInChildren<GrabInteractable>();
        }
    }

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger) && (grabInteractable.State == InteractableState.Select))
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