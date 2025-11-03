using UnityEngine;

public class ProgressiveSpawner : MonoBehaviour
{
    [Header("Prefab to Spawn")]
    public GameObject prefab;

    [Header("Spawn Timing")]
    public float startDelay = 3f;     // starting time between spawns
    public float minDelay = 0.3f;     // the fastest allowed
    public float speedIncrease = 0.95f; // multiply delay by this (less = faster)

    private float currentDelay;

    private void Start()
    {
        currentDelay = startDelay;
        StartCoroutine(SpawnRoutine());
    }

    private System.Collections.IEnumerator SpawnRoutine()
    {
        while (true)
        {
            // ✅ spawn the prefab at this object's position & rotation
            Instantiate(prefab, transform.position, transform.rotation);

            // ✅ wait for the current delay
            yield return new WaitForSeconds(currentDelay);

            // ✅ decrease delay toward the minDelay (makes spawning faster)
            currentDelay *= speedIncrease;

            // clamp so it doesn't go too low
            if (currentDelay < minDelay)
                currentDelay = minDelay;
        }
    }
}