using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class PlayerMovement : MonoBehaviour
{
    public Action<float, float> OnShockwave;

    private Rigidbody2D rb;
    private BoxCollider2D coll;
    private SpriteRenderer sprite;
    private Animator anim;
    private Animator shockAnimator;

    [SerializeField] private LayerMask jumpableGround;
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float dashForce = 10f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashColdown = 5.0f;
    [SerializeField] private float initialJumpForce = 5f;
    [SerializeField] private float holdJumpForce = 2f;
    [SerializeField] private float maxJumpTime = 0.75f;
    [SerializeField] private float dropForce = 1.0f;
    [SerializeField] private float shockwaveSpeed = 15.0f;
    [SerializeField] private float shockwaveStartDist = 3.0f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource audioSource2;
    [SerializeField] private float flippedTranslate = 2.5f;
    [SerializeField] private float wallJumpDelay = 0.5f;
    [SerializeField] private float soundDistance = 0.5f;

    private float dirX = 0;
    private float lastDirX = 0;
    private float jumpTime;
    private float wallJumpTime = 0;
    private bool isDashing;
    private float timeDash = 0;
    private bool soundPlayed = false;
    private bool isDropping = false;
    private bool isFlipped = false;
    private bool jumpPressed;
    private bool additionalJumpForceRequired;
    private bool dropPressed;

    private int health;
    public int fullHealth;
    public int noHearts;
    static public bool isDamaged;
    public int damagePerHit;
    public int damage;
    public float secondsCoolDown;
    public float secondsAttackAnimation;

    private enum MovementState
    {
        idle, running, jumping, falling, damage, death, dashing, down
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<BoxCollider2D>();
        sprite = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        shockAnimator = transform.Find("Shockwave").GetComponent<Animator>();

        rb.freezeRotation = true;
        noHearts = 3;
        health = fullHealth;
        isDamaged = false;
    }

    private void Update()
    {
        var gm = GameObject.Find("GameManager").GetComponent<GameManager>();

        if (!gm.gamePaused && noHearts > 0)
        {
            // Movement input
            if (Input.GetKey(KeyCode.A))
            {
                dirX = -1;
                lastDirX = dirX;
                if (!isFlipped)
                {
                    Vector3 theScale = transform.localScale;
                    theScale.x *= -1;
                    transform.localScale = theScale;
                    isFlipped = true;
                }
            }
            else if (Input.GetKey(KeyCode.D))
            {
                dirX = 1;
                lastDirX = dirX;
                if (isFlipped)
                {
                    Vector3 theScale = transform.localScale;
                    theScale.x *= -1;
                    transform.localScale = theScale;
                    isFlipped = false;
                }
            }
            else dirX = 0;

            gm.SetDash(Mathf.Clamp01((timeDash - Time.time) / dashColdown));

            // Dash input
            if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing && Time.time > timeDash)
            {
                StartCoroutine(Dash());
            }

            // Jump input
            if (Input.GetKeyDown(KeyCode.W) && IsGrounded() && IsObstacleInFront() && !IsGroundedDown())
            {
                jumpPressed = true;
            }

            if (Input.GetKey(KeyCode.W) && !IsObstacleOnTop())
            {
                additionalJumpForceRequired = true;
            }

            // Drop input
            if (Input.GetKeyDown(KeyCode.S) && !IsOverGround(shockwaveStartDist))
            {
                dropPressed = true;
            }
            else if (!Input.GetKey(KeyCode.S) && isDropping)
            {
                isDropping = false;
            }

            // Attack input
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartCoroutine(StopAndAttack(secondsAttackAnimation));
            }

            UpdateAnimationState();
        }
    }

    private void FixedUpdate()
    {
        if (noHearts <= 0) return;

        // Horizontal movement
        if (!isDashing)
        {
            if (IsObstacleInFront())
                rb.velocity = new Vector2(0, rb.velocity.y);
            else
                rb.velocity = new Vector2(dirX * moveSpeed, rb.velocity.y);
        }

        // Jumping logic
        if (jumpPressed)
        {
            if (IsObstacleInFront() && !IsGroundedDown() && Time.time > wallJumpTime)
            {
                wallJumpTime = Time.time + wallJumpDelay;
                rb.velocity = new Vector3(rb.velocity.x, initialJumpForce);
                jumpTime = Time.time + maxJumpTime;
            }
            if (IsGrounded())
            {
                rb.velocity = new Vector3(rb.velocity.x, initialJumpForce);
                jumpTime = Time.time + maxJumpTime;
                jumpPressed = false;
            }
        }

        if (additionalJumpForceRequired)
        {
            if (Time.time < jumpTime)
                rb.AddForce(new Vector2(0, holdJumpForce), ForceMode2D.Force);

            if (IsObstacleInFront() && !IsGroundedDown() && Time.time > wallJumpTime)
            {
                rb.velocity = new Vector3(rb.velocity.x, initialJumpForce);
                jumpTime = Time.time + maxJumpTime;
                wallJumpTime = Time.time + wallJumpDelay;
            }

            if (IsGrounded())
            {
                rb.velocity = new Vector3(rb.velocity.x, initialJumpForce);
                jumpTime = Time.time + maxJumpTime;
            }

            additionalJumpForceRequired = false;
        }

        // Dropping logic
        if (dropPressed)
        {
            isDropping = true;
            soundPlayed = false;
            rb.AddForce(new Vector2(0, -dropForce), ForceMode2D.Impulse);
            dropPressed = false;
        }

        if (isDropping && soundStarter() && Math.Abs(rb.velocity.y) >= shockwaveSpeed) { }

        if ((isDropping && IsOverGround(shockwaveStartDist)) && Math.Abs(rb.velocity.y) >= shockwaveSpeed)
        {
            audioSource.Play();
            soundPlayed = true;
            OnShockwave?.Invoke(5, 0.3f);
            anim.SetTrigger("Down");
            shockAnimator.SetTrigger("Shock");
            isDropping = false;
        }
        else
        {
            anim.ResetTrigger("Down");
            shockAnimator.ResetTrigger("Shock");
        }
    }

    private IEnumerator Dash()
    {
        GameObject.Find("GameManager").GetComponent<GameManager>().SetDash(0);
        isDashing = true;
        audioSource2.Play();
        rb.AddForce(new Vector2(lastDirX * dashForce, 0), ForceMode2D.Impulse);
        yield return new WaitForSeconds(dashDuration);
        isDashing = false;
        timeDash = Time.time + dashColdown;
    }

    private void UpdateAnimationState()
    {
        MovementState state;

        if (dirX > 0f || dirX < 0f) state = MovementState.running;
        else state = MovementState.idle;

        if (rb.velocity.y > 0.15f) state = MovementState.jumping;
        else if (rb.velocity.y < -0.15f) state = MovementState.falling;

        if (isDashing) state = MovementState.dashing;
        if (noHearts < 1) state = MovementState.death;

        anim.SetInteger("State", (int)state);
    }

    private bool IsGrounded()
    {
        return Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.down, 0.4f, jumpableGround).collider != null;
    }

    private bool soundStarter()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, -Vector2.up);
        if (hit.collider != null)
            return hit.distance <= soundDistance;
        return false;
    }

    private bool IsGroundedDown()
    {
        RaycastHit2D hit = Physics2D.Raycast(coll.bounds.center, Vector2.down, coll.bounds.extents.y + 0.2f, jumpableGround);
        return hit.collider != null;
    }

    private bool IsOverGround(float dist)
    {
        return Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.down, dist, jumpableGround).collider != null;
    }

    private bool IsObstacleInFront()
    {
        return Physics2D.BoxCast(coll.bounds.center, coll.bounds.size - new Vector3(0, 0.1f), 0f, new Vector2(dirX, 0), 0.1f, jumpableGround).collider != null;
    }

    private bool IsObstacleOnTop()
    {
        RaycastHit2D hit = Physics2D.Raycast(coll.bounds.center, Vector2.up, 1f, jumpableGround);
        return hit.collider != null;
    }

    public void TakeDamage(int damage)
    {
        anim.SetTrigger("IsHit");
        health -= damage;

        if (noHearts >= 0 && health <= 0)
        {
            noHearts--;
            health = fullHealth;
        }

        if (noHearts > 0) anim.SetInteger("State", 0);
        if (noHearts <= 0 || health <= 0)
        {
            health = -1;
            anim.SetInteger("State", 5);
            var gm = GameObject.Find("GameManager").GetComponent<GameManager>();
            StartCoroutine(RestartAfterDeath(gm));

        }
    }

    private IEnumerator RestartAfterDeath(GameManager gm)
    {
        // get length of current animation
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        float waitTime = stateInfo.length;

        // wait for the animation to finish
        yield return new WaitForSeconds(waitTime);

        gm.ResetGame();
    }

    private IEnumerator StopAndAttack(float seconds)
    {
        anim.SetTrigger("Attack");
        yield return new WaitForSeconds(seconds);
        anim.SetInteger("State", 1);
    }
}
