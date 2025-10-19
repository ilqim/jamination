using UnityEngine;
using System;
using System.Collections.Generic;

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
    [SerializeField] private float fireCooldown = 1.0f;
    [Tooltip("Basılı tutarken de (GetMouseButton) cooldown doldukça ateş etsin mi?")]
    [SerializeField] private bool autoFire = false;
    private float nextFireTime = 0f;

    [Header("Çok Mermili (Multi Shot)")]
    [SerializeField] private bool multiShot = false;
    [Tooltip("Child'lardaki BulletSpawn noktalarını sırayla (alttan üste) ekle")]
    [SerializeField] private List<Transform> bulletSpawns = new();
    [Tooltip("Her spawn için mouse yönüne göre derece cinsinden offset. (1. spawn için 0=mouse yönü)")]
    [SerializeField] private List<float> angleOffsetsDeg = new();

    [Header("Muzzle Flash (Particle)")]
    public ParticleSystem muzzleFlashInstance;
    public ParticleSystem muzzleFlashPrefab;

    [Header("Ateş Sesi (Audio)")]
    [Tooltip("Ateş sesi çalacak AudioSource (örnek: silahın üstünde).")]
    [SerializeField] private AudioSource fireAudioSource;
    [Tooltip("Çalınacak AudioClip (tek atış sesi).")]
    [SerializeField] private AudioClip fireClip;
    [Range(0f, 1f)] [SerializeField] private float fireVolume = 1f;

    [Header("Debug")]
    public bool showDebug = false;

    public event EventHandler<OnShootEventArgs> OnShoot;
    public class OnShootEventArgs : EventArgs
    {
        public Vector3 gunEndPointPosition;
        public Vector3 shootPosition;
        public Vector3 shellPosition;
    }

    private void Awake()
    {
        if (parentToRotate == null) parentToRotate = transform.parent;
        if (firePoint == null && transform.childCount > 0) firePoint = transform.GetChild(0);
        if (shellEjectPoint == null) shellEjectPoint = firePoint;

        if (fireAudioSource != null)
        {
            fireAudioSource.playOnAwake = false;
            fireAudioSource.loop = false;
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
        Vector3 aimDirection = (mousePosition - parentToRotate.position).normalized;

        float angleSigned = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        parentToRotate.eulerAngles = new Vector3(0f, 0f, angleSigned);

        // Flip scale
        Vector3 s = parentToRotate.localScale;
        if (angleSigned > 90f || angleSigned < -90f)
        {
            s.x = -Mathf.Abs(s.x);
            s.y = -Mathf.Abs(s.y);
        }
        else
        {
            s.x = Mathf.Abs(s.x);
            s.y = Mathf.Abs(s.y);
        }
        parentToRotate.localScale = s;

        if (showDebug) Debug.Log($"angleSigned: {angleSigned:F1}° | scaleX:{s.x} | scaleY:{s.y}");
    }

    // ---------- Shoot ----------
    private void HandleShooting()
    {
        bool wantsToShoot = autoFire ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
        if (!wantsToShoot) return;
        if (Time.time < nextFireTime) return;

        FireOnce();
        nextFireTime = Time.time + fireCooldown;
    }

    private void FireOnce()
    {
        if (bulletPrefab == null) return;

        if (multiShot && bulletSpawns.Count > 0)
        {
            while (angleOffsetsDeg.Count < bulletSpawns.Count)
                angleOffsetsDeg.Add(0f);

            Vector3 mousePos = GetMouseWorldPosition();

            for (int i = 0; i < bulletSpawns.Count; i++)
            {
                Transform spawn = bulletSpawns[i];
                if (spawn == null) continue;

                Vector2 baseDir = (mousePos - spawn.position).normalized;
                float deg = angleOffsetsDeg[i];
                Vector2 dir = RotateVector2(baseDir, deg);
                SpawnBullet(spawn.position, dir);
            }
        }
        else
        {
            if (firePoint == null) return;
            Vector3 mousePos = GetMouseWorldPosition();
            Vector2 dir = (mousePos - firePoint.position).normalized;
            SpawnBullet(firePoint.position, dir);
        }

        PlayMuzzleFlash();
        PlayFireSound(); // 🔊 Ateş sesi burada

        Vector3 mousePositionForEvent = GetMouseWorldPosition();
        OnShoot?.Invoke(this, new OnShootEventArgs
        {
            gunEndPointPosition = firePoint ? firePoint.position : transform.position,
            shootPosition = mousePositionForEvent,
            shellPosition = shellEjectPoint ? shellEjectPoint.position : (firePoint ? firePoint.position : transform.position),
        });
    }

    private void SpawnBullet(Vector3 pos, Vector2 dir)
    {
        GameObject bullet = Instantiate(bulletPrefab, pos, Quaternion.identity);
        bullet.transform.right = dir;

        var rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER || UNITY_2022_2_OR_NEWER
            rb.linearVelocity = dir * bulletSpeed;
#else
            rb.velocity = dir * bulletSpeed;
#endif
            rb.angularVelocity = 0f;
        }

        var bulletCol = bullet.GetComponent<Collider2D>();
        var ownerCol = parentToRotate ? parentToRotate.GetComponentInParent<Collider2D>() : null;
        if (bulletCol && ownerCol) Physics2D.IgnoreCollision(bulletCol, ownerCol, true);

        Destroy(bullet, 2f);
    }

    private static Vector2 RotateVector2(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cs = Mathf.Cos(rad), sn = Mathf.Sin(rad);
        return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
    }

    private void PlayMuzzleFlash()
    {
        if (firePoint == null) return;

        if (muzzleFlashInstance != null)
        {
            muzzleFlashInstance.Play(true);
        }
        else if (muzzleFlashPrefab != null)
        {
            var fx = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            fx.Play(true);
            Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax + 0.1f);
        }
    }

    // ---------- 🔊 Ateş sesi ----------
    private void PlayFireSound()
    {
        if (fireAudioSource == null || fireClip == null)
            return;

        fireAudioSource.PlayOneShot(fireClip, fireVolume);
    }

    // ---------- Utils ----------
    public static Vector3 GetMouseWorldPosition()
    {
        Vector3 vec = GetMouseWorldPositionWithZ(Input.mousePosition, Camera.main);
        vec.z = 0f;
        return vec;
    }

    public static Vector3 GetMouseWorldPositionWithZ(Vector3 screenPosition, Camera worldCamera)
    {
        return worldCamera.ScreenToWorldPoint(screenPosition);
    }
}
