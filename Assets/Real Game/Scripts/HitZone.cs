using UnityEngine;
using System.Collections.Generic; // Required for List

// Make sure you have this using statement if not already present
// Assumes your TimingAccuracy enum is accessible (e.g., defined in this file or another)

// public enum TimingAccuracy { Perfect, Good, Okay, Miss }
public enum TimingAccuracy
{
    Perfect,
    Good,
    Okay,
    Miss
}

[RequireComponent(typeof(SpriteRenderer))] // Good practice: ensure SpriteRenderer exists
public class HitZone : MonoBehaviour
{
    public int laneIndex; // Assign this in the Inspector (0, 1, 2, 3...)

    [Header("Visuals")]
    public Sprite idleSprite; // Assign the default sprite in the Inspector
    public Sprite pressedSprite; // Assign the sprite for when the key is pressed

    [Header("Timing Windows")]
    public double perfectWindow = 0.05; // +/- 50ms
    public double goodWindow = 0.1;    // +/- 100ms
    public double okayWindow = 0.2;   // +/- 150ms

    // --- Private References ---
    private SpriteRenderer spriteRenderer;
    private List<NoteController> notesInZone = new List<NoteController>();

    private TimingAccuracy JudgeTiming(double timeDifference)
    {
        // ... code inside JudgeTiming uses the TimingAccuracy enum ...
        double absTimeDiff = System.Math.Abs(timeDifference);
        if (absTimeDiff <= perfectWindow) return TimingAccuracy.Perfect; // Use the enum value
        if (absTimeDiff <= goodWindow) return TimingAccuracy.Good;    // Use the enum value
        if (absTimeDiff <= okayWindow) return TimingAccuracy.Okay;   // Use the enum value
        return TimingAccuracy.Miss; // Use the enum value
    }

    void Awake()
    {
        // Get the SpriteRenderer component attached to this same GameObject
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError($"HitZone {laneIndex}: SpriteRenderer component not found on this GameObject!");
        }
    }

    void Start()
    {
        // Set the initial sprite when the game starts
        SetSprite(idleSprite);
    }

    // Subscribe to events when the component becomes active
    void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            // Subscribe to BOTH press and release events
            InputManager.Instance.LanePressed += OnInputPressed;
            InputManager.Instance.LaneReleased += OnInputReleased;
            // Debug.Log($"HitZone {laneIndex}: Subscribed to input events.");
        }
        else
        {
            Debug.LogError($"HitZone {laneIndex}: InputManager.Instance was NULL during OnEnable! Cannot subscribe to input.");
        }
    }

    // Unsubscribe when the component becomes inactive or destroyed
    void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.LanePressed -= OnInputPressed;
            InputManager.Instance.LaneReleased -= OnInputReleased;
            // Debug.Log($"HitZone {laneIndex}: Unsubscribed from input events.");
        }

        // Optional: Reset sprite to idle when disabled (e.g., if game pauses/ends)
        // SetSprite(idleSprite);
    }

    // --- Input Event Handlers ---

    // Method called by InputManager when ANY lane key is PRESSED
    private void OnInputPressed(int pressedLaneIndex)
    {
        // Only react if the event is for THIS HitZone's lane
        if (pressedLaneIndex == this.laneIndex)
        {
            // Debug.Log($"HitZone {this.laneIndex}: Input Pressed.");
            SetSprite(pressedSprite); // Change to pressed sprite

            // --- Trigger the logic to check if a note was hit ---
            CheckNoteHitAttempt();
        }
    }

    // Method called by InputManager when ANY lane key is RELEASED
    private void OnInputReleased(int releasedLaneIndex)
    {
        // Only react if the event is for THIS HitZone's lane
        if (releasedLaneIndex == this.laneIndex)
        {
            // Debug.Log($"HitZone {this.laneIndex}: Input Released.");
            SetSprite(idleSprite); // Change back to idle sprite
        }
    }

    // --- Core Note Hitting Logic (Extracted from old HandleInput) ---

    // This method now ONLY handles checking for note hits when called.
    private void CheckNoteHitAttempt()
    {
        // Debug.Log($"HitZone {this.laneIndex}: CheckNoteHitAttempt RUNNING.");
        if (notesInZone.Count > 0)
        {
            NoteController noteToHit = FindBestNoteToHit();

            // Debug.Log($"HitZone {this.laneIndex}: FindBestNoteToHit returned '{(noteToHit == null ? "NULL" : noteToHit.gameObject.name)}'.");

            if (noteToHit != null)
            {
                double hitTime = Conductor.Instance.GetAudioTime(); // Use Conductor for precise time
                double timeDifference = hitTime - noteToHit.TargetHitTime;
                TimingAccuracy accuracy = JudgeTiming(timeDifference);

                Debug.Log($"HitZone {this.laneIndex}: Judged as '{accuracy}'. Time Diff: {timeDifference * 1000:F1}ms");

                if (accuracy != TimingAccuracy.Miss)
                {
                    // Debug.Log($"HitZone {this.laneIndex}: HIT SUCCESSFUL! Notifying GameManager.");
                    GameManager.Instance?.NoteHit(noteToHit, accuracy, timeDifference); // Notify GameManager
                    notesInZone.Remove(noteToHit); // Remove note from list *after* processing
                    Destroy(noteToHit.gameObject); // Destroy the note GameObject
                }
                else
                {
                    // Optional: Add feedback for pressing the key but missing the timing window?
                     Debug.Log($"HitZone {this.laneIndex}: Hit attempt failed - timing judged as Miss.");
                }
            }
            // else: FindBestNoteToHit returned null - means no note was deemed hittable right now.
        }
        // else: No notes were in the zone when the key was pressed.
        // Debug.Log($"HitZone {this.laneIndex}: CheckNoteHitAttempt - No notes in zone.");
    }


    // --- Helper Methods (Trigger Detection, Timing, etc.) ---

    // Utility method to safely change the sprite
    private void SetSprite(Sprite newSprite)
    {
        if (spriteRenderer != null)
        {
            if (newSprite != null)
            {
                spriteRenderer.sprite = newSprite;
            }
            else
            {
                // Keep existing sprite if the target sprite is null, but warn.
                // Debug.LogWarning($"HitZone {laneIndex}: Tried to set a NULL sprite.");
            }
        }
    }

    // Make sure OnTriggerEnter2D/OnTriggerExit2D are correctly implemented
    void OnTriggerEnter2D(Collider2D other)
    {
        NoteController note = other.GetComponent<NoteController>();
        if (note != null && note.LaneIndex == this.laneIndex)
        {
            // Debug.Log($"---> HitZone {laneIndex}: Note ADDED to zone. Target Time: {note.TargetHitTime}. Notes now in zone: {notesInZone.Count + 1}");
            if (!notesInZone.Contains(note)) // Prevent adding duplicates just in case
            {
                notesInZone.Add(note);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        NoteController note = other.GetComponent<NoteController>();
        if (note != null && note.LaneIndex == this.laneIndex)
        {
            if (notesInZone.Contains(note))
            {
                // Debug.Log($"<--- HitZone {laneIndex}: Note REMOVED from zone. Notes left in zone: {notesInZone.Count - 1}");
                notesInZone.Remove(note);

                // Decide: Should a note leaving the zone *without being hit* count as a miss?
                // If so, you might notify the GameManager here, but be careful not to double-penalize
                // if the NoteController also handles misses when it goes too far.
                // Consider if GameManager.NoteMissed needs protection against being called multiple times for the same note.
            }
        }
    }

    // FindBestNoteToHit and JudgeTiming methods remain the same as before
    private NoteController FindBestNoteToHit()
    {
        // ... (Your existing logic to find the most appropriate note in notesInZone) ...
        if (notesInZone.Count == 0) return null;
        // Simple approach: return the first one (assumes they arrive in order)
        // More robust: Find the one whose TargetHitTime is closest to Conductor.GetAudioTime()
        // and is within the okayWindow.
        NoteController bestNote = null;
        double minDiff = double.MaxValue;
        double currentTime = Conductor.Instance.GetAudioTime();

        foreach (var note in notesInZone)
        {
            double diff = Mathf.Abs((float)(note.TargetHitTime - currentTime));
            if (diff <= okayWindow)
            { // Only consider notes potentially hittable
                if (bestNote == null || note.TargetHitTime < bestNote.TargetHitTime)
                { // Prioritize earliest note if times are close
                    bestNote = note;
                    minDiff = diff;
                }
            }
        }
        return bestNote; // Return the best candidate (or null if none found)
    }
}