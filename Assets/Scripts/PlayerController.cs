using System;
using UnityEngine;
using FirstGearGames.SmoothCameraShaker;

public class PlayerController : MonoBehaviour
{
    // >>> Hasar olayı (zaten vardı)
    public static event Action<int> OnTakeDamage;
    // >>> Fade isteği (ölümden sonra)
    public static event Action OnRequestFadeOut;

    [Header("Hareket Ayarları")]
    public float moveSpeed = 10f;

    [Header("Hasar / Ölüm Görseli")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color hitColor = Color.red;      // hasar flash rengi
    [SerializeField] private float hitFlashDuration = 0.2f;   // kısa flash süresi
    [SerializeField] private Color deathColor = new Color(1f, 0.2f, 0.2f); // ölümde kalıcı renk

    public ShakeData shakeData;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private bool facingRight = true;
    private Color originalColor;
    private bool isFlashing = false;
    private bool isDead = false;

    private void OnEnable()
    {
        PlayerHealth.OnPlayerDied += HandlePlayerDied; // >>> Ölümü dinle
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
    }

    void Update()
    {
        if (isDead) return; // öldüyse input alma

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput.Normalize();

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

    private void Damage(int amount = 1)
    {
        CameraShakerHandler.Shake(shakeData);
        OnTakeDamage?.Invoke(amount);
        FlashColor();
    }

    private void FlashColor()
    {
        if (spriteRenderer == null || isFlashing || isDead) return;
        StartCoroutine(FlashRoutine());
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        isFlashing = true;
        var prev = spriteRenderer.color;
        spriteRenderer.color = hitColor;
        yield return new WaitForSeconds(hitFlashDuration);
        if (!isDead) spriteRenderer.color = prev; // ölmediyse geri dön
        isFlashing = false;
    }

    private void HandlePlayerDied()
    {
        isDead = true;
        if (spriteRenderer != null)
            spriteRenderer.color = deathColor; // kalıcı olarak kırmızı ton

        // İstersen animasyon/rigidbody kapat:
        if (animator) animator.SetFloat("Speed", 0f);

        // Fade isteğini yayınla
        OnRequestFadeOut?.Invoke();
    }

    // 2D çarpışma
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.collider.CompareTag("DeathAreaPlayer"))
        {
            Damage(1);
        }
    }

    // Trigger kullanıyorsan bunu aç:
    /*
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("DeathAreaPlayer"))
            Damage(1);
    }
    */
}
