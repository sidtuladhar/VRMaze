using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class FlashlightSystem : MonoBehaviour
{
    [Header("Flashlight Components")]
    [SerializeField] private Light flashlight;

    [Header("Flashlight Properties")]

    public bool isOn = true;

    void Start()
    {
        if (flashlight == null)
        {
            Debug.LogError("Flashlight light component not assigned!");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleFlashlight();
        }
    }

    private void ToggleFlashlight()
    {
        isOn = !isOn;
        flashlight.enabled = isOn;
    }
}