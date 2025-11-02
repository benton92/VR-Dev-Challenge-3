using UnityEngine;
using TMPro;

public class DualGestureDetector : MonoBehaviour
{
    [SerializeField] private TextMeshPro statusText;

    public bool leftHandGestureDetected = false;
    public bool rightHandGestureDetected = false;

    [SerializeField] private string bothGesturesMessage = "Both Gestures Detected!";
    [SerializeField] private string defaultMessage = "Waiting for gestures...";

    void Update()
    {
        if (statusText == null) return;

        // Only update text when BOTH gestures are detected
        if (leftHandGestureDetected && rightHandGestureDetected)
        {
            statusText.text = bothGesturesMessage;
        }
        else
        {
            statusText.text = defaultMessage;
        }
    }

    // Call these methods from other scripts to set gestures
    public void SetLeftGesture(bool detected) => leftHandGestureDetected = detected;
    public void SetRightGesture(bool detected) => rightHandGestureDetected = detected;
    public void ResetGestures() => leftHandGestureDetected = rightHandGestureDetected = false;
}