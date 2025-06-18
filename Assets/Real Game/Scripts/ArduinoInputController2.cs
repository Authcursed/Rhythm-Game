using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO.Ports;
using System.Threading;
using System.Collections;

public class ArduinoInputController2 : MonoBehaviour
{   
    // THIS SCRIPT IS FOR MAIN MENU!!!!!
    public static ArduinoInputController2 Instance { get; private set; }

    public string portName = "COM3";
    public int baudRate = 9600;

    private SerialPort serialPort;
    private Thread serialThread;
    private volatile bool isThreadRunning = false;

    private System.Collections.Generic.Queue<string> dataQueue = new System.Collections.Generic.Queue<string>();
    private object queueLock = new object();

    // --- UI and Scene Management ---
    [Header("UI Elements")]
    public GameObject startScreen; // Assign your Start Screen GameObject in the Inspector
    public GameObject songSelectionScreen; // Assign your Song Selection Screen GameObject in the Inspector

    [Header("Scene Navigation")]
    public string gameSceneName = "Game"; // Enter the name of your main game scene

    // Keep track of the current active screen for input context
    private enum MenuState { Start, SongSelection }
    private MenuState currentMenuState = MenuState.Start;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject); // Optional
    }

    void Start()
    {
        OpenSerialPort();
        // Ensure initial UI state is correct
        if (startScreen != null) startScreen.SetActive(true);
        if (songSelectionScreen != null) songSelectionScreen.SetActive(false);
        currentMenuState = MenuState.Start;
    }

    void Update()
    {
        string[] messagesToProcess;
        lock (queueLock)
        {
            messagesToProcess = new string[dataQueue.Count];
            dataQueue.CopyTo(messagesToProcess, 0);
            dataQueue.Clear();
        }

        foreach (string message in messagesToProcess)
        {
            ProcessSerialData(message);
        }
    }

    void ProcessSerialData(string message)
    {
        message = message.Trim();

        if (message.StartsWith("HIT"))
        {
            //Debug.Log("Arduino says: " + message); // Good for debugging

            int laneIndex = -1;
            if (message == "HIT 1") laneIndex = 0;      // Assuming Red Monster is HIT 1 / laneIndex 0
            else if (message == "HIT 2") laneIndex = 1;
            else if (message == "HIT 3") laneIndex = 2;  // Assuming Green Monster is HIT 3 / laneIndex 2
            else if (message == "HIT 4") laneIndex = 3;

            if (laneIndex != -1)
            {
                // --- Menu Navigation Logic ---
                if (currentMenuState == MenuState.Start && laneIndex == 0) // Red monster pressed on Start Screen
                {
                    Debug.Log("Red monster pressed on Start Screen. Activating Song Selection.");
                    if (startScreen != null) startScreen.SetActive(false);
                    if (songSelectionScreen != null) songSelectionScreen.SetActive(true);
                    currentMenuState = MenuState.SongSelection;
                }
                else if (currentMenuState == MenuState.SongSelection && laneIndex == 2) // Green monster pressed on Song Selection
                {
                    Debug.Log("Green monster pressed on Song Selection. Loading game scene.");
                    if (!string.IsNullOrEmpty(gameSceneName))
                    {
                        SceneManager.LoadScene(gameSceneName);
                    }
                    else
                    {
                        Debug.LogError("Game Scene Name is not set in the Inspector!");
                    }
                }
                // --- End Menu Navigation Logic ---
                else if (InputManager.Instance != null) // Handle game input if not in menu or no menu action taken
                {
                    // This part is for your existing in-game rhythm input
                    InputManager.Instance.OnLanePressed(laneIndex);
                    StartCoroutine(DelayedLaneRelease(laneIndex, 0.1f));
                }
                else
                {
                    Debug.LogWarning("InputManager instance not found, but a HIT was registered. Lane: " + laneIndex);
                }
            }
        }
    }

    private IEnumerator DelayedLaneRelease(int laneIndex, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnLaneReleased(laneIndex);
        }
    }

    void OpenSerialPort()
    {
        serialPort = new SerialPort(portName, baudRate);
        serialPort.ReadTimeout = 100;

        try
        {
            serialPort.Open();
            isThreadRunning = true;
            serialThread = new Thread(ReadSerial);
            serialThread.IsBackground = true;
            serialThread.Start();
            Debug.Log("Serial port opened: " + portName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error opening serial port: " + e.Message);
        }
    }

    private void ReadSerial()
    {
        while (isThreadRunning && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                if (serialPort.BytesToRead > 0)
                {
                    string message = serialPort.ReadLine();
                    lock (queueLock)
                    {
                        dataQueue.Enqueue(message);
                    }
                }
            }
            catch (System.TimeoutException)
            {
                // Normal, just continue
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error reading from serial port: " + e.Message);
                isThreadRunning = false;
            }
        }
    }

    void OnDestroy()
    {
        CloseSerialPort();
    }

    void OnApplicationQuit()
    {
        CloseSerialPort();
    }

    void CloseSerialPort()
    {
        isThreadRunning = false;

        if (serialThread != null && serialThread.IsAlive)
        {
            serialThread.Join();
        }

        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            Debug.Log("Serial port closed.");
        }
    }
}