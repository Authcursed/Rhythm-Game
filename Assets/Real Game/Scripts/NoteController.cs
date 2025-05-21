using UnityEngine;

public class NoteController : MonoBehaviour
{
    public int LaneIndex { get; private set; }
    public double TargetHitTime { get; private set; } // Precise time the note should be hit (using AudioSettings.dspTime)
    public double SpawnTime { get; private set; } // When the note was actually spawned
    public float TravelTime { get; private set; } // Time it takes to travel from spawn to hit zone

    private Vector3 startPosition;
    private Vector3 endPosition;
    private bool isInitialized = false;

    public void Initialize(int laneIndex, double targetHitTime, double spawnTime, float travelTime, Vector3 startPos, Vector3 endPos)
    {
        LaneIndex = laneIndex;
        TargetHitTime = targetHitTime;
        SpawnTime = spawnTime;
        TravelTime = travelTime; // Usually constant, determines note speed visual
        startPosition = startPos;
        endPosition = endPos;

        transform.position = startPosition; // Set initial position
        isInitialized = true;

        // Optional: Calculate initial position based on how much time has already passed since spawn
        // if you spawn slightly late relative to the ideal pre-calculation
        double timeSinceSpawn = Conductor.Instance.GetAudioTime() - spawnTime;
        if (timeSinceSpawn > 0 && TravelTime > 0)
        {
            float progress = (float)(timeSinceSpawn / TravelTime);
            transform.position = Vector3.Lerp(startPosition, endPosition, progress);
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        // Calculate the note's current position based on audio time
        double currentTime = Conductor.Instance.GetAudioTime(); // Get precise audio time
        double timeElapsed = currentTime - SpawnTime;
        float progress = (TravelTime > 0) ? (float)(timeElapsed / TravelTime) : 1.0f; // Avoid division by zero

        // Lerp (Linear Interpolation) moves the note smoothly
        transform.position = Vector3.LerpUnclamped(startPosition, endPosition, progress);

        // Optional: Destroy note if it goes too far past the hit zone (missed)
        if (progress > 1.1f) // Adjust margin as needed
        {
            // Notify GameManager/ScoreManager about the miss BEFORE destroying
            GameManager.Instance?.NoteMissed(this);
            Destroy(gameObject);
        }
    }
}