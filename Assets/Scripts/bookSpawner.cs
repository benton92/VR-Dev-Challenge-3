using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bookSpawner : MonoBehaviour
{

    public GameObject spellBook;
    public GameObject smallImplosion;
    public GameObject smallExplosion;
    public bool handsInPosition;
    public bool bookIsSummonable;
    public bool bookIsDesummonable;
    public bool bookIsSummonded;

    private GameObject SpellBookClone;

    // Start is called before the first frame update
    void Start()
    {
        handsInPosition = false;
        bookIsSummonable = true;
        bookIsDesummonable = false;
        bookIsSummonded = false;
        handsSetTrue();
        Invoke(nameof(despawnBook), 4f);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void handsSetTrue()
    {
        handsInPosition = true;
        if (bookIsSummonable)
        { spawnBook(); }
    }

    public void handsSetFalse()
    {
        handsInPosition = false;
        if (bookIsDesummonable)
        {
            despawnBook();
        }
    }

    public void spawnBook()
    {
        //if hands are still in position for book spawn book with a flash
        bookIsSummonable = false;
        GameObject ExClone = Instantiate(smallExplosion, transform.position, transform.rotation);
        Destroy(ExClone, 5f);
        StartCoroutine(spawnBook2());
    }

    IEnumerator spawnBook2()
    {
        yield return new WaitForSeconds(0.5f);
        SpellBookClone = Instantiate(spellBook, transform.position, transform.rotation);
        bookIsDesummonable = true;
        bookIsSummonded = true;
    }

    public void despawnBook()
    {
        bookIsDesummonable = false;
        GameObject ImplosionClone = Instantiate(smallImplosion, transform.position, transform.rotation);
        Destroy(ImplosionClone, 5f);
        Destroy(SpellBookClone, 0.1f);
        bookIsSummonable = true;
        bookIsSummonded = false;
    }
}
