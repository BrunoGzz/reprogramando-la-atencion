using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CrosshairController : MonoBehaviour
{
    public MindFlexReader reader;
    public RectTransform crosshairOuter;
    public float minRadius = 30f;
    public float maxRadius = 120f;
    public float smoothSpeed = 2.0f;
    public int smoothingWindow = 20;

    private Queue<int> attentionHistory = new Queue<int>();

    void Update()
    {
        if (reader == null || crosshairOuter == null) return;

        int currentAttention = reader.Attention;

        // Guardar valores para suavizar
        attentionHistory.Enqueue(currentAttention);
        if (attentionHistory.Count > smoothingWindow)
            attentionHistory.Dequeue();

        float averageAttention = 0;
        foreach (var value in attentionHistory)
            averageAttention += value;
        averageAttention /= attentionHistory.Count;

        // Ajustar tamaño de crosshair
        float targetRadius = Mathf.Lerp(maxRadius, minRadius, averageAttention / 100f);
        Vector2 currentSize = crosshairOuter.sizeDelta;
        Vector2 targetSize = new Vector2(targetRadius, targetRadius);
        crosshairOuter.sizeDelta = Vector2.Lerp(currentSize, targetSize, Time.deltaTime * smoothSpeed);
    }
}
