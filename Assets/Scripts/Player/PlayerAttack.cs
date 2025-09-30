using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public int damagePerHit;
    public int damage;
    public float secondsCoolDown;

    void Start()
    {
        damage = 0;
    }

    void Update() { }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"{gameObject} collided with {collision.gameObject}");
        if (collision.gameObject.tag == "enemyCollider" || collision.gameObject.tag == "enemyWeapon")
        {
            StartCoroutine(GiveDamage(collision.gameObject, secondsCoolDown));
        }
    }

    IEnumerator GiveDamage(GameObject enemy, float seconds)
    {
        damage += damagePerHit;
        enemy.GetComponent<HealthEnemy>().TakeDamage(damagePerHit);
        damage = 0;
        yield return new WaitForSeconds(seconds);
    }
}
