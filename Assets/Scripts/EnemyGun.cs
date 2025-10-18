using UnityEngine;

[DisallowMultipleComponent]
public class EnemyGun : MonoBehaviour
{
    [Header("Bağlantılar")]
    [SerializeField] private Enemy owner;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;

    [Header("Ateş Ayarları")]
    [SerializeField] private float bulletSpeed = 12f;
    [SerializeField] private float fireInterval = 1f;
    [SerializeField] private float startDelay = 0f;
    [SerializeField] private bool rotateToTarget = true;
    [SerializeField] private float destroyTime = 3f;

    private float nextFireTime;

    private void Awake()
    {
        if (owner == null) owner = GetComponentInParent<Enemy>();
        if (firePoint == null) firePoint = transform;
    }

    private void OnEnable()
    {
        nextFireTime = Time.time + startDelay;
    }

    private void Update()
    {
        // Bu component, Enemy tarafından enable/disable edilir.
        if (owner == null || owner.target == null) return;
        if (Time.time < nextFireTime) return;

        // Yön
        Vector2 dir = (owner.target.position - firePoint.position).normalized;

        if (rotateToTarget)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            firePoint.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        FireBullet(dir);
        nextFireTime = Time.time + fireInterval;
    }

    private void FireBullet(Vector2 dir)
    {
        if (bulletPrefab == null) return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        bullet.transform.right = dir;

        var rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER || UNITY_2022_2_OR_NEWER
            rb.linearVelocity = dir * bulletSpeed;
#else
            rb.velocity = dir * bulletSpeed;
#endif
        }

        var enemyCols = owner ? owner.GetComponentsInChildren<Collider2D>() : null;
        var bulletCol = bullet.GetComponent<Collider2D>();
        if (enemyCols != null && bulletCol != null)
        {
            foreach (var col in enemyCols)
                Physics2D.IgnoreCollision(col, bulletCol, true);
        }

        Destroy(bullet, destroyTime);
    }
}
