using UnityEngine;

public class playerTeleport : MonoBehaviour
{
    public Vector3 teleportPosition = new Vector3(21.6000004f, 92.7337189f, -104.599998f);
    public AudioSource audioSource;
    public AudioClip teleportSound;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Play sound
            if (audioSource != null && teleportSound != null)
                audioSource.Stop();
                audioSource.PlayOneShot(teleportSound);

            // Disable CharacterController (prevents Y snapping)
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // Freeze Rigidbody (prevents falling or snapping)
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // Teleport
            other.transform.position = teleportPosition;

            // Re-enable controllers
            if (cc != null) cc.enabled = true;
            if (rb != null) rb.isKinematic = false;
        }
    }
}