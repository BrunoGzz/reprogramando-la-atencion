using UnityEngine;

public class HaloPulse : MonoBehaviour
{
    public float pulseDuration = 1f;       // Tiempo total de cada pulso
    public float maxScale = 3f;            // Escala máxima del halo
    public float maxAlpha = 0.4f;          // Opacidad máxima

    private SpriteRenderer sr;
    private float timer;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.enabled = true;
        sr.color = new Color(1, 1, 1, 0);
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > pulseDuration)
        {
            timer = 0f; // Reiniciar cada segundo
        }

        // t va de 0 a 1 en cada ciclo
        float t = timer / pulseDuration;

        // Escalar suavemente desde 0 hasta maxScale
        float scale = Mathf.Lerp(0f, maxScale, t);
        transform.localScale = new Vector3(scale, scale, 1f);

        // Disminuir opacidad al final del ciclo
        float alpha = Mathf.Lerp(maxAlpha, 0f, t);
        sr.color = new Color(1, 1, 1, alpha);
    }
}
