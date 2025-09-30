using Pathfinding;
using System.Collections;
using UnityEngine;

public class HealthEnemy : MonoBehaviour
{
    public Animator animator;
    public Rigidbody2D enemy;
    public int damagePerHit = 1;
    public int enemyHealth = 5;

    private AIPath aiPath;
    private Collider2D col;

    void Start()
    {
        animator = GetComponent<Animator>();
        aiPath = GetComponent<AIPath>();
        col = GetComponent<Collider2D>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !collision.gameObject.CompareTag("PlayerWeapon"))
        {
            var player = collision.gameObject.GetComponent<PlayerScript>();
            if (player != null)
            {
                player.TakeDamage(damagePerHit);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        StartCoroutine(HitState(damage));
    }

    private IEnumerator HitState(int damage)
    {
        animator.SetTrigger("IsHit");
        animator.ResetTrigger("IsNotHit");
        enemyHealth -= damage;
        if (enemyHealth <= 0)
        {
            enemyHealth = 0;
            StartCoroutine(Die());
        }
        yield return new WaitForSeconds(0.1f);
        animator.SetTrigger("IsNotHit");
    }

    private IEnumerator Die()
    {
        Debug.Log($"{gameObject.name} has died!");
        animator.SetInteger("State", -1);
        if (aiPath != null) aiPath.enabled = false;
        if (col != null) col.enabled = false;
        if (enemy != null) enemy.bodyType = RigidbodyType2D.Static;
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}
