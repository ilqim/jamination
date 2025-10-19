using System;
using System.Collections;
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

    [Header("Görsel Efekt (Color Flash)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.2f;

    [Header("Particle FX (Hasar/Ölüm)")]
    [Tooltip("Sahnede duran, Play On Awake KAPALI instance'lar (opsiyonel)")]
    [SerializeField] private ParticleSystem hitFxInstance;
    [SerializeField] private ParticleSystem deathFxInstance;
    [Tooltip("Prefab verirsen instantiate edilir (opsiyonel)")]
    [SerializeField] private ParticleSystem hitFxPrefab;
    [SerializeField] private ParticleSystem deathFxPrefab;
    [Tooltip("Hit FX sprite'a child olsun mu? (Death FX her zaman bağımsız oluşturulur)")]
    [SerializeField] private bool attachFxToSprite = true;
    [Tooltip("Hit FX'i yüzey normaline hizala (2D için kabaca merkezden dışa doğru).")]
    [SerializeField] private bool alignFxToNormal = false;

    [Header("Ses Efektleri (Audio)")]
    [SerializeField] private AudioSource audioSource;     // 🔊 Inspector’dan ver
    [SerializeField] private AudioClip hitClip;           // hasar sesi
    [SerializeField] private AudioClip deathClip;         // ölüm sesi
    [Range(0f, 1f)] [SerializeField] private float audioVolume = 1f;

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
    private Collider2D myCol2D;

    private Color originalColor;
    private bool isFlashing = false;
    private bool isDead = false;

    private bool facingRight = false;

    private void Awake()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        rb2d = GetComponent<Rigidbody2D>();
        rb3d = GetComponent<Rigidbody>();
        myCol2D = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (hasGun && gun == null)
            gun = GetComponentInChildren<EnemyGun>(includeInactive: true);
        if (hasGun && gun != null)
            gun.enabled = false;

        // AudioSource varsa ayarları sıfırla
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }
    }

    private void Update()
    {
        HandleGunActivation();
        HandleFacingDirection();
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

        if (target.position.x > transform.position.x && !facingRight) Flip(false);
        else if (target.position.x < transform.position.x && facingRight) Flip(true);
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

        if (!gun.enabled && dist <= gunActivateRange)
            gun.enabled = true;
        else if (gun.enabled && dist >= gunDeactivateRange)
            gun.enabled = false;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - Mathf.Abs(amount));
        PlayHitSound(); // 🎧 vurulma sesi
        FlashColor();

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        PlayDeathSound(); // 🎧 ölüm sesi
        SpawnDeathFX(transform.position);
        CameraShakerHandler.Shake(shakeData);
        OnEnemyDied?.Invoke();
        Destroy(gameObject);
    }

    private void FlashColor()
    {
        if (spriteRenderer == null || isFlashing) return;
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        isFlashing = true;
        spriteRenderer.color = hitColor;
        yield return new WaitForSeconds(hitFlashDuration);
        if (!isDead) spriteRenderer.color = originalColor;
        isFlashing = false;
    }

    private void PlayHitFXAt(Vector3 pos)
    {
        Quaternion rot = Quaternion.identity;
        if (alignFxToNormal)
        {
            Vector2 normal = ((Vector2)pos - (Vector2)transform.position).normalized;
            float ang = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg;
            rot = Quaternion.Euler(0f, 0f, ang);
        }

        Transform parent = (attachFxToSprite && spriteRenderer != null) ? spriteRenderer.transform : null;

        if (hitFxInstance != null)
        {
            var t = hitFxInstance.transform;
            if (parent != null) t.SetParent(parent, worldPositionStays: false);
            t.SetPositionAndRotation(pos, rot);
            hitFxInstance.Play(true);
        }
        else if (hitFxPrefab != null)
        {
            var fx = Instantiate(hitFxPrefab, pos, rot, parent);
            fx.Play(true);
            DestroyAfterLifetime(fx);
        }
    }

    private void SpawnDeathFX(Vector3 pos)
    {
        ParticleSystem fx = null;

        if (deathFxInstance != null)
            fx = Instantiate(deathFxInstance, pos, Quaternion.identity, null);
        else if (deathFxPrefab != null)
            fx = Instantiate(deathFxPrefab, pos, Quaternion.identity, null);

        if (fx != null)
        {
            fx.Play(true);
            DestroyAfterLifetime(fx);
        }
    }

    private static void DestroyAfterLifetime(ParticleSystem ps)
    {
        var main = ps.main;
        float life = main.duration;

        switch (main.startLifetime.mode)
        {
            case ParticleSystemCurveMode.TwoConstants:
                life += main.startLifetime.constantMax;
                break;
            case ParticleSystemCurveMode.TwoCurves:
                life += Mathf.Max(main.startLifetime.constant, main.startLifetime.constantMax);
                break;
            default:
                life += main.startLifetime.constant;
                break;
        }

        UnityEngine.Object.Destroy(ps.gameObject, life + 0.1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("DeathArea")) return;
        var b = other.GetComponent<Bullet>();
        if (b == null) return;

        Vector3 hitPos = (myCol2D != null)
            ? (Vector3)myCol2D.ClosestPoint(other.bounds.center)
            : other.ClosestPoint(transform.position);

        PlayHitFXAt(hitPos);
        damageOnDeathArea = b.damage;
        Destroy(other.gameObject);
        TakeDamage(damageOnDeathArea);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("DeathArea")) return;
        var b = collision.collider.GetComponent<Bullet>();
        if (b == null) return;

        Vector3 hitPos = collision.GetContact(0).point;
        PlayHitFXAt(hitPos);
        damageOnDeathArea = b.damage;
        Destroy(collision.collider.gameObject);
        TakeDamage(damageOnDeathArea);
    }

    // 🔊 Ses Fonksiyonları
    private void PlayHitSound()
    {
        if (audioSource != null && hitClip != null)
            audioSource.PlayOneShot(hitClip, audioVolume);
    }

    private void PlayDeathSound()
    {
        if (audioSource != null && deathClip != null)
            audioSource.PlayOneShot(deathClip, audioVolume);
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
