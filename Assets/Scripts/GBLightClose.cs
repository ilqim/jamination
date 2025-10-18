using UnityEngine;
using UnityEngine.Rendering.Universal; // Light2D

public class GBLightClose : MonoBehaviour
{
    [SerializeField] private Light2D globalLight;
    [SerializeField] private float targetIntensity = 0.13f;

    private void OnEnable()
    {
        DialogTypewriter.OnDialogFinished += DimLight;
    }

    private void OnDisable()
    {
        DialogTypewriter.OnDialogFinished -= DimLight;
    }

    private void DimLight()
    {
        if (globalLight == null)
        {
            Debug.LogWarning("[GBLightClose] Global Light2D atanmadı.");
            return;
        }

        globalLight.intensity = targetIntensity;
        Debug.Log($"[GBLightClose] Global light intensity => {targetIntensity}");
    }
}
