using UnityEngine;

public class GunChange : MonoBehaviour
{
    [Header("Hedef (sahnede görünen)")]
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("Yeni Silah Sprite'ı (Asset olarak)")]
    [SerializeField] private Sprite newGunSprite; // 👈 Artık doğrudan sprite veriyorsun

    private void OnEnable()
    {
        DialogTypewriter.OnDialogFinished += ApplyGunSprite;
    }

    private void OnDisable()
    {
        DialogTypewriter.OnDialogFinished -= ApplyGunSprite;
    }

    private void ApplyGunSprite()
    {
        if (targetRenderer == null || newGunSprite == null)
        {
            Debug.LogWarning("[GunChange] Sprite veya hedef renderer atanmadı!");
            return;
        }

        targetRenderer.sprite = newGunSprite;
        Debug.Log("[GunChange] Hedef silah sprite'ı değiştirildi!");
    }
}
