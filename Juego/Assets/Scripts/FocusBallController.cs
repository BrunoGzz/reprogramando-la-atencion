using UnityEngine;

public class FocusBallController2D : MonoBehaviour
{
    public EEGAttentionInput eegAttentionInput;
    [Range(0f, 1f)] public float attentionThreshold = 0.85f;

    public Vector2 startPosition = new Vector2(4f, -4f);
    public Vector2 centerPosition = Vector2.zero;

    public float moveSpeed = 2f;

    public int attentionThresholValue = 3;

    public GameObject haloCircle;

    private bool haloActive = false;

    private int attentionAboveThresholdCount = 0;
    private bool actionTriggered = false;

    private float attentionAboveThresholdSeconds = 0f;

    void Start()
    {
        transform.position = startPosition;
        haloCircle.SetActive(false);
    }

    void Update()
    {
        float attention = eegAttentionInput.smoothedAttention;

        // Movimiento interpolado entre esquina y centro
        float t = Mathf.Clamp01(attention);
        Vector3 targetPosition = Vector3.Lerp(startPosition, centerPosition, t);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);

        // Activar el halo cuando la atención está en el threshold
        bool isAboveThreshold = attention >= attentionThreshold;

        if (isAboveThreshold)
        {
            haloActive = true;
            haloCircle.SetActive(true);

            // Incrementar el contador solo al cruzar el umbral hacia arriba
            attentionAboveThresholdCount++;
            attentionAboveThresholdSeconds += Time.deltaTime;

            actionTriggered = true;
        }
        else if (!isAboveThreshold && haloActive)
        {
            haloActive = false;
            haloCircle.SetActive(false);

            // Acción al dejar de mantener la atención después de superar 3 veces
            if (actionTriggered)
            {
                actionTriggered = false;
                DoPostAttentionDropAction();
                attentionAboveThresholdSeconds = 0f;
            }
        }
    }

    void DoPostAttentionDropAction()
    {
        Debug.Log("¡Atención cayó! Acción secundaria.");
        MindFlexReader.Instance.SendMessageToApp("ATTENTION_DROPPED_AFTER_THRESHOLD;"+attentionAboveThresholdSeconds);
        MindFlexReader.Instance.SendMessageToApp("END_ACTION");
    }
}
