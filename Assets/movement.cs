using UnityEngine;

public class MoveTowardTarget : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float maxLifetime = 10f;   // safety despawn

    // ✅ Fixed world target position
    private Vector3 targetPos = new Vector3(21.6000004f, 92.7337189f, -104.599998f);

    private void start()
    {
        // Auto-destroy if nothing hits it
        Destroy(gameObject, maxLifetime);
    }

    private void Update()
    {
        // ✅ Move toward the target
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            moveSpeed * Time.deltaTime
        );
    }


    // ✅ Destroy on any TRIGGER
    private void OnTriggerEnter(Collider other)
    {
        /*
        if (other.CompareTag("Playersheild"))
        {
            Destroy(gameObject);
        }
        if (other.CompareTag("Untagged"))
        {
            Destroy(gameObject);
        }
        */
    }
}