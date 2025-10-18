using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CameraUnstable : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    [Header("Açı Aralığı (derece)")]
    [SerializeField] private float angleMin = -5f;
    [SerializeField] private float angleMax =  5f;
    [SerializeField] private Axis rotateAxis = Axis.Z; // 2D için Z

    [Header("Bekleme Süresi Aralığı (sn)")]
    [SerializeField] private float waitMin = 2f;
    [SerializeField] private float waitMax = 5f;

    [Header("Dönüş Süresi (sn)")]
    [SerializeField] private float rotateDuration = 1f;

    [Header("Otomatik Tekrarlama")]
    [SerializeField] private bool autoRepeat = true;
    [SerializeField] private float pauseBetweenCycles = 0f;

    [Header("İlk Başlangıç Beklemesi")]
    [Tooltip("Loop ilk kez başlamadan beklesin mi?")]
    [SerializeField] private bool waitAtStart = true;

    [Tooltip(">= 0 ise sabit ilk bekleme süresi. < 0 ise waitMin–waitMax aralığından rastgele seçilir.")]
    [SerializeField] private float startDelay = -1f;

    private Quaternion _baseRot;
    private Coroutine _routine;

    void OnEnable()
    {
        _baseRot = transform.rotation;
        if (autoRepeat) _routine = StartCoroutine(LoopRoutine());
    }

    void OnDisable()
    {
        if (_routine != null) StopCoroutine(_routine);
        transform.rotation = _baseRot;
    }

    /// Tek seferlik çalıştırma (ilk bekleme uygulanmaz).
    public void TriggerOnce()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(OneCycleRoutine());
    }

    private IEnumerator LoopRoutine()
    {
        // --- İLK BAŞLANGIÇ BEKLEMESİ ---
        if (waitAtStart)
        {
            float delay = (startDelay >= 0f) ? startDelay : Random.Range(waitMin, waitMax);
            if (delay > 0f) yield return new WaitForSeconds(delay);
        }

        while (true)
        {
            yield return OneCycleRoutine();

            if (pauseBetweenCycles > 0f)
                yield return new WaitForSeconds(pauseBetweenCycles);
        }
    }

    private IEnumerator OneCycleRoutine()
    {
        // 1) Rastgele süre ve açı seç
        float holdTime = Random.Range(waitMin, waitMax);
        float angle    = Random.Range(angleMin, angleMax);

        // 2) Hedef rotasyon (baseRot + axis’e göre açı)
        Quaternion targetRot = _baseRot * Quaternion.AngleAxis(angle, AxisVector(rotateAxis));

        // 3) Base -> Target
        yield return RotateOverTime(transform.rotation, targetRot, rotateDuration);

        // 4) Rastgele süre bekle
        if (holdTime > 0f) yield return new WaitForSeconds(holdTime);

        // 5) Target -> Base
        yield return RotateOverTime(transform.rotation, _baseRot, rotateDuration);
    }

    private IEnumerator RotateOverTime(Quaternion from, Quaternion to, float duration)
    {
        duration = Mathf.Max(0.0001f, duration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration); // yumuşak geçiş
            transform.rotation = Quaternion.Slerp(from, to, k);
            yield return null;
        }
        transform.rotation = to;
    }

    private static Vector3 AxisVector(Axis axis)
    {
        switch (axis)
        {
            case Axis.X: return Vector3.right;
            case Axis.Y: return Vector3.up;
            default:     return Vector3.forward;
        }
    }

    public void SetBaseToCurrent() => _baseRot = transform.rotation;
    public void SetAngleRange(float minDeg, float maxDeg) { angleMin = minDeg; angleMax = maxDeg; }
    public void SetWaitRange(float minSec, float maxSec) { waitMin = minSec; waitMax = maxSec; }
    public void SetRotateDuration(float seconds) { rotateDuration = Mathf.Max(0f, seconds); }

#if UNITY_EDITOR
    // Inspector’da değerleri güvene almak için
    void OnValidate()
    {
        if (angleMax < angleMin) (angleMin, angleMax) = (angleMax, angleMin);
        if (waitMax  < waitMin)  (waitMin,  waitMax)  = (waitMax,  waitMin);
    }
#endif
}
