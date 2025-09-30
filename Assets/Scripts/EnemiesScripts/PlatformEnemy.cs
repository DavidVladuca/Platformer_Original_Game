using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlatformEnemy : MonoBehaviour
{
    [Header("References")]
    public Transform sage;
    public static bool turn = false;

    [Header("Movement")]
    public float speed = 1f;
    public float flippedTranslate = 0.2f;
    public int direction = 1;

    [Header("Attack")]
    public int damagePerHit = 1;
    public float attackRange = 1.5f;
    public float attackCooldown = 2f;
    public float attackTime = 0.6f;

    [Header("Health / Animator")]
    public int enemyHealth = 5;
    public Animator animator;

    private Rigidbody2D rb;
    private Collider2D col;
    private float tileMinX;
    private float tileMaxX;
    private bool isActive = false;
    private float lastAttackTime = -999f;
    private bool isAttacking = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (animator == null) animator = GetComponent<Animator>();

        Bounds b = col.bounds;
        float halfWidth = b.extents.x;
        tileMinX = transform.position.x - halfWidth - 0.01f;
        tileMaxX = transform.position.x + halfWidth + 0.01f;

        if (sage == null)
        {
            var s = GameObject.FindWithTag("Player");
            if (s) sage = s.transform;
        }

        var playerCollider = FindObjectOfType<PlayerScript>()?.GetComponent<Collider2D>();
        if (playerCollider != null)
        {
            Physics2D.IgnoreCollision(col, playerCollider, false);
        }
    }

    void Update()
    {
        if (enemyHealth <= 0) return;

        if (sage != null)
        {
            bool sameXCell = sage.position.x >= tileMinX && sage.position.x <= tileMaxX;
            bool sameY = Mathf.Abs(sage.position.y - transform.position.y) < (col.bounds.extents.y + 0.5f);
            isActive = sameXCell && sameY;
        }

        if (!isAttacking && isActive && sage != null)
        {
            float dist = Vector2.Distance(transform.position, sage.position);
            if (dist <= attackRange && Time.time >= lastAttackTime + attackCooldown)
            {
                StartCoroutine(DoAttack());
            }
        }
    }

    void FixedUpdate()
    {
        if (enemyHealth <= 0) return;

        if (isActive)
        {
            Vector2 vel = rb.velocity;
            vel.x = direction * speed;
            rb.velocity = new Vector2(vel.x, rb.velocity.y);

            Vector2 pos = rb.position;
            pos.x = Mathf.Clamp(pos.x, tileMinX, tileMaxX);
            rb.position = pos;
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    IEnumerator DoAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        if (animator) animator.SetTrigger("Attack");
        float prevSpeed = speed;
        speed = 0f;
        yield return new WaitForSeconds(attackTime);

        if (sage != null && Vector2.Distance(transform.position, sage.position) <= attackRange)
        {
            var playerMovement = sage.GetComponent<PlayerMovement>();
            var playerScript = sage.GetComponent<PlayerScript>();
            if (playerMovement != null) playerMovement.TakeDamage(damagePerHit);
            else if (playerScript != null) playerScript.TakeDamage(damagePerHit);
        }

        speed = prevSpeed;
        isAttacking = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (enemyHealth <= 0) return;
        if (collision.gameObject.CompareTag("Player"))
        {
            var ps = collision.gameObject.GetComponent<PlayerScript>();
            var pm = collision.gameObject.GetComponent<PlayerMovement>();
            if (pm != null) pm.TakeDamage(damagePerHit);
            else if (ps != null) ps.TakeDamage(damagePerHit);
        }
    }

    public void TakeDamage(int damage)
    {
        if (enemyHealth <= 0) return;
        animator?.SetTrigger("IsHit");
        enemyHealth -= damage;
        if (enemyHealth <= 0)
        {
            enemyHealth = -1;
            animator?.SetInteger("State", -1);
            rb.velocity = Vector2.zero;
            enabled = false;
        }
    }

    public void FlipDirectionWithNudge()
    {
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
        float nudge = (direction == 1) ? -flippedTranslate : flippedTranslate;
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x + nudge, tileMinX, tileMaxX);
        transform.position = p;
        direction *= -1;
    }
}
