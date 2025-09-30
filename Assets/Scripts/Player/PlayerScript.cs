using System.Collections;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    [SerializeField] private int maxHealth;
    public int health;
    private int noHearts;
    static public bool isDamaged;
    public Animator animator;
    public int damagePerHit;
    public int damage;
    public float secondsCoolDown;
    public float secondsAttackAnimation;

    void Start()
    {
        noHearts = 3;
        health = maxHealth;
        isDamaged = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(StopAndAttack(secondsAttackAnimation));
        }
    }

    public void TakeDamage(int damage)
    {
        animator.SetInteger("State", 4);
        if (health - damage > 0) health -= damage;

        if (noHearts >= 0 && health <= 0)
        {
            noHearts--;
            health = maxHealth;
            GameObject.Find("GameManager").GetComponent<GameManager>().Die();
        }
        animator.SetInteger("State", 0);
    }

    public void LoseLife()
    {
        noHearts--;
        health = maxHealth;
        GameObject.Find("GameManager").GetComponent<GameManager>().Die();
    }

    IEnumerator StopAndAttack(float seconds)
    {
        animator.SetTrigger("Attack");
        yield return new WaitForSeconds(seconds);
        animator.SetInteger("State", 1);
    }
}
