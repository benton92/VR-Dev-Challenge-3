using UnityEngine;

public class PlaySoundOnTrigger : MonoBehaviour
{
    public AudioSource audioSource;   // Assign the AudioSource in the Inspector
    public AudioClip soundClip;       // Assign the AudioClip in the Inspector

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))   // Only trigger for the Player
        {
            if (audioSource != null && soundClip != null)
            {
                audioSource.PlayOneShot(soundClip);
            }
        }
    }
}