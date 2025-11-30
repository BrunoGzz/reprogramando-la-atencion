using UnityEngine;
using UnityEngine.UI;

public class SignalIndicator : MonoBehaviour
{
    public MindFlexReader reader;
    public Image indicatorImage;

    void Update()
    {
        /*if (reader == null || indicatorImage == null) return;

        int signal = reader.PoorSignal;

        if (signal == 200)
        {
            indicatorImage.color = Color.red;  // Desconectado
        }
        else if (signal >= 20 && signal < 200)
        {
            indicatorImage.color = new Color(1f, 0.65f, 0f);  // Naranja
        }
        else if (signal < 20)
        {
            indicatorImage.color = Color.green;  // Buena señal
        }*/
    }
}
