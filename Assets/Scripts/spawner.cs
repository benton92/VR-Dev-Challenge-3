using System.Collections;
using UnityEngine;

/// Simple spawner: instantiate a prefab at this GameObject's transform (or an optional spawn point)
/// at a fixed interval (seconds) until an optional cutoff time is reached.
/// - Set <see cref="startDelay"/> to add an initial delay (seconds) before the first spawn.
/// - Set <see cref="spawnInterval"/> to control speed of spawning (seconds between spawns).
/// - Set <see cref="cutoffTime"/> to the duration (seconds) after which spawning stops. 0 or negative => infinite.
/// - Call StartSpawning() / StopSpawning() from other scripts if you need runtime control.
public class Spawner : MonoBehaviour
{
	[Header("Spawn Settings")]
	[Tooltip("Prefab to spawn")]
	public GameObject prefab;

	[Tooltip("Optional transform used for spawn position/rotation. If null, this GameObject's transform is used.")]
	public Transform spawnPoint;

	[Tooltip("Seconds between spawns (spawn speed). Use a positive value. If <= 0, will spawn every frame.")]
	public float spawnInterval = 1f;

	[Tooltip("How many seconds to keep spawning. If <= 0, spawn indefinitely.")]
	public float cutoffTime = 10f;

	[Tooltip("Delay (seconds) before the first spawn after spawning starts. Use 0 for immediate start.")]
	public float startDelay = 0f;

	[Tooltip("Start spawning automatically when the object awakes/starts.")]
	public bool startOnAwake = true;

	// internal state
	private Coroutine spawnRoutine;
	// simple incremental counter used to give each spawned instance a unique name
	private int spawnCounter = 0;

	private void Start()
	{
		if (startOnAwake)
			StartSpawning();
	}

	/// Start spawning if not already running.
	///
	/// Caller can pass `applyStartDelay = false` to begin spawning immediately and skip the configured
	/// `startDelay`. The default (`true`) preserves previous behavior so existing calls to
	/// `StartSpawning()` behave the same.
	public void StartSpawning(bool applyStartDelay = true)
	{
		if (spawnRoutine == null)
			spawnRoutine = StartCoroutine(SpawnCoroutine(applyStartDelay));
	}

	/// Stop spawning (if running).
	public void StopSpawning()
	{
		if (spawnRoutine != null)
		{
			StopCoroutine(spawnRoutine);
			spawnRoutine = null;
		}
	}

	private IEnumerator SpawnCoroutine(bool applyStartDelay)
	{
		float elapsed = 0f;

		// If cutoffTime <= 0 => infinite loop
		bool infinite = cutoffTime <= 0f;

		// initial delay before the first spawn (counts toward cutoffTime) if requested
		if (applyStartDelay && startDelay > 0f)
		{
			// allow StopSpawning() to cancel during this wait since it stops the coroutine
			yield return new WaitForSeconds(startDelay);
			elapsed += startDelay;
		}

		while (infinite || elapsed < cutoffTime)
		{
			SpawnOne();

			// wait either a frame (for non-positive interval) or the configured seconds
			if (spawnInterval <= 0f)
			{
				yield return null; // next frame
				elapsed += Time.deltaTime;
			}
			else
			{
				yield return new WaitForSeconds(spawnInterval);
				elapsed += spawnInterval;
			}
		}

		spawnRoutine = null;
	}

	private void SpawnOne()
	{
		if (prefab == null)
			return;

		Transform point = spawnPoint != null ? spawnPoint : transform;

		GameObject go = Instantiate(prefab, point.position, point.rotation) as GameObject;
		if (go != null)
		{
			spawnCounter++;
			// name like "Skeleton #1" (avoids the default "(Clone)" suffix)
			go.name = string.Format("{0} #{1}", prefab.name, spawnCounter);
		}
	}
}

