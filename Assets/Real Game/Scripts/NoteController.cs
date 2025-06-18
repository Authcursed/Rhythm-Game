using UnityEngine;

public class NoteController : MonoBehaviour
{
    public int LaneIndex { get; private set; }
    public double TargetHitTime { get; private set; }
    public double SpawnTime { get; private set; }
    public float TravelTime { get; private set; }
    public bool IsForbidden { get; private set; } // Property to know its type

    private Vector3 startPosition;
    private Vector3 endPosition;
    private bool isInitialized = false;

    // The initialize method must accept the 'isForbidden' flag from the NoteSpawner
    public void Initialize(int laneIndex, double targetHitTime, double spawnTime, float travelTime, Vector3 startPos, Vector3 endPos, bool isForbidden)
    {
        this.LaneIndex = laneIndex;
        this.TargetHitTime = targetHitTime;
        this.SpawnTime = spawnTime;
        this.TravelTime = travelTime;
        this.IsForbidden = isForbidden; // Set the flag here

        this.startPosition = startPos;
        this.endPosition = endPos;
        transform.position = startPosition;
        this.isInitialized = true;

        // Optional: Pre-position note if spawned slightly late
        double timeSinceSpawn = Conductor.Instance.GetAudioTime() - SpawnTime;
        if (timeSinceSpawn > 0 && TravelTime > 0)
        {
            float progress = (float)(timeSinceSpawn / TravelTime);
            transform.position = Vector3.Lerp(startPosition, endPosition, progress);
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        // Standard movement logic
        double currentTime = Conductor.Instance.GetAudioTime();
        double timeElapsed = currentTime - SpawnTime;
        float progress = (TravelTime > 0) ? (float)(timeElapsed / TravelTime) : 1.0f;
        transform.position = Vector3.LerpUnclamped(startPosition, endPosition, progress);

        // --- THIS IS THE CORRECTED MISS LOGIC ---
        // Check if note went past the hit zone plus a small time buffer
        if (currentTime > TargetHitTime + 0.2) // Using 0.2 seconds as a buffer past the hit time
        {
            // Check if this note is forbidden BEFORE declaring a miss
            if (!this.IsForbidden)
            {
                // It's a regular note, so this is a genuine miss.
                Debug.Log($"Regular Note missed (went too far). Notifying GameManager.");
                GameManager.Instance?.NoteMissed(this);
            }
            else
            {
                // It's a forbidden note that the player correctly avoided.
                // This is a success, so we do NOTHING here except let it get destroyed.
                Debug.Log("Forbidden note successfully avoided (went too far).");
            }

            // Destroy the note in either case once it's far enough away.
            Destroy(gameObject);
        }
    }
}