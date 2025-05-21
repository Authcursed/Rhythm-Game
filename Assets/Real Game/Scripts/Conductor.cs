using UnityEngine;

public class Conductor : MonoBehaviour
{
    public static Conductor Instance { get; private set; }

    public AudioSource musicSource;
    public float songBpm = 115f; // Beats per minute of the song
    public float initialDelay = 1.1f; // Seconds before the first beat hits the judgment line (adjust based on your setup)

    public double songStartTimeDsp; // Precise start time from AudioSettings.dspTime
    private bool isPlaying = false;

    // Calculated values
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
        if (musicSource.clip == null)
        {
            Debug.LogError("No AudioClip assigned to Conductor!");
            return;
        }

        // Record the precise time the song WILL start playing
        // AudioSettings.dspTime provides a highly accurate clock independent of frame rate
        songStartTimeDsp = AudioSettings.dspTime + initialDelay;

        // Schedule the song to play exactly at songStartTimeDsp
        musicSource.PlayScheduled(songStartTimeDsp);

        isPlaying = true;
        Debug.Log($"Song scheduled to start at DSP time: {songStartTimeDsp}");
    }

    public double GetAudioTime()
    {
        if (!isPlaying) return 0.0;
        // Current precise audio time
        return AudioSettings.dspTime;
    }

    public double GetSongPosition()
    {
        if (!isPlaying) return 0.0;
        // Time elapsed since the song *actually* started playing according to the DSP clock
        return AudioSettings.dspTime - songStartTimeDsp;
    }

    public double GetSongPositionInBeats()
    {
        if (!isPlaying) return 0.0;
        // Correct calculation: Time since audio started / seconds per beat
        return (AudioSettings.dspTime - songStartTimeDsp) / SecPerBeat;

        // Or if you still have the GetSongPosition() helper method:
        // return GetSongPosition() / SecPerBeat;
    }
    public double GetDspTimeForBeat(double targetBeat)
    {
        return songStartTimeDsp + initialDelay + (targetBeat * SecPerBeat);
    }

    public bool IsPlaying()
    {
        return isPlaying && musicSource.isPlaying;
    }

    public void StopSong()
    {
        musicSource.Stop();
        isPlaying = false;
    }
}