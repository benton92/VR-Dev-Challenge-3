using UnityEngine;
using System.Collections;

public class HalfCircleMover : MonoBehaviour
{
    public float radius = 3f;            // Size of the half-circle
    public float speed = 2f;             // Movement speed
    public Transform centerPoint;        // Center of the circle

    private float angle = 0f;            // Current angle
    private int direction = 1;           // 1 = forward, -1 = backward
    private bool isPaused = false;

    private void Start()
    {
        if (centerPoint == null)
        {
            Debug.LogError("Assign a centerPoint transform!");
        }

        StartCoroutine(RandomPauseRoutine());
    }

    private void Update()
    {
        if (isPaused || centerPoint == null)
            return;

        // Move angle
        angle += speed * direction * Time.deltaTime;

        // Reverse at end of arc (0 to 180 degrees)
        if (angle >= 180f)
        {
            angle = 180f;
            direction = -1;
        }
        else if (angle <= 0f)
        {
            angle = 0f;
            direction = 1;
        }

        // Convert angle to position in world space
        float radians = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * radius;

        transform.position = centerPoint.position + offset;
    }

    private IEnumerator RandomPauseRoutine()
    {
        while (true)
        {
            // Wait randomly between pauses
            yield return new WaitForSeconds(Random.Range(3f, 7f));

            // Pause
            isPaused = true;
            yield return new WaitForSeconds(Random.Range(2f, 3f)); // pause 2–3 sec

            // Resume
            isPaused = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);  
    }
}