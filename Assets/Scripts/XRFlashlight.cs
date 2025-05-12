using UnityEngine;
using UnityEngine.XR;
using Oculus.Interaction;
using System.Collections.Generic;

public class XRFlashlight : MonoBehaviour
{
    [SerializeField] private Light flashlightLight;
    [SerializeField] private Grabbable grabbable;

    private bool isLightOn = false;
    private bool previousTriggerPressed = false;

    private InputDevice leftHand;
    private InputDevice rightHand;

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

        InitializeHands();
    }

    private void InitializeHands()
    {
        var leftDevices = new List<InputDevice>();
        var rightDevices = new List<InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, leftDevices);
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightDevices);

        if (leftDevices.Count > 0) leftHand = leftDevices[0];
        if (rightDevices.Count > 0) rightHand = rightDevices[0];
    }

    private void Update()
    {
        if (grabbable == null || grabbable.SelectingPointsCount == 0)
        {
            previousTriggerPressed = false;
            Debug.Log("No grabbable object selected.");
            return;
        }

        // Check if left or right hand is grabbing
        bool leftGrabbing = IsDeviceGrabbing(leftHand);
        bool rightGrabbing = IsDeviceGrabbing(rightHand);

        // Check trigger for the correct hand
        bool triggerPressed = false;
        if (leftGrabbing && leftHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool leftTrigger))
        {
            triggerPressed = leftTrigger;
            Debug.Log("Left trigger pressed");
        }
        else if (rightGrabbing && rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool rightTrigger))
        {
            triggerPressed = rightTrigger;
            Debug.Log("Right trigger pressed");
        }

        // Toggle on trigger press
        if (triggerPressed && !previousTriggerPressed)
        {
            ToggleFlashlight();
        }

        previousTriggerPressed = triggerPressed;
    }

    private bool IsDeviceGrabbing(InputDevice device)
    {
        if (!device.isValid || grabbable.GrabPoints.Count == 0)
            return false;

        // Try to match based on device position
        if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 devicePos))
        {
            Pose grabPose = grabbable.GrabPoints[0];
            float distance = Vector3.Distance(grabPose.position, devicePos);
            return distance < 0.15f; // adjust threshold as needed
        }

        return false;
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
