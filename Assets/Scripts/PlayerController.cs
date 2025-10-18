using System;
using UnityEngine;
using UnityEngine.UI; // Image
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
    [SerializeField] private Image dialogHintImage; // “E’ye basarak konuş”
    [SerializeField] private Image doorHintImage;   // “E’ye basarak gir”

    public ShakeData shakeData;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private bool facingRight = true;
    private Color originalColor;
    private bool isFlashing = false;
    private bool isDead = false;

    // --- Kapı değişkenleri (trigger) ---
    private Door nearbyDoor = null;
    private bool isNearDoor = false;

    // --- Diyalog değişkenleri (COLLIDER) ---
    private bool isNearDialog = false;   // diyalog collider’ı ile temas var mı
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
    }

    void Update()
    {
        if (isDead) return;

        // hareket
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput.Normalize();

        // === KAPI (trigger + E) ===
        if (isNearDoor && nearbyDoor != null)
        {
            if (doorHintImage) doorHintImage.enabled = true;
            if (Input.GetKeyDown(KeyCode.E))
            {
                nearbyDoor.TryOpen();
                return;
            }
        }
        else
        {
            if (doorHintImage) doorHintImage.enabled = false;
        }

        // === DİYALOG (COLLIDER + E) ===
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
        else
        {
            if (dialogHintImage) dialogHintImage.enabled = false;
        }

        // görünüm yönü
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
        if (!isDead) spriteRenderer.color = prev;
        isFlashing = false;
    }

    private void HandlePlayerDied()
    {
        isDead = true;
        if (spriteRenderer != null)
            spriteRenderer.color = deathColor;

        if (animator) animator.SetFloat("Speed", 0f);
        OnRequestFadeOut?.Invoke();
    }

    // ====== KAPI: TRIGGER ENTER/EXIT ======
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Door"))
        {
            nearbyDoor = other.GetComponent<Door>();
            isNearDoor = true;
        }

        if (other.CompareTag("DeathAreaPlayer"))
            Damage(1);

        if (other.CompareTag("Diyalog"))
        {
            isNearDialog = true;
            // not: dialogTriggered false kalır; E’ye basınca başlatılır
        }
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
            dialogTriggered = false; // tekrar temas edince yeniden E ile başlatılabilir
            if (dialogHintImage) dialogHintImage.enabled = false;
        }
    }

    // ====== DİYALOG: COLLISION ENTER/EXIT ======
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("DeathAreaPlayer"))
            Damage(1);

        // if (collision.collider.CompareTag("Diyalog"))
        // {
        //     isNearDialog = true;
        //     // not: dialogTriggered false kalır; E’ye basınca başlatılır
        // }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // if (collision.collider.CompareTag("Diyalog"))
        // {
        //     isNearDialog = false;
        //     dialogTriggered = false; // tekrar temas edince yeniden E ile başlatılabilir
        //     if (dialogHintImage) dialogHintImage.enabled = false;
        // }
    }

    public static void RequestFadeOut()
    {
        OnRequestFadeOut?.Invoke();
    }
}
