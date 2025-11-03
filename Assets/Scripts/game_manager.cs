using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class game_manager : MonoBehaviour
{
    [Tooltip("Name of the tutorial scene")] public string tutorialSceneName = "Tutorial Scene";
    [Tooltip("Name of the bridge scene")] public string bridgeSceneName = "bridge";
    [Tooltip("Seconds to wait before switching scenes")] public float tutorialDuration = 120f;

    private bool switched = false;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Only start timer if we're in the tutorial scene
        if (SceneManager.GetActiveScene().name == tutorialSceneName)
        {
            StartCoroutine(TutorialTimer());
        }
    }

    private IEnumerator TutorialTimer()
    {
        yield return new WaitForSeconds(tutorialDuration);
        // Switch to bridge scene
        SceneManager.LoadScene(bridgeSceneName);
        switched = true;
    }

    void Update()
    {
        // If we've switched scenes, disable this manager (player rig is already in bridge)
        if (switched && SceneManager.GetActiveScene().name == bridgeSceneName)
        {
            // Optionally destroy or disable this manager
            gameObject.SetActive(false);
        }
    }
}
