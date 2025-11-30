using UnityEngine;

public class DriveCar : MonoBehaviour
{
    private MindFlexReader reader;

    public Rigidbody2D backTire;
    public Rigidbody2D frontTire;
    public float baseSpeed = 20f;
    public EEGAttentionInput attentionInput; // Reference to EEGAttentionInput
    public double minAtt = 0.4;
    
    private float currentSpeed = 0f;
    public float maxSpeed = 50f;           // Velocidad máxima que puede alcanzar
    public float accelerationRate = 30f;   // Qué tan rápido acelera cuando hay concentración
    public float decelerationRate = 60f;   // Qué tan rápido frena cuando hay poca concentración
    public float brakeRate = 10f;

    [Header("Speed Needle Settings")]
    public Transform speedNeedle; // Assign the needle object here
    public float minNeedleAngle = -90f; // Angle at 0 speed
    public float maxNeedleAngle = 90f;  // Angle at max speed

    void Start(){
        reader = MindFlexReader.Instance;
    }

    void Update()
    {
        if (reader == null) return;

        maxSpeed = reader.MaxSpeed;
        minAtt = reader.MinAtt;
        accelerationRate = reader.AccRate;
        decelerationRate = reader.DecRate;
        brakeRate = reader.BrakeRate;

    }

    void FixedUpdate()
    {
        if (attentionInput == null) return;

        float attention = Mathf.Clamp01(attentionInput.smoothedAttention); // 0-1

        if (attention >= minAtt)
        {
            // Aceleración proporcional a la atención
            float targetSpeed = baseSpeed * attention;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelerationRate * Time.fixedDeltaTime);

            // Limitamos la velocidad máxima
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
        }
        else
        {
            // Frenado brusco si atención baja
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, decelerationRate * Time.fixedDeltaTime);
        }

        backTire.AddTorque(-currentSpeed * Time.fixedDeltaTime);
        frontTire.AddTorque(-currentSpeed * Time.fixedDeltaTime);

        UpdateSpeedNeedle();
    }

    void UpdateSpeedNeedle()
    {
        if (speedNeedle == null) return;

        // Normaliza la velocidad actual entre 0 y maxSpeed
        float t = Mathf.InverseLerp(0f, maxSpeed, currentSpeed);

        // Interpola entre los ángulos mínimo y máximo
        float angle = Mathf.Lerp(minNeedleAngle, maxNeedleAngle, t);

        // Aplica la rotación (solo en Z)
        speedNeedle.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
