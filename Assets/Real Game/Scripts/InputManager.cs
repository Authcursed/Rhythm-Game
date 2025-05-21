using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; } // Simple Singleton pattern

    private PlayerInputActions playerInputActions;

    // Events for other scripts to subscribe to
    public event System.Action<int> LanePressed; // Sends lane index (0-based)
    public event System.Action<int> LaneReleased; // Sends lane index

    void Awake()
    {
        //Debug.Log("InputManager Awake!");
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject); // Optional: if needed across scenes

        playerInputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        //Debug.Log("InputManager OnEnable! Enabling Gameplay map.");
        playerInputActions.Gameplay.Enable();

        // Subscribe to the 'performed' (press) and 'canceled' (release) events
        playerInputActions.Gameplay.Lane1.performed += ctx => OnLanePressed(0);
        playerInputActions.Gameplay.Lane2.performed += ctx => OnLanePressed(1);
        playerInputActions.Gameplay.Lane3.performed += ctx => OnLanePressed(2);
        playerInputActions.Gameplay.Lane4.performed += ctx => OnLanePressed(3);
        // Add more lanes if needed

        playerInputActions.Gameplay.Lane1.canceled += ctx => OnLaneReleased(0);
        playerInputActions.Gameplay.Lane2.canceled += ctx => OnLaneReleased(1);
        playerInputActions.Gameplay.Lane3.canceled += ctx => OnLaneReleased(2);
        playerInputActions.Gameplay.Lane4.canceled += ctx => OnLaneReleased(3);
        // Add more lanes if needed
    }

    private void OnDisable()
    {
        //Debug.Log("InputManager OnDisable! Disabling Gameplay map.");
        if (playerInputActions != null)
        {
            playerInputActions.Gameplay.Lane1.performed -= ctx => OnLanePressed(0);
            playerInputActions.Gameplay.Lane2.performed -= ctx => OnLanePressed(1);
            playerInputActions.Gameplay.Lane3.performed -= ctx => OnLanePressed(2);
            playerInputActions.Gameplay.Lane4.performed -= ctx => OnLanePressed(3);

            playerInputActions.Gameplay.Lane1.canceled -= ctx => OnLaneReleased(0);
            playerInputActions.Gameplay.Lane2.canceled -= ctx => OnLaneReleased(1);
            playerInputActions.Gameplay.Lane3.canceled -= ctx => OnLaneReleased(2);
            playerInputActions.Gameplay.Lane4.canceled -= ctx => OnLaneReleased(3);

            playerInputActions.Gameplay.Disable();
        }
    }

    public void OnLanePressed(int laneIndex)
    {
        //Debug.Log($"Lane {laneIndex + 1} Pressed");
        LanePressed?.Invoke(laneIndex); // Trigger the event
    }

    public void OnLaneReleased(int laneIndex)
    {
        //Debug.Log($"Lane {laneIndex + 1} Released");
        LaneReleased?.Invoke(laneIndex); // Trigger the event
    }
}