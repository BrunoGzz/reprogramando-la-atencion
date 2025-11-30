using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using PimDeWitte.UnityMainThreadDispatcher;

public class MindFlexReader : MonoBehaviour
{
    public static MindFlexReader Instance { get; private set; } // Instancia estática para el Singleton
    
    public int port = 5005; // Puerto UDP para escuchar
    private int appListenPort;
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = true;
    private int attention = 0;
    private int dotVisibility = 1;
    private double minAttention = 0.6;
    private int maxSpeed = 80;
    private int accRate = 50;
    private int decRate = 90;
    private int brakeRate = 20;
    private int gameLevel = 1;
    private int gameSeed = 1;
    private int levelLength = 10;
    //private IPEndPoint lastRemoteEndPoint;

    public int Attention => attention;
    public int DotVisibility => dotVisibility;
    public double MinAtt => minAttention;
    public int MaxSpeed => maxSpeed;
    public int AccRate => accRate;
    public int DecRate => decRate;
    public int BrakeRate => brakeRate;
    public int LevelLength => levelLength;
    public int GameLevel => gameLevel;
    public int GameSeed => gameSeed;

    void Awake()
    {
        // Verificar si ya existe una instancia, y destruir este objeto si es así
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Establecer la instancia y asegurarse de que no se destruya al cambiar de escena
        Instance = this;
        DontDestroyOnLoad(this.gameObject); // Evita que se destruya al cambiar de escena
    }

    void Start()
    {
        appListenPort = port + 1;
        // Inicializar el cliente UDP y comenzar el hilo de recepción
        udpClient = new UdpClient(port);
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
        
        Debug.Log("UDP Listener iniciado en puerto " + port);
    }

    void ReceiveData()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
        try
        {
            while (isRunning)
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(data);
                Debug.Log("HELLO!" + message);
                //lastRemoteEndPoint = remoteEndPoint;

                string[] parts = message.Split(',');
                if (parts.Length >= 1)
                {
                    string command = parts[0].Trim().ToUpper();
                    string argument = parts.Length > 1 ? parts[1].Trim() : "";

                    switch (command)
                    {
                        case "ATT":
                            if (int.TryParse(argument, out int attValue))
                            {
                                attention = attValue;
                                Debug.Log("Valor de atención recibido: " + attention);
                            }
                            break;

                        case "SET_DOT":
                            if (int.TryParse(argument, out int dotValue))
                            {
                                dotVisibility = dotValue;
                            }
                            break;
                        
                        case "SET_MIN_ATT":
                            if (int.TryParse(argument, out int minAttValue))
                            {
                                minAttention = minAttValue;
                            }
                            break;
                        
                        case "SET_MAX_SPEED":
                            if (int.TryParse(argument, out int maxSpeedValue))
                            {
                                maxSpeed = maxSpeedValue;
                            }
                            break;
                        
                        case "SET_LVL_LENGTH":
                            if (int.TryParse(argument, out int levelLengthValue))
                            {
                                levelLength = levelLengthValue;
                            }
                            break;

                        case "SET_ACC_RATE":
                            if (int.TryParse(argument, out int accRateValue))
                            {
                                accRate = accRateValue;
                            }
                            break;

                        case "SET_DEC_RATE":
                            if (int.TryParse(argument, out int decRateValue))
                            {
                                decRate = decRateValue;
                            }
                            break;

                        case "SET_BRAKE_RATE":
                            if (int.TryParse(argument, out int brakeRateValue))
                            {
                                brakeRate = brakeRateValue;
                            }
                            break;
                        
                        case "GAME_LEVEL":
                            if (int.TryParse(argument, out int gameLevelValue))
                            {
                                gameLevel = gameLevelValue;
                            }
                            break;

                        case "GAME_SEED":
                            if (int.TryParse(argument, out int gameSeedValue))
                            {
                                gameSeed = gameSeedValue;
                            }
                            break;
                        
                        case "SET_LEVEL":
                            if (UnityMainThreadDispatcher.Exists())
                            {
                            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                                Debug.Log("Cambiando escena a: " + argument);
                                if (int.TryParse(argument, out int sceneIndex))
                                    SceneManager.LoadScene(sceneIndex);
                                else
                                    SceneManager.LoadScene(argument);
                            });
                            }else{
                                Debug.LogWarning("Dispatcher no disponible al intentar cambiar escena.");
                            }
                            break;

                        case "PAUSE_GAME":
                            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                                Debug.Log("Pausando juego...");
                                Time.timeScale = 0f;
                                // También podés mostrar un menú de pausa aquí
                            });
                            break;

                        case "RESUME_GAME":
                            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                                Debug.Log("Reanudando juego...");
                                Time.timeScale = 1f;
                            });
                            break;
                    }
                }
            }
        }
        catch (SocketException ex)
        {
            if (isRunning) Debug.LogError("Error de socket UDP: " + ex.Message);
        }
        catch (Exception ex)
        {
            Debug.LogError("Exception: " + ex.ToString());
        }
    }

    public void SendMessageToApp(string message)
    {
        try
        {
            using (UdpClient sender = new UdpClient())
            {
                IPEndPoint appEndPoint = new IPEndPoint(IPAddress.Loopback, appListenPort);
                byte[] data = Encoding.UTF8.GetBytes(message);
                sender.Send(data, data.Length, appEndPoint);
                Debug.Log($"Mensaje enviado a la app ({appListenPort}): {message}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error al enviar mensaje UDP: " + e.Message);
        }
    }


    void OnApplicationQuit()
    {
        isRunning = false;
        udpClient.Close();
        receiveThread.Abort();
        Debug.Log("UDP Listener cerrado.");
    }
}
