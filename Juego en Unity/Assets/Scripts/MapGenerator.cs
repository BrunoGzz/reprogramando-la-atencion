using UnityEngine;
using UnityEngine.U2D;

[ExecuteInEditMode]
public class MapGenerator : MonoBehaviour
{
    private MindFlexReader reader;

    [SerializeField] private SpriteShapeController _spriteShapeController;
    [SerializeField] private Transform _player;
    [SerializeField] private GameObject _flagPrefab;

    [SerializeField, Range(3, 100)] private int _levelLength = 50;
    [SerializeField, Range(1, 5)] private int _difficulty = 1;
    [SerializeField] private int _seedNumber = 1;

    [SerializeField, Range(0f, 1f)] private float _curveSmoothness = 0.5f;
    [SerializeField] private float _noiseStep = 0.5f;
    [SerializeField] private float _bottom = 10f;

    [Header("Spawn")]
    [SerializeField] private float _spawnYOffset = 3f;

    private Vector3 _lastPos;
    private GameObject _flagInstance;
    private Vector3 _flagPosition;
    private bool _sessionEnded = false;

    private void Start()
    {
        if (Application.isPlaying)
        {
            reader = MindFlexReader.Instance;
            _difficulty = reader.GameLevel;
            _seedNumber = reader.GameSeed;
        }


        GenerateSpline();
        CreateFlag();
        ResetPlayerToStart();
    }

    private void OnValidate()
    {
        // Solo actualizar la spline visual en editor, no instanciar prefabs
        if (_spriteShapeController != null)
        {
            GenerateSpline();
        }
    }

    private void Update()
    {
        int newDifficulty = reader.GameLevel;
        int newSeed = reader.GameSeed;
        int levelLength = reader.LevelLength;

        if (newDifficulty != _difficulty || newSeed != _seedNumber || levelLength != _levelLength)
        {
            _difficulty = newDifficulty;
            _seedNumber = newSeed;
            _levelLength = levelLength;

            GenerateSpline();
            CreateFlag();
            ResetPlayerToStart();
        }

        if (_sessionEnded) return;

        if (_player != null && _flagInstance != null)
        {
            if (_player.position.x >= _flagPosition.x)
            {
                EndSession();
            }
        }
    }

    private void GenerateSpline()
    {
        if (_spriteShapeController == null)
            return;

        Random.InitState(_seedNumber);
        _spriteShapeController.spline.Clear();

        float xMultiplier = Mathf.Lerp(4f, 1.2f, (_difficulty - 1) / 4f);
        float yMultiplier = Mathf.Lerp(2f, 12f, (_difficulty - 1) / 4f);
        float variationFreq = Mathf.Lerp(0.05f, 0.2f, (_difficulty - 1) / 4f);

        for (int i = 0; i < _levelLength; i++)
        {
            float variation = Mathf.PerlinNoise(100f, i * variationFreq);
            float localYMultiplier = Mathf.Lerp(yMultiplier * 0.3f, yMultiplier * 1.5f, variation);
            float y = Mathf.PerlinNoise(0, i * _noiseStep) * localYMultiplier;

            _lastPos = transform.position + new Vector3(i * xMultiplier, y);
            _spriteShapeController.spline.InsertPointAt(i, _lastPos);

            if (i != 0 && i != _levelLength - 1)
            {
                _spriteShapeController.spline.SetTangentMode(i, ShapeTangentMode.Continuous);
                _spriteShapeController.spline.SetLeftTangent(i, Vector3.left * xMultiplier * _curveSmoothness);
                _spriteShapeController.spline.SetRightTangent(i, Vector3.right * xMultiplier * _curveSmoothness);
            }
        }

        _spriteShapeController.spline.InsertPointAt(_levelLength, new Vector3(_lastPos.x, transform.position.y - _bottom));
        _spriteShapeController.spline.InsertPointAt(_levelLength + 1, new Vector3(transform.position.x, transform.position.y - _bottom));
    }

    private void CreateFlag()
    {
        if (_flagPrefab == null) return;

        if (_flagInstance != null)
            Destroy(_flagInstance);

        _flagPosition = _lastPos - new Vector3(5f, 0f, 0f);
        _flagInstance = Instantiate(_flagPrefab, _flagPosition, Quaternion.identity);
    }

    private void ResetPlayerToStart()
    {
        if (_player != null && _spriteShapeController.spline.GetPointCount() > 0)
        {
            Vector3 terrainStartPos = _spriteShapeController.spline.GetPosition(0);
            _player.position = terrainStartPos + Vector3.up * _spawnYOffset;
            _player.rotation = Quaternion.identity;

            Rigidbody rb = _player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            _sessionEnded = false;
        }
    }

    private void EndSession()
    {
        _sessionEnded = true;

        Rigidbody rb = _player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Debug.Log("Jugador llegó al final. END_ACTION");
        reader.SendMessageToApp("END_ACTION");
    }
}
