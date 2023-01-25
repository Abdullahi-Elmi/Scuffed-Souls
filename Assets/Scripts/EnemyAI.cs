using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EnemyAI : MonoBehaviour
{
    private Rigidbody2D rb;
    [Header("Assets")][SerializeField] private Transform target;
    [SerializeField] private Slider healthBarSlider;
    //[SerializeField] private GameObject winScreen;
    [SerializeField] private AudioClip hurtClip;
    private float maxHealth = 100f;
    private float currentHealth;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren(typeof(Animator)) as Animator;
        target = GameObject.FindWithTag("Player").transform;
        currentHealth = maxHealth;
        healthBarSlider.maxValue = maxHealth;
        healthBarSlider.value = maxHealth;
        movementSpeed = 5f;
        attackCooldownTimer = Time.time + attackCooldown;
    }

    // Update is called once per frame
    void Update()
    {
        CheckConditions();
        Animate();
        if (dying || beingHit || attacking) return;
        if (Time.time > attackCooldownTimer && Time.time > attackTimer)
        {
            StartCoroutine(Attack());
            return;
        }
        Move();
        if (facingRight == false && direction > 0)
            Flip();
        if (facingRight == true && direction < 0)
            Flip();
    }

    #region Moving
    [Header("Movement")]
    [SerializeField] private float movementSpeed;
    private float direction;
    private bool outOfRange;
    void Move()
    {
        direction = (target.position.x > transform.position.x) ? 1 : -1;
        float difference = target.position.x - transform.position.x;
        outOfRange = Mathf.Abs(difference) > 4.5f;
        bool stoppingRange = Mathf.Abs(difference) < 3f;
        if (outOfRange && !dying) rb.velocity = new Vector2(direction * movementSpeed, rb.velocity.y);
        if (stoppingRange) rb.velocity = Vector2.zero;
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

    #region Attacking
    [Header("Attacking")][SerializeField] private LayerMask playerLayers;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 2f;
    
    private float attackAnimationLength = 2f;
    private float attackTimer;
    private float attackCooldown = 4f; // attack every x seconds
    private float attackCooldownTimer;

    IEnumerator Attack()
    {
        attacking = true;
        attackTimer = Time.time + attackAnimationLength;
        yield return new WaitForSeconds(attackAnimationLength - 1f);
        DoDamage();
        attackCooldownTimer = Time.time + attackCooldown;
    }

    void DoDamage()
    {
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayers);
        foreach (Collider2D player in hitPlayers)
        {
            player.GetComponent<PlayerController>().TakeDamage(34f);
        }
    }
    #endregion

    #region Damage
    private float hitTimer;
    private float hitDuration = 0.5f;
    public void TakeDamage(float damage)
    {
        if (attacking) return;
        currentHealth -= damage;
        healthBarSlider.value = currentHealth;
        beingHit = true;
        hitTimer = Time.time + hitDuration;
        AudioManager.Instance.PlaySound(hurtClip);

        // Play Hit Animation

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        dying = true;
        Destroy(gameObject, 1.2f);
    }

    //void YouWon()
    //{
    //    winScreen.SetActive(true);
    //}

    //void ResetGame()
    //{
    //    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    //}
    #endregion

    #region Animation
    private static readonly int Idle_Anim = Animator.StringToHash("Enemy_Idle");
    private static readonly int Run_Anim = Animator.StringToHash("Enemy_Run");
    private static readonly int Attack_Anim = Animator.StringToHash("Enemy_Attack");
    private static readonly int Hit_Anim = Animator.StringToHash("Enemy_Hit");
    private static readonly int Death_Anim = Animator.StringToHash("Enemy_Death");

    private Animator anim;
    private int currentState;
    private float lockedTill;

    public bool dying;
    private bool beingHit;
    private bool attacking;
    private bool isMoving;
    void CheckConditions()
    {
        if (Time.time > attackTimer)
            attacking = false;
        if (Time.time > hitTimer)
            beingHit = false;
        // if hitTimer expired, we're no longer considering ourselves to be hit
        //beingHit = !(Time.time > hitTimer);
        isMoving = rb.velocity.magnitude > 0f;
    }

    void Animate()
    {
        var state = GetState();
        if (state == currentState) return;
        anim.CrossFade(state, 0, 0);
        currentState = state;
    }

    private int GetState()
    {
        if (Time.time < lockedTill) return currentState;

        // Priorities
        if (dying) return Death_Anim;
        if (attacking) return LockState(Attack_Anim, attackAnimationLength);
        if (beingHit) return LockState(Hit_Anim, hitDuration);
        return isMoving ? Run_Anim : Idle_Anim;

        int LockState(int s, float t)
        {
            lockedTill = Time.time + t;
            return s;
        }
    }
    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
