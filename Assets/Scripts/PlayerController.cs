using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    [Header("Assets")][SerializeField] private Slider healthBarSlider;
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject loseScreen;
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip loseClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip dodgeClip;
    [SerializeField] private AudioClip hurtClip;

    private float maxHealth = 100f;
    private float currentHealth;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<CapsuleCollider2D>();
        anim = GetComponentInChildren(typeof(Animator)) as Animator;
        movementSpeed = 10f;
        currentHealth = maxHealth;
        healthBarSlider.maxValue = maxHealth;
        healthBarSlider.value = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        GatherInput();
        CheckConditions();
        Animate();
        if (dying || Time.time < attackTimer || Time.time < dodgeTimer || Time.time < hitTimer) return;
        if (attackInput && isGrounded && Time.time > attackCooldownTimer)
        {
            StartCoroutine(Attack());
            return;
        }
        if (dodgeInput && isGrounded)
        {
            StartCoroutine(Dodge());
            return;
        }
        Jump();
        Move();
        if (facingRight == false && movementInput > 0)
            Flip();
        if (facingRight == true && movementInput < 0)
            Flip();
    }

    void FixedUpdate()
    {
        if(transform.position.y < 0f)
        {
            Vector3 correction = new Vector3(transform.position.x, 0, transform.position.z);
            transform.position = correction;
        }
        //Move();
        //if (facingRight == false && movementInput > 0)
        //    Flip();
        //if (facingRight == true && movementInput < 0)
        //    Flip();
    }

    #region Inputs
    private float movementInput;
    private bool jumpInput;
    private bool attackInput;
    private bool dodgeInput;
    private bool resetInput;
    void GatherInput()
    {
        jumpInput = Input.GetButtonDown("Jump");
        movementInput = Input.GetAxisRaw("Horizontal");
        attackInput = Input.GetMouseButtonDown(0);
        dodgeInput = Input.GetMouseButtonDown(1);
        isGrounded = Physics2D.OverlapCircle(bottom.position, checkRadius, surface);
        resetInput = Input.GetKeyDown(KeyCode.Escape);
        if (resetInput) ResetGame();
    }
    #endregion

    #region Moving
    [Header("Movement")]
    [SerializeField] private float movementSpeed;
    void Move()
    {
        rb.velocity = new Vector2(movementInput * movementSpeed, rb.velocity.y);
    }

    private bool facingRight = true;
    void Flip()
    {
        facingRight = !facingRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }
    #endregion

    #region Jumping
    public static bool isGrounded;
    [Header("Jumping")][SerializeField] private float jumpSpeed;
    [SerializeField] private float checkRadius;
    [SerializeField] private Transform bottom;
    [SerializeField] private LayerMask surface;
    void Jump()
    {
        isGrounded = Physics2D.OverlapCircle(bottom.position, checkRadius, surface);
        if (jumpInput && isGrounded)
        {
            rb.AddForce(Vector2.up * jumpSpeed, ForceMode2D.Impulse);
            AudioManager.Instance.PlaySound(jumpClip);
        }
    }
    #endregion

    #region Dodging
    [Header("Dodging")][SerializeField] private CapsuleCollider2D myCollider;
    [SerializeField] public static float dodgeDuration = 0.5f;
    private float dodgeTimer;
    IEnumerator Dodge()
    {
        dodgeTimer = Time.time + dodgeDuration;
        //rb.constraints = RigidbodyConstraints2D.FreezePositionY;
        //myCollider.enabled = false;
        if (facingRight)
            rb.velocity = new Vector2(+1 * movementSpeed * 2f, 0); // 1.5f
        else
            rb.velocity = new Vector2(-1 * movementSpeed * 2f, 0);

        Physics2D.IgnoreLayerCollision(3, 7, true);
        AudioManager.Instance.PlaySound(dodgeClip);
        yield return new WaitForSeconds(dodgeDuration);
        Physics2D.IgnoreLayerCollision(3, 7, false);
        //myCollider.enabled = true;
        //rb.constraints = RigidbodyConstraints2D.None;
        //rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }
    #endregion

    #region Attacking

    [Header("Attacking")][SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] public static float attackDuration = 0.3f;
    [SerializeField] private float attackCooldown = 0.4f;
    private float attackTimer;
    public static float attackCooldownTimer;
    IEnumerator Attack()
    {
        attackTimer = Time.time + attackDuration;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            enemy.GetComponent<EnemyAI>().TakeDamage(4f);
            // Check if we killed the boss
            bool killedBoss = enemy.GetComponent<EnemyAI>().dying;
            if (killedBoss)
            {
                Invoke("Win", 1.2f);
                Invoke("ResetGame", 3.0f);
            }
        }

        yield return new WaitForSeconds(attackDuration);
        attackCooldownTimer = Time.time + attackCooldown;
    }
    #endregion


    #region Damage
    private float hitTimer;
    private bool beingHit;
    private float hitDuration = 0.5f;
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        healthBarSlider.value = currentHealth;
        beingHit = true;
        hitTimer = Time.time + hitDuration;
        AudioManager.Instance.PlaySound(hurtClip);
        if (currentHealth <= 0f)
        {
            // Death
            dying = true;
            Invoke("Lose", 0.5f);
            Invoke("ResetGame", 2.0f);
        }
    }
    #endregion

    #region Animation
    private bool attacking;
    private bool dodging;
    private bool dying = false;

    void CheckConditions()
    {
        attacking = attackInput && isGrounded && Time.time > attackCooldownTimer;
        dodging = dodgeInput && isGrounded;
        if (Time.time > hitTimer)
            beingHit = false;
    }

    private static readonly int Idle_Anim = Animator.StringToHash("Player_Idle");
    private static readonly int Run_Anim = Animator.StringToHash("Player_Run");
    private static readonly int Dodge_Anim = Animator.StringToHash("Player_Roll");
    private static readonly int Jump_Anim = Animator.StringToHash("Player_Jump");
    private static readonly int Attack_Anim = Animator.StringToHash("Player_Attack");
    private static readonly int Hit_Anim = Animator.StringToHash("Player_Hit");
    private static readonly int Death_Anim = Animator.StringToHash("Player_Death");

    private Animator anim;
    private int currentState;
    private float lockedTill;

    private int GetState()
    {
        if (Time.time < lockedTill) return currentState;

        // Priorities
        if (dying) return Death_Anim;
        if (beingHit) return LockState(Hit_Anim, hitDuration);
        if (attacking) return LockState(Attack_Anim, attackDuration);
        if (dodging) return LockState(Dodge_Anim, dodgeDuration);
        if (!isGrounded) return Jump_Anim;
        if (isGrounded) return movementInput == 0 ? Idle_Anim : Run_Anim;

        int LockState(int s, float t)
        {
            lockedTill = Time.time + t;
            return s;
        }
        return Idle_Anim;
    }

    void Animate()
    {
        var state = GetState();
        if (state == currentState) return;
        anim.CrossFade(state, 0, 0);
        currentState = state;
    }
    #endregion

    #region End Game
    void Win()
    {
        AudioManager.Instance.PlaySound(winClip);
        winScreen.SetActive(true);
    }

    void Lose()
    {
        AudioManager.Instance.PlaySound(loseClip);
        loseScreen.SetActive(true);
    }

    void ResetGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(bottom.position, checkRadius);
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
