using UnityEngine;
using System.Collections;

public class HalfCircleLeftRight_ZY : MonoBehaviour
{
    public float radius = 3f;
    public float speed = 2f;

    public GameObject spawnPrefab;    // ✅ prefab to spawn
    public Transform spawnPoint;      // ✅ optional spawn point

    private Vector3 centerPoint;
    private float angle = 0f;
    private int direction = 1;
    private bool isPaused = false;

    private void Start()
    {
        // Use spawn point as arc center
        centerPoint = transform.position;

        // Force Y rotation to 90
        transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        StartCoroutine(RandomPauseRoutine());
    }

    private void Update()
    {
        if (isPaused) return;

        angle += speed * direction * Time.deltaTime;

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

        float rad = angle * Mathf.Deg2Rad;

        float zPos = Mathf.Lerp(-radius, radius, angle / 180f);
        float yPos = Mathf.Sin(rad) * radius;

        transform.position = new Vector3(
            centerPoint.x,
            centerPoint.y + yPos,
            centerPoint.z + zPos
        );
    }

    private IEnumerator RandomPauseRoutine()
    {
        while (true)
        {
            // ✅ Wait normally before spawning
            yield return new WaitForSeconds(Random.Range(3f, 7f));

            // ✅ Spawn prefab right BEFORE the pause starts
            if (spawnPrefab != null)
            {
                Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;
                Quaternion rot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

                Instantiate(spawnPrefab, pos, rot);
            }

            // ✅ Begin pause
            isPaused = true;

            // Pause duration
            yield return new WaitForSeconds(Random.Range(2f, 3f));

            isPaused = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("FireSpellCollider"))
        {
            Destroy(gameObject);
        }
        if (other.CompareTag("LightningSpellCollider"))
        {
            Destroy(gameObject);
        }
    }
}