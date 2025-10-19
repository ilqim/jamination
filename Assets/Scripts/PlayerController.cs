using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using FirstGearGames.SmoothCameraShaker;

public class PlayerController : MonoBehaviour
{
    public static event Action<int> OnTakeDamage;
    public static event Action OnRequestFadeOut;

    [Header("Hareket Ayarları")]
    public float moveSpeed = 10f;

    [Header("Hasar / Ölüm Görseli")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.2f;
    [SerializeField] private Color deathColor = new Color(1f, 0.2f, 0.2f);

    [Header("UI Referansları")]
    [SerializeField] private Image dialogHintImage;
    [SerializeField] private Image doorHintImage;

    [Header("Particle FX (Hasar/Ölüm)")]
    [Tooltip("Sahnede duran, Play On Awake KAPALI instance'lar (opsiyonel)")]
    [SerializeField] private ParticleSystem hitFxInstance;
    [SerializeField] private ParticleSystem deathFxInstance;
    [Tooltip("Prefab verirsen instantiate edilir (opsiyonel)")]
    [SerializeField] private ParticleSystem hitFxPrefab;
    [SerializeField] private ParticleSystem deathFxPrefab;
    [Tooltip("Hit FX sprite'a child olsun mu? Death FX BAĞIMSIZ oluşturulur.")]
    [SerializeField] private bool attachFxToSprite = true;

    [Header("Ses Efektleri (Audio)")]
    [SerializeField] private AudioSource audioSource;  // Inspector’dan ver
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip deathClip;
    [Range(0f, 1f)] [SerializeField] private float audioVolume = 1f;

    public ShakeData shakeData;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private bool facingRight = true;
    private Color originalColor;
    private bool isFlashing = false;
    private bool isDead = false;

    // Kapı/Diyalog
    private Door nearbyDoor = null;
    private bool isNearDoor = false;
    private bool isNearDialog = false;
    private bool dialogTriggered = false;

    private void OnEnable()
    {
        PlayerHealth.OnPlayerDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        PlayerHealth.OnPlayerDied -= HandlePlayerDied;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (dialogHintImage) dialogHintImage.enabled = false;
        if (doorHintImage) doorHintImage.enabled = false;

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }
    }

    void Update()
    {
        if (isDead) return;

        // giriş
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput.Normalize();

        // Kapı
        if (isNearDoor && nearbyDoor != null)
        {
            if (doorHintImage) doorHintImage.enabled = true;
            if (Input.GetKeyDown(KeyCode.E))
            {
                nearbyDoor.TryOpen();
                return;
            }
        }
        else if (doorHintImage) doorHintImage.enabled = false;

        // Diyalog
        if (isNearDialog)
        {
            if (dialogHintImage) dialogHintImage.enabled = true;
            if (!dialogTriggered && Input.GetKeyDown(KeyCode.E))
            {
                dialogTriggered = true;
                DialogTypewriter.RequestStart();
                return;
            }
        }
        else if (dialogHintImage) dialogHintImage.enabled = false;

        // Flip
        Vector3 mousePos = Input.mousePosition;
        Vector3 charScreenPos = Camera.main.WorldToScreenPoint(transform.position);
        if (mousePos.x >= charScreenPos.x) { if (!facingRight) Flip(true); }
        else { if (facingRight) Flip(false); }

        if (animator != null)
            animator.SetFloat("Speed", moveInput.magnitude);
    }

    void FixedUpdate()
    {
        if (isDead) return;
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    void Flip(bool lookRight)
    {
        facingRight = lookRight;
        Vector3 scale = transform.localScale;
        scale.x = lookRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    // === HASAR ===
    public void DamageAt(int amount, Vector3 hitPos)
    {
        if (isDead) return;

        CameraShakerHandler.Shake(shakeData);
        OnTakeDamage?.Invoke(amount);

        PlayHitSound();
        PlayHitFX(hitPos);
        FlashColor();
    }

    private void PlayHitFX(Vector3 pos)
    {
        Transform parent = (attachFxToSprite && spriteRenderer != null) ? spriteRenderer.transform : null;

        if (hitFxInstance != null)
        {
            var t = hitFxInstance.transform;
            if (parent != null) t.SetParent(parent, worldPositionStays: false);
            t.position = pos;
            hitFxInstance.Play(true);
        }
        else if (hitFxPrefab != null)
        {
            var fx = Instantiate(hitFxPrefab, pos, Quaternion.identity, parent);
            fx.Play(true);
            Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax + 0.1f);
        }
    }

    // 🔥 Ölüm FX'i BAĞIMSIZ oluştur (player yok olsa da çalışsın)
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

    private void PlayDeathFX(Vector3 pos)
    {
        Transform parent = (attachFxToSprite && spriteRenderer != null) ? spriteRenderer.transform : null;

        if (deathFxInstance != null)
        {
            var t = deathFxInstance.transform;
            if (parent != null) t.SetParent(parent, worldPositionStays: false);
            t.position = pos;
            deathFxInstance.Play(true);
        }
        else if (deathFxPrefab != null)
        {
            var fx = Instantiate(deathFxPrefab, pos, Quaternion.identity, parent);
            fx.Play(true);
            Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax + 0.1f);
        }
    }

    private void FlashColor()
    {
        if (spriteRenderer == null || isFlashing || isDead) return;
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        isFlashing = true;
        var prev = spriteRenderer.color;
        spriteRenderer.color = hitColor;
        yield return new WaitForSeconds(hitFlashDuration);
        if (!isDead) spriteRenderer.color = prev;
        isFlashing = false;
    }

    private void HandlePlayerDied()
    {
        if (isDead) return;
        isDead = true;

        // Ses + bağımsız death FX
        PlayDeathSound();
        SpawnDeathFX(transform.position);

        if (spriteRenderer != null)
            spriteRenderer.color = deathColor;
        if (animator) animator.SetFloat("Speed", 0f);

        OnRequestFadeOut?.Invoke();

        Destroy(gameObject);
    }

    // 🔊 Ses
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

    // ====== TRIGGER ======
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Door"))
        {
            nearbyDoor = other.GetComponent<Door>();
            isNearDoor = true;
        }

        if (other.CompareTag("DeathAreaPlayer"))
        {
            Vector3 hitPos = other.ClosestPoint(transform.position);
            DamageAt(1, hitPos);

            // ——— Mermiyi yok et ———
            // Sadece mermileri silmek istersen:
            // if (other.GetComponent<Bullet>() != null) Destroy(other.gameObject);
            // Tag'e güveniyorsan direkt:
            Destroy(other.gameObject);
        }

        if (other.CompareTag("Diyalog"))
            isNearDialog = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Door"))
        {
            nearbyDoor = null;
            isNearDoor = false;
            if (doorHintImage) doorHintImage.enabled = false;
        }

        if (other.CompareTag("Diyalog"))
        {
            isNearDialog = false;
            dialogTriggered = false;
            if (dialogHintImage) dialogHintImage.enabled = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("DeathAreaPlayer"))
        {
            Vector3 hitPos = collision.GetContact(0).point;
            DamageAt(1, hitPos);

            // ——— Mermiyi yok et ———
            // if (collision.collider.GetComponent<Bullet>() != null) Destroy(collision.collider.gameObject);
            //Destroy(collision.collider.gameObject);
        }
    }

    public static void RequestFadeOut()
    {
        OnRequestFadeOut?.Invoke();
    }
}
