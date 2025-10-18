using System;
using UnityEngine;
using FirstGearGames.SmoothCameraShaker;

[DisallowMultipleComponent]
public class Enemy : MonoBehaviour
{
    [Header("Can Ayarları")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHealth;
    private int damageOnDeathArea;

    [Header("Hareket Ayarları")]
    public Transform target;                 
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float stopDistance = 0.5f;

    [Header("Görsel Efekt")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.2f;

    [Header("Silah Kontrolü")]
    [SerializeField] private bool hasGun = true;
    [SerializeField] private EnemyGun gun;
    [SerializeField] private bool controlGunByDistance = true;
    [SerializeField] private float gunActivateRange = 5f;
    [SerializeField] private float gunDeactivateRange = 5.5f;

    public ShakeData shakeData;

    public event Action OnEnemyDied;

    private Rigidbody2D rb2d;
    private Rigidbody rb3d;

    private Color originalColor;
    private bool isFlashing = false;
    private bool isDead = false;

    private bool facingRight = false; // şu an hangi yöne bakıyor

    private void Awake()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        rb2d = GetComponent<Rigidbody2D>();
        rb3d = GetComponent<Rigidbody>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (hasGun && gun == null)
            gun = GetComponentInChildren<EnemyGun>(includeInactive: true);

        if (hasGun && gun != null)
            gun.enabled = false;
    }

    private void Update()
    {
        HandleGunActivation();
        HandleFacingDirection(); // 👈 yeni yön kontrolü
    }

    private void FixedUpdate()
    {
        if (isDead || target == null) return;

        Vector3 toTarget = target.position - transform.position;
        float dist = toTarget.magnitude;
        if (dist <= stopDistance) return;

        Vector3 dir = toTarget.normalized;
        Vector3 step = dir * moveSpeed * Time.fixedDeltaTime;

        if (rb2d != null) rb2d.MovePosition(transform.position + step);
        else if (rb3d != null) rb3d.MovePosition(transform.position + step);
        else transform.position += step;
    }

    // 🔹 Sprite yönünü hedefe göre değiştir
    private void HandleFacingDirection()
    {
        if (target == null || spriteRenderer == null) return;

        // Hedef sağda mı solda mı?
        if (target.position.x > transform.position.x && !facingRight)
        {
            Flip(false);
        }
        else if (target.position.x < transform.position.x && facingRight)
        {
            Flip(true);
        }
    }

    private void Flip(bool lookRight)
    {
        facingRight = lookRight;

        Vector3 scale = transform.localScale;
        scale.x = lookRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    private void HandleGunActivation()
    {
        if (!hasGun || gun == null || target == null) return;

        if (!controlGunByDistance)
        {
            if (!gun.enabled) gun.enabled = true;
            return;
        }

        float dist = Vector2.Distance(transform.position, target.position);
        // Debug.Log(dist);

        if (!gun.enabled && dist <= gunActivateRange)
            gun.enabled = true;
        else if (gun.enabled && dist >= gunDeactivateRange)
            gun.enabled = false;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - Mathf.Abs(amount));
        FlashColor();

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        CameraShakerHandler.Shake(shakeData);
        OnEnemyDied?.Invoke();
        Destroy(gameObject);
    }

    private void FlashColor()
    {
        if (spriteRenderer == null || isFlashing) return;
        StartCoroutine(FlashRoutine());
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        isFlashing = true;
        spriteRenderer.color = hitColor;
        yield return new WaitForSeconds(hitFlashDuration);
        if (!isDead) spriteRenderer.color = originalColor;
        isFlashing = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("DeathArea"))
        {
            var b = other.GetComponent<Bullet>();
            if (b != null)
            {
                damageOnDeathArea = b.damage;
                Destroy(other.gameObject);
                TakeDamage(damageOnDeathArea);
            }
        }
    }

    public void SetTarget(Transform newTarget) => target = newTarget;
    public int GetHealth() => currentHealth;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, .35f);
        Gizmos.DrawWireSphere(transform.position, gunActivateRange);
        Gizmos.color = new Color(1f, .5f, 0f, .25f);
        Gizmos.DrawWireSphere(transform.position, gunDeactivateRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
#endif
}
