using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // Needed for Retrying/Changing scenes

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public bool isGameRunning = false;

    [Header("Scoring")]
    public int score = 0;
    private int currentCombo = 0;
    public int maxCombo = 0;

    // --- ADDED: Accuracy Counters ---
    private int perfectCount = 0;
    private int goodCount = 0;
    private int okayCount = 0;
    private int missCount = 0;
    // --- END ADDED ---

    public int perfectScore = 100;
    public int goodScore = 75;
    public int okayScore = 50;

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI accuracyText;

    // --- ADDED: Results Panel UI References ---
    [Header("Results Panel UI")]
    public GameObject resultsPanel; // The parent panel for results
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalMaxComboText;
    public TextMeshProUGUI perfectsCountText;
    public TextMeshProUGUI goodsCountText;
    public TextMeshProUGUI okaysCountText;
    public TextMeshProUGUI missesCountText;
    public TextMeshProUGUI rankText; // For displaying S, A, B, etc.
    // --- END ADDED ---

    private CanvasGroup accuracyCanvasGroup;
    private CanvasGroup comboCanvasGroup;

    [Header("Feedback Animation")]
    public float feedbackDisplayDuration = 0.6f;
    public float feedbackFadeTime = 0.2f;
    public Vector3 feedbackInitialScale = new Vector3(1.5f, 1.5f, 1.5f);
    public Vector3 feedbackTargetScale = Vector3.one;

    private Dictionary<TextMeshProUGUI, Coroutine> runningFeedbackCoroutines = new Dictionary<TextMeshProUGUI, Coroutine>();

    [Header("Feedback Effects")]
    public GameObject hitEffectPrefab;
    public Transform[] laneHitZoneTransforms;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (accuracyText != null) accuracyCanvasGroup = accuracyText.GetComponent<CanvasGroup>();
        if (comboText != null) comboCanvasGroup = comboText.GetComponent<CanvasGroup>();
    }

    void Start()
    {
        UpdateScoreUI();
        if (accuracyText != null) accuracyText.gameObject.SetActive(false);
        if (comboText != null) comboText.gameObject.SetActive(false);
        // --- ADDED: Ensure results panel is hidden at start ---
        if (resultsPanel != null) resultsPanel.SetActive(false);
        // --- END ADDED ---
        StartGame();
    }

    public void StartGame()
    {
        if (isGameRunning && Conductor.Instance.IsPlaying()) return; // Prevent restart if already running

        score = 0;
        currentCombo = 0;
        maxCombo = 0;
        // --- ADDED: Reset accuracy counters ---
        perfectCount = 0;
        goodCount = 0;
        okayCount = 0;
        missCount = 0;
        // --- END ADDED ---

        isGameRunning = true;
        UpdateScoreUI();

        if (accuracyText != null) accuracyText.gameObject.SetActive(false);
        if (comboText != null) comboText.gameObject.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false); // Hide results panel
        StopAllFeedbackCoroutines();

        Conductor.Instance?.StartSong();
        Debug.Log("Game Started!");
    }

    // Called by Conductor when song ends
    public void SongFinished()
    {
        if (!isGameRunning) return; // Don't show results if game wasn't running (e.g. already game over)

        isGameRunning = false;
        Conductor.Instance?.StopSong(); // Ensure music is explicitly stopped if it wasn't already
        StopAllFeedbackCoroutines();    // Stop any pop-up text animations

        Debug.Log("Song Finished! Displaying Results.");
        DisplayResults();
    }

    void DisplayResults()
    {
        if (resultsPanel == null)
        {
            Debug.LogError("Results Panel not assigned in GameManager!");
            return;
        }

        // Populate the text fields with ONLY the numbers
        if (finalScoreText != null) finalScoreText.text = score.ToString();
        if (finalMaxComboText != null) finalMaxComboText.text = maxCombo.ToString();
        if (perfectsCountText != null) perfectsCountText.text = perfectCount.ToString();
        if (goodsCountText != null) goodsCountText.text = goodCount.ToString();       
        if (okaysCountText != null) okaysCountText.text = okayCount.ToString();
        if (missesCountText != null) missesCountText.text = missCount.ToString();

        string rank = CalculateRank();
        if (rankText != null) rankText.text = rank;


        resultsPanel.SetActive(true); // Show the panel
    }

    string CalculateRank() // Example ranking logic
    {
        int totalNotes = perfectCount + goodCount + okayCount + missCount;
        if (totalNotes == 0) return "N/A"; // No notes played

        double accuracyPercentage = (double)(perfectCount * 1.0 + goodCount * 0.75 + okayCount * 0.5) / totalNotes;

        if (missCount == 0 && perfectCount == totalNotes) return "SS"; // Perfect Clear
        if (accuracyPercentage >= 0.95 && missCount <= 1) return "S";
        if (accuracyPercentage >= 0.90) return "A";
        if (accuracyPercentage >= 0.80) return "B";
        if (accuracyPercentage >= 0.70) return "C";
        return "D";
    }

    public void GameOver() // If you implement a fail state (e.g. health bar)
    {
        if (!isGameRunning) return;
        isGameRunning = false;
        Conductor.Instance?.StopSong();
        StopAllFeedbackCoroutines();
        Debug.Log($"Game Over! Final Score: {score}, Max Combo: {maxCombo}");
        DisplayResults(); // Show results even on game over
    }

    public void NoteHit(NoteController note, TimingAccuracy accuracy, double timeDifference)
    {
        if (!isGameRunning) return;

        int scoreToAdd = 0;
        // --- ADDED: Increment accuracy counters ---
        switch (accuracy)
        {
            case TimingAccuracy.Perfect: scoreToAdd = perfectScore; perfectCount++; break;
            case TimingAccuracy.Good:    scoreToAdd = goodScore;    goodCount++; break;
            case TimingAccuracy.Okay:    scoreToAdd = okayScore;    okayCount++; break;
        }
        // --- END ADDED ---

        score += scoreToAdd * (1 + currentCombo / 10);
        currentCombo++;
        if (currentCombo > maxCombo) maxCombo = currentCombo;

        UpdateScoreUI();

        string accuracyString = accuracy.ToString() + "!";
        string comboString = "Combo " + currentCombo;
        Color feedbackColor = Color.white;
        switch (accuracy) {
            case TimingAccuracy.Perfect: feedbackColor = Color.cyan; break;
            case TimingAccuracy.Good:    feedbackColor = Color.green; break;
            case TimingAccuracy.Okay:    feedbackColor = Color.yellow; break;
        }
        ShowFeedback(accuracyText, accuracyString, feedbackColor);
        if (currentCombo > 1) ShowFeedback(comboText, comboString, Color.white);
        else if (comboText != null) { StopFeedbackCoroutine(comboText); comboText.gameObject.SetActive(false); }

        PlayHitEffect(note.LaneIndex);
    }

    public void NoteMissed(NoteController note)
    {
        if (!isGameRunning) return;

        // --- ADDED: Increment miss counter ---
        missCount++;
        // --- END ADDED ---

        if (currentCombo > 0)
        {
            Debug.Log($"Missed Note! Combo Reset."); // Removed laneIndex as note can be null
            ShowFeedback(accuracyText, "MISS", Color.red);
        } else {
             Debug.Log($"Missed Note (Combo already 0).");
        }
        currentCombo = 0;
        if (comboText != null) { StopFeedbackCoroutine(comboText); comboText.gameObject.SetActive(false); }
    }

    void UpdateScoreUI() { if (scoreText != null) scoreText.text = $"{score}"; }
    void PlayHitEffect(int laneIndex)
    {
        if (hitEffectPrefab != null && laneHitZoneTransforms != null && laneIndex >= 0 && laneIndex < laneHitZoneTransforms.Length)
        {
            if (laneHitZoneTransforms[laneIndex] != null)
            { // Added null check for transform
                Instantiate(hitEffectPrefab, laneHitZoneTransforms[laneIndex].position, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning($"HitZone Transform for lane {laneIndex} is not assigned in GameManager.");
            }

        }
    }
    private void ShowFeedback(TextMeshProUGUI textElement, string message, Color color)
    {
        if (textElement == null) return; // Don't try to show feedback if UI element isn't assigned

        StopFeedbackCoroutine(textElement); // Stop existing animation first

        textElement.text = message;
        textElement.color = new Color(color.r, color.g, color.b, 1f); // Set color (alpha handled by coroutine)

        Coroutine newCoroutine = StartCoroutine(AnimateFeedbackCoroutine(textElement));
        runningFeedbackCoroutines[textElement] = newCoroutine;
    }
    private void StopFeedbackCoroutine(TextMeshProUGUI textElement)
    {
        if (textElement != null && runningFeedbackCoroutines.ContainsKey(textElement) && runningFeedbackCoroutines[textElement] != null)
        {
            StopCoroutine(runningFeedbackCoroutines[textElement]);
            runningFeedbackCoroutines[textElement] = null;
        }
    }
    private void StopAllFeedbackCoroutines()
    {
        // Stop all known running feedback coroutines
        // Important if restarting/ending game abruptly
        if (runningFeedbackCoroutines == null) return;

        // Create a temporary list of keys to avoid modifying dictionary while iterating
        List<TextMeshProUGUI> keys = new List<TextMeshProUGUI>(runningFeedbackCoroutines.Keys);

        foreach (var textElement in keys)
        {
            StopFeedbackCoroutine(textElement);
            // Optionally ensure they are hidden
            if (textElement != null) textElement.gameObject.SetActive(false);
        }
        runningFeedbackCoroutines.Clear(); // Clear the tracking dictionary
    }
    private IEnumerator AnimateFeedbackCoroutine(TextMeshProUGUI textElement)
    {
        RectTransform rectTransform = textElement.rectTransform;
        // Try to get CanvasGroup for potentially smoother fading
        CanvasGroup canvasGroup = textElement.GetComponent<CanvasGroup>();

        textElement.gameObject.SetActive(true);

        // Initial state for animation
        rectTransform.localScale = feedbackInitialScale;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        else textElement.alpha = 0f; // Fallback to text alpha if no CanvasGroup

        // Calculate durations
        float fadeInOutTime = Mathf.Min(feedbackFadeTime, feedbackDisplayDuration / 2f);
        float holdDuration = feedbackDisplayDuration - (fadeInOutTime * 2f);
        if (holdDuration < 0) holdDuration = 0; // Prevent negative hold time


        // Phase 1: Pop Scale & Fade In
        float timer = 0f;
        while (timer < fadeInOutTime)
        {
            float progress = timer / fadeInOutTime;
            rectTransform.localScale = Vector3.Lerp(feedbackInitialScale, feedbackTargetScale, progress);
            float currentAlpha = Mathf.Lerp(0f, 1f, progress);
            if (canvasGroup != null) canvasGroup.alpha = currentAlpha;
            else textElement.alpha = currentAlpha;

            timer += Time.deltaTime;
            yield return null;
        }
        rectTransform.localScale = feedbackTargetScale;
        if (canvasGroup != null) canvasGroup.alpha = 1f; else textElement.alpha = 1f;


        // Phase 2: Hold
        if (holdDuration > 0) yield return new WaitForSeconds(holdDuration);


        // Phase 3: Fade Out
        timer = 0f;
        while (timer < fadeInOutTime)
        {
            float progress = timer / fadeInOutTime;
            float currentAlpha = Mathf.Lerp(1f, 0f, progress);
            if (canvasGroup != null) canvasGroup.alpha = currentAlpha;
            else textElement.alpha = currentAlpha;

            timer += Time.deltaTime;
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 0f; else textElement.alpha = 0f;


        // Cleanup
        textElement.gameObject.SetActive(false);
        if (runningFeedbackCoroutines.ContainsKey(textElement))
        {
            runningFeedbackCoroutines[textElement] = null;
        }
    }

    // --- ADDED: UI Button Handlers for Results Panel ---
    public void OnRetryButtonPressed()
    {
        Debug.Log("Retry Button Pressed!");
        if (resultsPanel != null) resultsPanel.SetActive(false);
        // Option 1: Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        // Option 2: Just call StartGame (ensure everything is reset properly)
        // StartGame();
    }

    public void OnMainMenuButtonPressed()
    {
        Debug.Log("Main Menu Button Pressed!");
        if (resultsPanel != null) resultsPanel.SetActive(false);
        // Replace "MainMenuScene" with the actual name of your main menu scene
        // SceneManager.LoadScene("MainMenuScene");
        Debug.LogWarning("Load Main Menu Scene - Not Implemented Yet");
    }
    // --- END ADDED ---
}