using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuNarrative : MonoBehaviour
{
    [Header("UI Elements")]
    public CanvasGroup mainMenuGroup;
    public TMP_Text mainMenuText;
    public Button beginButton;

    public TMP_Text backstoryText;
    [TextArea(5, 20)] public string backstoryContent;

    public CanvasGroup enterButtonGroup;
    public Button enterButton;

    [Header("Timings")]
    public float fadeDuration = 1f;
    public float typewriterSpeed = 0.05f; // seconds per character

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip typewriterClip;
    public bool loopTypewriterAudio = true;

    [Header("Button Click Sound")]
    public AudioSource buttonClickSource;
    public AudioClip beginClickSound;


    private void Start()
    {
        backstoryText.text = "";
        enterButtonGroup.alpha = 0;
        enterButtonGroup.interactable = false;
        enterButtonGroup.blocksRaycasts = false;

        beginButton.onClick.AddListener(OnBeginClicked);
    }

    void OnBeginClicked()
    {
        if (buttonClickSource && beginClickSound)
        {
            buttonClickSource.PlayOneShot(beginClickSound);
        }
        beginButton.interactable = false;
        StartCoroutine(FadeOutMainMenu());
    }

    IEnumerator FadeOutMainMenu()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            mainMenuGroup.alpha = 1 - timer / fadeDuration;
            yield return null;
        }

        mainMenuGroup.gameObject.SetActive(false);
        yield return StartCoroutine(TypeBackstory());
        ShowEnterButton();
    }

    IEnumerator TypeBackstory()
    {
        backstoryText.text = "";

        if (audioSource && typewriterClip)
        {
            audioSource.clip = typewriterClip;
            audioSource.loop = loopTypewriterAudio;
            audioSource.Play();
        }
        foreach (char c in backstoryContent)
        {
            backstoryText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }

    void ShowEnterButton()
    {
        StartCoroutine(FadeInEnterButton());
    }

    IEnumerator FadeInEnterButton()
    {
        float timer = 0f;
        enterButtonGroup.gameObject.SetActive(true);

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            enterButtonGroup.alpha = timer / fadeDuration;
            yield return null;
        }

        enterButtonGroup.interactable = true;
        enterButtonGroup.blocksRaycasts = true;
    }
}
