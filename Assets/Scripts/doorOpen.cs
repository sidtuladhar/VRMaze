using UnityEngine;

public class doorOpen : MonoBehaviour
{
    private Animator animator;
    private AudioSource audioSource;
    private bool hasOpened = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!hasOpened && other.CompareTag("Player"))
        {
            hasOpened = true; // mark as used right away

            animator.SetTrigger("Open");

            if (audioSource)
                audioSource.Play();
        }
    }

    public void playSound()
    {
        if (audioSource)
            audioSource.Play();
    }
}
