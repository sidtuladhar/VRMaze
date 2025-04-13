using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class FlashlightSystem : MonoBehaviour
{
    [Header("Flashlight Components")]
    [SerializeField] private Light flashlight;
    [SerializeField] private float maxBatteryLife = 100f;
    [SerializeField] private float batteryDrainRate = 5f; // per second
    [SerializeField] private float batteryRechargeAmount = 25f;

    [Header("Flashlight Properties")]

    private float currentBattery;
    public bool isOn = true;
    private Image batteryBar;

    [Header("Game Over Effects")]
    [SerializeField] private float maxFogDensity = 0.5f;
    [SerializeField] private Color fogColor = Color.black;
    [SerializeField] private float gameOverDelay = 5f;

    private Color initialFogColor;

    private void UpdateUI()
    {
        if (batteryBar != null)
        {
            batteryBar.fillAmount = currentBattery / maxBatteryLife;
        }
    }

    void Start()
    {
        currentBattery = maxBatteryLife;
        if (flashlight == null)
        {
            Debug.LogError("Flashlight light component not assigned!");
        }
        initialFogColor = RenderSettings.fogColor;

        UpdateUI();
        batteryBar = GameObject.Find("BatteryBar").GetComponent<Image>();

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleFlashlight();
        }

        if (isOn)
        {
            DrainBatteryFast();
        }
        else
        {
            DrainBatterySlow();
        }
    }

    private void DrainBatteryFast()
    {
        currentBattery -= batteryDrainRate * Time.deltaTime;
        UpdateUI();

        if (currentBattery <= 0)
        {
            GameOver();
        }
    }
    private void DrainBatterySlow()
    {
        currentBattery -= batteryDrainRate * Time.deltaTime / 2;
        UpdateUI();

        if (currentBattery <= 0)
        {
            GameOver();
        }
    }

    private void ToggleFlashlight()
    {
        isOn = !isOn;
        flashlight.enabled = isOn;
    }

    public void AddBattery()
    {
        currentBattery = Mathf.Min(currentBattery + batteryRechargeAmount, maxBatteryLife);
        UpdateUI();
    }

    void GameOver()
    {
        isOn = false;
        flashlight.enabled = false;
        // Call your game over function here
        StartCoroutine(FogTransition());
    }

    private System.Collections.IEnumerator FogTransition()
    {
        // Enable fog if not already enabled
        RenderSettings.fog = true;

        // Gradually increase fog density
        float elapsedTime = 0f;
        float currentFogDensity = RenderSettings.fogDensity;

        // Disable player control
        PlayerController player = GetComponent<PlayerController>();
        if (player != null)
        {
            player.enabled = false;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowGameOverUI();
        }
        else
        {
            Debug.LogWarning("GameManager instance not found");
        }


        // Gradually transition to dense fog
        while (elapsedTime < gameOverDelay)
        {
            float t = elapsedTime / gameOverDelay;
            RenderSettings.fogDensity = Mathf.Lerp(currentFogDensity, maxFogDensity, t);
            RenderSettings.fogColor = Color.Lerp(initialFogColor, fogColor, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Game over - reload scene or show game over screen
        //causes a bug that resets the position and pose of the player, and resets the scene
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}