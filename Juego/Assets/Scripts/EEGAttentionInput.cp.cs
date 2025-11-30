using UnityEngine;
using System.Collections.Generic;

public class EEGAttentionInput : MonoBehaviour
{
    private MindFlexReader reader;
    [Range(0, 1)]
    public float smoothedAttention = 0;

    public int smoothingWindow = 20;
    private Queue<int> attentionHistory = new Queue<int>();

    void Start(){
        reader = MindFlexReader.Instance;
    }

    void Update()
    {
        if (reader == null) return;

        int currentAttention = reader.Attention;

        attentionHistory.Enqueue(currentAttention);
        if (attentionHistory.Count > smoothingWindow)
            attentionHistory.Dequeue();

        float average = 0;
        foreach (var value in attentionHistory)
            average += value;
        average /= attentionHistory.Count;

        smoothedAttention = Mathf.Clamp01(average / 100f);  // Valor normalizado de 0 a 1.
    }
}
