using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Takip Edilecek Nesne")]
    public Transform target;     // Player Transform'unu buraya ata

    [Header("Offset (kamera kaydırması)")]
    public Vector2 offset = Vector2.zero; // Örn: (0, 1) yukarıdan baksın

    private void LateUpdate()
    {
        if (target == null) return;

        // Anında pozisyonu eşitle (Z ekseni korunur)
        Vector3 newPos = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z
        );

        transform.position = newPos;
    }

    // 🔹 Player ölürken çağrılabilecek yardımcılar:
    public void DetachNow(bool keepPosition = true)
    {
        if (keepPosition)
            transform.SetParent(null, true); // Parent’tan ayır, pozisyonu koru
        target = null;
    }

    public void FollowTempAnchorAt(Vector3 pos)
    {
        // Ölüm anında sabit bir noktada kalması için geçici "ankor" objesi oluşturur
        GameObject anchor = new GameObject("CameraAnchor_Temp");
        anchor.transform.position = pos;
        transform.SetParent(null, true);
        target = anchor.transform;
    }
}
