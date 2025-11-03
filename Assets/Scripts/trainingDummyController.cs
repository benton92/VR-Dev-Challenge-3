using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class trainingDummyController : MonoBehaviour
{
    public Animator animator;
    public GameObject poof;
    // Start is called before the first frame update
    void Start()
    {
        Invoke(nameof(startAnimation), 3f);
    }

    // Update is called once per frame
    void Update()
    {
        //check for falling over animation completion
        //call replacement animation
        if (Input.GetKeyDown(KeyCode.Space))
        {
            startAnimation();
        }
    }

    public void startAnimation()
    {
        //Start falling over animation
        animator.SetTrigger("StartFalling");
        Invoke(nameof(spawnPoof), 4f);
    }

    public void spawnPoof()
    {
        GameObject clone = Instantiate(poof, transform.position, transform.rotation);
        animator.SetTrigger("Respawn");
        Destroy(clone, 5f);
    }
}
