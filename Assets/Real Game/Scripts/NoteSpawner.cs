using UnityEngine;
using System.Collections.Generic; // Required for Lists

// Simple structure to hold note data
[System.Serializable]
public class NoteData
{
    public double targetBeat; // When the note should be hit (in beats)
    public int laneIndex; // Which lane (0-based)
    // Add other properties if needed (e.g., note type for holds)
}

public class NoteSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject[] notePrefabsPerLane; // Assign your Note Prefab here
    public Transform[] laneSpawnPoints; // Assign the SpawnPoint GameObjects for each lane
    public Transform[] laneHitZones;   // Assign the HitZone GameObjects for each lane

    [Header("Note Chart")]
    public List<NoteData> notesToSpawn = new List<NoteData>(); // Your level data

    [Header("Settings")]
    public float noteTravelTime = 2.0f; // Time (in seconds) for a note to travel from spawn to hit zone

    private int nextNoteIndex = 0;

    void Start()
    {
        if (Conductor.Instance == null)
        {
            Debug.LogError("Conductor instance not found!");
            enabled = false; // Disable this script if Conductor is missing
            return;
        }

        // Optional: Sort notes by beat time just in case they aren't ordered
        notesToSpawn.Sort((a, b) => a.targetBeat.CompareTo(b.targetBeat));
    }


    void Update()
    {
        if (!Conductor.Instance.IsPlaying() || nextNoteIndex >= notesToSpawn.Count)
        {
            return; // Don't spawn if song isn't playing or no notes left
        }

        // Get current song position in beats
        double currentBeat = Conductor.Instance.GetSongPositionInBeats();

        // Calculate how many beats ahead we need to look to spawn notes
        double beatsToLookAhead = noteTravelTime / Conductor.Instance.SecPerBeat;

        // Check if the next note needs to be spawned
        if (notesToSpawn[nextNoteIndex].targetBeat <= currentBeat + beatsToLookAhead)
        {
            SpawnNote(notesToSpawn[nextNoteIndex]);
            nextNoteIndex++;
        }
    }

    void SpawnNote(NoteData noteData)
    {
        if (notePrefabsPerLane == null || notePrefabsPerLane.Length <= noteData.laneIndex || notePrefabsPerLane[noteData.laneIndex] == null)
        {
            Debug.LogError($"Note Prefab Per Lane not assigned or invalid for index {noteData.laneIndex}"); return;
        }
        if (noteData.laneIndex < 0 || noteData.laneIndex >= laneSpawnPoints.Length || noteData.laneIndex >= laneHitZones.Length)
        {
            Debug.LogError($"Invalid lane index: {noteData.laneIndex}");
            return;
        }


        // Calculate the precise time this note should be hit using the Conductor's start time and beat info
        double targetHitDspTime = Conductor.Instance.GetDspTimeForBeat(noteData.targetBeat);
        // Calculate the precise time this note *should* be spawned
        double spawnDspTime = targetHitDspTime - noteTravelTime;


        // Instantiate the note prefab
        GameObject noteObject = Instantiate(notePrefabsPerLane[noteData.laneIndex], laneSpawnPoints[noteData.laneIndex].position, Quaternion.identity);
        NoteController noteController = noteObject.GetComponent<NoteController>();

        if (noteController != null)
        {
            // Initialize the note with all necessary timing and position info
            noteController.Initialize(
                noteData.laneIndex,
                targetHitDspTime,
                spawnDspTime, // Pass the calculated ideal spawn time
                noteTravelTime,
                laneSpawnPoints[noteData.laneIndex].position,
                laneHitZones[noteData.laneIndex].position
            );
        }
        else
        {
            Debug.LogError("Instantiated note prefab is missing NoteController script!");
            Destroy(noteObject); // Clean up invalid object
        }
    }
}