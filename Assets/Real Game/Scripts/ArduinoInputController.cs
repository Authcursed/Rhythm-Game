using UnityEngine;
using System.IO.Ports; // Required for serial communication
using System.Threading; // Required for running serial reading on a separate thread
using System.Collections;

public class ArduinoInputController : MonoBehaviour
{
    public static ArduinoInputController Instance { get; private set; } // Optional Singleton

    // Serial Port Configuration
    public string portName = "COM3"; // CHANGE THIS to your Arduino's serial port (e.g., "COM3" on Windows, "/dev/tty.usbmodemXXXX" on Mac)
    public int baudRate = 9600;

    private SerialPort serialPort;
    private Thread serialThread;
    private volatile bool isThreadRunning = false; // Volatile for thread safety

    // Queue to pass data from serial thread to main thread
    private System.Collections.Generic.Queue<string> dataQueue = new System.Collections.Generic.Queue<string>();
    private object queueLock = new object(); // For thread-safe access to the queue

    void Awake()
    {
        // Optional Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject); // Optional: if needed across scenes
    }

    void Start()
    {
        OpenSerialPort();
    }

    void Update()
    {
        // Process data from the queue on the main thread
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
        message = message.Trim(); // Clean up whitespace

        // We are only interested in "HIT X" messages for game input
        if (message.StartsWith("HIT"))
        {
            //Debug.Log("Arduino says: " + message);

            if (InputManager.Instance != null)
            {
                int laneIndex = -1;
                if (message == "HIT 1") laneIndex = 0;
                else if (message == "HIT 2") laneIndex = 1;
                else if (message == "HIT 3") laneIndex = 2;
                else if (message == "HIT 4") laneIndex = 3;

                if (laneIndex != -1)
                {
                    // Assuming OnLanePressed in InputManager is public
                    InputManager.Instance.OnLanePressed(laneIndex);
                    StartCoroutine(DelayedLaneRelease(laneIndex, 0.1f));
                }
            }
            else
            {
                Debug.LogError("InputManager instance not found!");
            }
        }
        // You can add else-if blocks here to parse other messages like "val1: ..., val2: ..." if needed for other purposes
    }

    private IEnumerator DelayedLaneRelease(int laneIndex, float delay)
    {
        yield return new WaitForSeconds(delay); // Wait for the specified delay

        if (InputManager.Instance != null)
        {
            // Make sure OnLaneReleased in InputManager is public
            InputManager.Instance.OnLaneReleased(laneIndex);
            // +Debug.Log($"Simulated Release for Lane {laneIndex} after {delay}s");
            // Or if OnLaneReleased is private:
            // InputManager.Instance.SendMessage("OnLaneReleased", laneIndex, SendMessageOptions.DontRequireReceiver);
        }
    }

    // --- Helper methods for simulated release (Example) ---
    // void ReleaseLane0() { if(InputManager.Instance != null) InputManager.Instance.SendMessage("OnLaneReleased", 0, SendMessageOptions.DontRequireReceiver); }
    // void ReleaseLane1() { if(InputManager.Instance != null) InputManager.Instance.SendMessage("OnLaneReleased", 1, SendMessageOptions.DontRequireReceiver); }
    // void ReleaseLane2() { if(InputManager.Instance != null) InputManager.Instance.SendMessage("OnLaneReleased", 2, SendMessageOptions.DontRequireReceiver); }
    // void ReleaseLane3() { if(InputManager.Instance != null) InputManager.Instance.SendMessage("OnLaneReleased", 3, SendMessageOptions.DontRequireReceiver); }


    void OpenSerialPort()
    {
        serialPort = new SerialPort(portName, baudRate);
        serialPort.ReadTimeout = 100; // Milliseconds

        try
        {
            serialPort.Open();
            isThreadRunning = true;
            serialThread = new Thread(ReadSerial);
            serialThread.IsBackground = true; // Important: allows Unity to close the thread when exiting play mode
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
                // ReadLine can timeout, this is normal, just continue
            }
            catch (System.Exception e)
            {
                // If the port is closed or another error occurs
                Debug.LogError("Error reading from serial port: " + e.Message);
                isThreadRunning = false; // Stop the thread
            }
        }
    }

    void OnDestroy() // Or OnApplicationQuit for builds
    {
        CloseSerialPort();
    }

    void OnApplicationQuit() // Ensure port is closed on application quit
    {
        CloseSerialPort();
    }

    void CloseSerialPort()
    {
        isThreadRunning = false; // Signal the thread to stop

        if (serialThread != null && serialThread.IsAlive)
        {
            serialThread.Join(); // Wait for the thread to finish
        }

        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            Debug.Log("Serial port closed.");
        }
    }
}