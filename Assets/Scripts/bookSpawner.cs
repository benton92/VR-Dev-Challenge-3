using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bookSpawner : MonoBehaviour
{
    public GameObject spellBook;
    public GameObject smallImplosion;
    public GameObject smallExplosion;
    [SerializeField] private float bookXRotation = 0f;

    public bool handsInPosition;
    public bool bookIsSummonable;
    public bool bookIsDesummonable;
    public bool bookIsSummonded;

    private GameObject SpellBookClone;

    void Start()
    {
        handsInPosition = false;
        bookIsSummonable = true;
        bookIsDesummonable = false;
        bookIsSummonded = false;
        handsSetTrue();
        Invoke(nameof(despawnBook), 4f);
    }

    void Update()
    {
    }

    public void handsSetTrue()
    {
        handsInPosition = true;
        if (bookIsSummonable)
        {
            spawnBook();
        }
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
        bookIsSummonable = false;
        GameObject ExClone = Instantiate(smallExplosion, transform.position, transform.rotation);
        Destroy(ExClone, 5f);
        StartCoroutine(spawnBook2());
    }

    IEnumerator spawnBook2()
    {
        yield return new WaitForSeconds(0.5f);

        Quaternion bookRotation = Quaternion.Euler(bookXRotation, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
        SpellBookClone = Instantiate(spellBook, transform.position, bookRotation);

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