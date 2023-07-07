using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Damage : MonoBehaviour
{

    public int damagePerHit;
    public int damage;

    public float seconds;
    void Start()
    {
        damage = 0;
    }

    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log(collision.gameObject.tag);
        if (collision.gameObject.tag == "Player")
        {
            StartCoroutine(GiveDamage(collision.gameObject, seconds));
        }
    }

    IEnumerator GiveDamage(GameObject player, float seconds)
    {
        Debug.Log("Collision enemys");
        damage += damagePerHit;
        Debug.Log(damage);
        player.GetComponent<PlayerMovement>().TakeDamage(damagePerHit);
        damage = 0;
        yield return new WaitForSeconds(seconds);
    }
}
