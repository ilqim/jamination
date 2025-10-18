using UnityEngine;
using System;

public class Gun : MonoBehaviour
{
    [Header("Referanslar")]
    public Transform parentToRotate;
    public Transform firePoint;
    public Transform shellEjectPoint;
    public GameObject bulletPrefab;

    [Header("Ateş")]
    public float bulletSpeed = 12f;

    [Tooltip("İki atış arasındaki bekleme (saniye). Örn: 1.0 = saniyede 1 mermi")]
    [SerializeField] private float fireCooldown = 1.0f; // <<< EKLENDİ
    [Tooltip("Basılı tutarken de (GetMouseButton) cooldown doldukça ateş etsin mi?")]
    [SerializeField] private bool autoFire = false;      // <<< EKLENDİ
    private float nextFireTime = 0f;                     // <<< EKLENDİ

    [Header("Muzzle Flash (Particle)")]
    public ParticleSystem muzzleFlashInstance;
    public ParticleSystem muzzleFlashPrefab;

    [Header("Debug")]
    public bool showDebug = false;

    public event EventHandler<OnShootEventArgs> OnShoot;
    public class OnShootEventArgs : EventArgs {
        public Vector3 gunEndPointPosition;
        public Vector3 shootPosition;
        public Vector3 shellPosition;
    }

    private void Awake()
    {
        if (parentToRotate == null) parentToRotate = transform.parent;
        if (firePoint == null && transform.childCount > 0) firePoint = transform.GetChild(0);
        if (shellEjectPoint == null) shellEjectPoint = firePoint;

        if (muzzleFlashInstance != null && firePoint != null)
        {
            // muzzleFlashInstance.transform.position = firePoint.position;
            muzzleFlashInstance.transform.rotation = firePoint.rotation;
        }
    }

    private void Update()
    {
        HandleAiming();
        HandleShooting();
    }

    // ---------- Aim ----------
    private void HandleAiming()
    {
        if (parentToRotate == null) return;

        Vector3 mousePosition = GetMouseWorldPosition();
        Vector3 aimDirection  = (mousePosition - parentToRotate.position).normalized;

        float angleSigned = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        parentToRotate.eulerAngles = new Vector3(0f, 0f, angleSigned);

        Vector3 s = parentToRotate.localScale;
        if (angleSigned > 90f || angleSigned < -90f) { s.x = -Mathf.Abs(s.x); s.y = -Mathf.Abs(s.y); }
        else { s.x =  Mathf.Abs(s.x); s.y =  Mathf.Abs(s.y); }
        parentToRotate.localScale = s;

        // if (muzzleFlashInstance != null && firePoint != null)
        // {
        //     muzzleFlashInstance.transform.SetPositionAndRotation(firePoint.position, firePoint.rotation);
        // }

        if (showDebug) Debug.Log($"angleSigned: {angleSigned:F1}° | scaleX:{s.x} | scaleY:{s.y}");
    }

    // ---------- Shoot ----------
    private void HandleShooting()
    {
        // Tek tek tıklama mı (GetMouseButtonDown) yoksa basılı tutma mı (GetMouseButton)?
        bool wantsToShoot = autoFire ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);

        // Cooldown kontrolü
        if (!wantsToShoot) return;
        if (Time.time < nextFireTime) return; // <<< COOLDOWN

        // Atış
        FireOnce();

        // Bir sonraki izinli atış zamanı
        nextFireTime = Time.time + fireCooldown; // <<< COOLDOWN
    }

    private void FireOnce()
    {
        if (bulletPrefab != null && firePoint != null)
        {
            Vector3 mousePos = GetMouseWorldPosition();
            Vector2 dir = (mousePos - firePoint.position).normalized;

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            bullet.transform.SetParent(null);

            var rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
#if UNITY_6000_0_OR_NEWER || UNITY_2022_2_OR_NEWER
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.gravityScale = 0f;
                rb.linearDamping = 0f;
                rb.linearVelocity = dir * bulletSpeed;
#else
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.gravityScale = 0f;
                rb.drag = 0f;
                rb.velocity = dir * bulletSpeed;
#endif
            }

            var bulletCol = bullet.GetComponent<Collider2D>();
            var playerCol = parentToRotate ? parentToRotate.GetComponentInParent<Collider2D>() : null;
            if (bulletCol != null && playerCol != null)
                Physics2D.IgnoreCollision(bulletCol, playerCol, true);

            Destroy(bullet, 2f);
        }

        // Muzzle flash ve event
        PlayMuzzleFlash();

        Vector3 mousePositionForEvent = GetMouseWorldPosition();
        OnShoot?.Invoke(this, new OnShootEventArgs {
            gunEndPointPosition = firePoint ? firePoint.position : transform.position,
            shootPosition       = mousePositionForEvent,
            shellPosition       = shellEjectPoint ? shellEjectPoint.position : (firePoint ? firePoint.position : transform.position),
        });
    }

    private void PlayMuzzleFlash()
    {
        if (firePoint == null) return;

        if (muzzleFlashInstance != null)
        {
            // muzzleFlashInstance.transform.SetPositionAndRotation(firePoint.position, firePoint.rotation);
            muzzleFlashInstance.Play(true);
        }
        else if (muzzleFlashPrefab != null)
        {
            var fx = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            fx.Play(true);
            Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax + 0.1f);
        }
    }

    // ---------- Utils ----------
    public static Vector3 GetMouseWorldPosition() {
        Vector3 vec = GetMouseWorldPositionWithZ(Input.mousePosition, Camera.main);
        vec.z = 0f; return vec;
    }
    public static Vector3 GetMouseWorldPositionWithZ() {
        return GetMouseWorldPositionWithZ(Input.mousePosition, Camera.main);
    }
    public static Vector3 GetMouseWorldPositionWithZ(Camera worldCamera) {
        return GetMouseWorldPositionWithZ(Input.mousePosition, worldCamera);
    }
    public static Vector3 GetMouseWorldPositionWithZ(Vector3 screenPosition, Camera worldCamera) {
        return worldCamera.ScreenToWorldPoint(screenPosition);
    }
}
