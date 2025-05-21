// Inside Conductor.cs
using UnityEngine;

public class Conductor : MonoBehaviour
{
    public static Conductor Instance { get; private set; }

    public AudioSource musicSource;
    public float songBpm = 120f;
    public float initialDelay = 1.0f;

    public double songStartTimeDsp; // Made public for NoteSpawner if needed, or use getter
    private bool isPlaying = false;
    private bool songFinishedNotified = false; // --- ADDED: Flag to prevent multiple notifications ---

    public double SecPerBeat { get; private set; }
    public double BeatsPerSec { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject); // Optional

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            Debug.LogWarning("Conductor created its own AudioSource.");
            // Assign your AudioClip via Inspector or code
        }

        SecPerBeat = 60.0 / songBpm;
        BeatsPerSec = songBpm / 60.0;
    }

    public void StartSong(AudioClip clip = null)
    {
        if (clip != null) musicSource.clip = clip;
        if (musicSource.clip == null) { Debug.LogError("No AudioClip!"); return; }

        SecPerBeat = 60.0 / songBpm;
        BeatsPerSec = songBpm / 60.0;

        songStartTimeDsp = AudioSettings.dspTime + initialDelay;
        musicSource.PlayScheduled(songStartTimeDsp);
        isPlaying = true;
        songFinishedNotified = false; // --- ADDED: Reset flag on new song start ---
        Debug.Log($"Song scheduled. DSP Start: {songStartTimeDsp}, Duration: {musicSource.clip.length}s");
    }

    void Update()
    {
        if (!isPlaying || musicSource == null || musicSource.clip == null || songFinishedNotified)
        {
            return;
        }

        // Check if song has effectively ended
        // Using GetSongPosition() which is (AudioSettings.dspTime - songStartTimeDsp)
        double currentSongTime = GetSongPosition();

        // musicSource.isPlaying can turn false slightly before or after time matches length
        // So we check both time and the isPlaying flag after a certain point.
        if (currentSongTime >= musicSource.clip.length - 0.05f) // Small buffer for precision
        {
            // If time is past duration OR if Unity reports it's no longer playing (and it wasn't just stopped by us)
            if (currentSongTime >= musicSource.clip.length || !musicSource.isPlaying)
            {
                HandleSongFinished();
            }
        }
    }

    private void HandleSongFinished()
    {
        if (songFinishedNotified) return; // Already handled

        isPlaying = false; // Update internal flag
        songFinishedNotified = true;
        Debug.Log("Conductor: Song playback finished.");
        GameManager.Instance?.SongFinished();
    }

    public double GetAudioTime() { return AudioSettings.dspTime; }
    public double GetSongPosition() { return isPlaying ? (AudioSettings.dspTime - songStartTimeDsp) : 0.0; }
    public double GetSongPositionInBeats() { return GetSongPosition() / SecPerBeat; }
    public double GetDspTimeForBeat(double beat) { return songStartTimeDsp + initialDelay + (beat * SecPerBeat); }
    public bool IsPlaying() { return isPlaying && (musicSource != null && musicSource.isPlaying); } // More robust check

    public void StopSong()
    {
        if (musicSource != null) musicSource.Stop();
        isPlaying = false;
        // songFinishedNotified = true; // Consider if manual stop should trigger results
    }
}