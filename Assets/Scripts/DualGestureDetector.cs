using UnityEngine;
using TMPro;

public class DualGestureDetector : MonoBehaviour
{
    [SerializeField] private TextMeshPro statusText;
    [SerializeField] private AudioSource audioSource;

    public bool leftHandGestureDetected = false;
    public bool rightHandGestureDetected = false;

    [SerializeField] private string bothGesturesMessage = "Both Gestures Detected!";
    [SerializeField] private string defaultMessage = "Waiting for gestures...";

    void Update()
    {
        if (statusText == null) return;

        // Only update text and play audio when BOTH gestures are detected
        if (leftHandGestureDetected && rightHandGestureDetected)
        {
            statusText.text = bothGesturesMessage;

            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            statusText.text = defaultMessage;

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }

    // Call these methods from other scripts to set gestures
    public void SetLeftGesture(bool detected) => leftHandGestureDetected = detected;
    public void SetRightGesture(bool detected) => rightHandGestureDetected = detected;
    public void ResetGestures() => leftHandGestureDetected = rightHandGestureDetected = false;
}