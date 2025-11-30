using UnityEngine;

public class FocusNeedleController : MonoBehaviour
{
    public EEGAttentionInput eegAttentionInput;

    public float minAngle = -90f; // Ángulo mínimo (cuando atención = 0)
    public float maxAngle = 90f;  // Ángulo máximo (cuando atención = 1)

    public float rotationSpeed = 1;

    void Start()
    {
    }

    void Update()
    {
        float attention = Mathf.Clamp01(eegAttentionInput.smoothedAttention / 100f);

        // Calcular ángulo destino en función de la atención
        float targetAngle = Mathf.Lerp(minAngle, maxAngle, attention);

        // Rotar suavemente hacia el ángulo destino
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }
}
