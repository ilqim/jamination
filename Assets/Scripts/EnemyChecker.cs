using UnityEngine;

public class EnemyChecker : MonoBehaviour
{
    [Header("Kapı Adı (Door objesinin ismi ya da sahne adı)")]
    [SerializeField] private string targetDoorName = "NextSceneDoor";

    [Header("Kontrol Aralığı (saniye)")]
    [SerializeField] private float checkInterval = 1f;

    [Header("Kontrol Edilecek Layer")]
    [SerializeField] private LayerMask enemyLayer; // 👈 Artık tag değil layer

    private float timer = 0f;
    private bool doorUnlocked = false;

    private void Update()
    {
        if (doorUnlocked) return;

        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            CheckEnemies();
        }
    }

    private void CheckEnemies()
    {
        // Sahnedeki tüm objeleri al
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        bool anyEnemyLeft = false;

        foreach (var obj in allObjects)
        {
            // Objeler aktif mi ve belirtilen layer’da mı?
            if (obj.activeInHierarchy && ((1 << obj.layer) & enemyLayer) != 0)
            {
                anyEnemyLeft = true;
                break;
            }
        }

        // Eğer hiç enemy layer'ında obje yoksa
        if (!anyEnemyLeft)
        {
            Debug.Log("[EnemyChecker] Belirtilen layer'da hiç düşman kalmadı, kapı açılabilir!");
            Door.UnlockDoor(targetDoorName);
            doorUnlocked = true;
        }
    }
}
