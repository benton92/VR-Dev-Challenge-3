using UnityEngine;
using TMPro;
public class DualGestureDetector : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bookSpawner bookSpawnerScript; // Add reference to book spawner

    public bool leftHandGestureDetected = false;
    public bool rightHandGestureDetected = false;

    private bool gesturesWereDetected = false; // Track previous state

    void Update()
    {
        // Only update text and play audio when BOTH gestures are detected
        if (leftHandGestureDetected && rightHandGestureDetected)
        {
            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }

            // Trigger book spawn only once when gestures first detected
            if (!gesturesWereDetected && bookSpawnerScript != null)
            {
                bookSpawnerScript.handsSetTrue();
                gesturesWereDetected = true;
            }
        }
        else
        {
            audioSource.Stop();

            // Call handsSetFalse when gestures are no longer detected
            if (gesturesWereDetected && bookSpawnerScript != null)
            {
                bookSpawnerScript.handsSetFalse();
                gesturesWereDetected = false;
            }
        }
    }

    // Call these methods from other scripts to set gestures
    public void SetLeftGesture(bool detected) => leftHandGestureDetected = detected;
    public void SetRightGesture(bool detected) => rightHandGestureDetected = detected;
    public void ResetGestures() => leftHandGestureDetected = rightHandGestureDetected = false;
}