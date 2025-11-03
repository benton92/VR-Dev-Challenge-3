using UnityEngine;

public class DeactivateOnTrigger : MonoBehaviour
{
    public float reactivateDelay = 5f;  // seconds before reactivating
    public GameObject objectToDeactivate; // assign the object (can be self)

    private void Start()
    {
        // If not set, deactivate this object itself
        if (objectToDeactivate == null)
            objectToDeactivate = gameObject;
    }

    private void OnTriggerEnter(Collider other)
    {
         if (other.CompareTag("PlayerAttack"))
        {
            objectToDeactivate.SetActive(false);
            Invoke(nameof(ReactivateObject), reactivateDelay);
        }
    }

    private void ReactivateObject()
    {
        objectToDeactivate.SetActive(true);
    }
}