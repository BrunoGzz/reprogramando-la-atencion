using UnityEngine;

public class dotVisibility : MonoBehaviour
{
    private MindFlexReader reader;
    private Renderer objectRenderer;

    void Start()
    {
        reader = MindFlexReader.Instance;
        objectRenderer = GetComponent<Renderer>();  // Get the Renderer component
    }

    void Update()
    {
        if (reader == null || objectRenderer == null) return;

        objectRenderer.enabled = reader.DotVisibility != 0;
    }
}
