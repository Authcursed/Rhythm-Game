using UnityEngine;
using UnityEngine.UI; // Keep if you ever use legacy UI
using TMPro; // Use TextMeshPro for better text
using System.Collections; // Needed for Coroutines
using System.Collections.Generic; // Needed for Dictionary

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public bool isGameRunning = false;

    [Header("Scoring")]
    public int score = 0;
    // public int combo = 0; // Changed: Renamed to currentCombo for clarity
    private int currentCombo = 0; // Track combo internally
    public int maxCombo = 0;

    // Score values for each accuracy - adjust as needed
    public int perfectScore = 100;
    public int goodScore = 75;
    public int okayScore = 50;

    [Header("UI References (Optional)")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;    // Assign in Inspector
    public TextMeshProUGUI accuracyText; // Assign in Inspector

    // --- ADDED: References for Canvas Groups (Recommended for fading) ---
    private CanvasGroup accuracyCanvasGroup;
    private CanvasGroup comboCanvasGroup;
    // --- END ADDED ---

    // --- ADDED: Feedback Animation Parameters ---
    [Header("Feedback Animation")]
    public float feedbackDisplayDuration = 0.6f; // Total time from pop start to fade end
    public float feedbackFadeTime = 0.2f;      // How long the fade in/out takes
    public Vector3 feedbackInitialScale = new Vector3(1.5f, 1.5f, 1.5f); // Pop start scale
    public Vector3 feedbackTargetScale = Vector3.one; // Normal scale
    // --- END ADDED ---

    // --- ADDED: Track Running Coroutines ---
    private Dictionary<TextMeshProUGUI, Coroutine> runningFeedbackCoroutines = new Dictionary<TextMeshProUGUI, Coroutine>();
    // --- END ADDED ---


    [Header("Feedback (Optional)")]
    public GameObject hitEffectPrefab; // Prefab to instantiate on hit
    public Transform[] laneHitZoneTransforms; // Reference to hit zone positions for effects

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject); // Optional

        // --- ADDED: Get CanvasGroup components if they exist ---
        if (accuracyText != null) accuracyCanvasGroup = accuracyText.GetComponent<CanvasGroup>();
        if (comboText != null) comboCanvasGroup = comboText.GetComponent<CanvasGroup>();
        // --- END ADDED ---
    }

    void Start()
    {
        // Initialize UI Score text
        UpdateScoreUI();

        // --- CHANGED: Ensure feedback texts start inactive ---
        if (accuracyText != null) accuracyText.gameObject.SetActive(false);
        if (comboText != null) comboText.gameObject.SetActive(false);
        // --- END CHANGED ---

        // Example: Start the game immediately
        StartGame();
    }

    public void StartGame()
    {
        if (isGameRunning) return;

        score = 0;
        currentCombo = 0; // Use internal variable
        maxCombo = 0;
        isGameRunning = true;

        UpdateScoreUI();

        // --- CHANGED: Ensure feedback texts are inactive on start ---
        if (accuracyText != null) accuracyText.gameObject.SetActive(false);
        if (comboText != null) comboText.gameObject.SetActive(false);
        StopAllFeedbackCoroutines(); // Stop any leftovers if restarting
        // --- END CHANGED ---

        Conductor.Instance?.StartSong();
        Debug.Log("Game Started!");
    }

    public void GameOver()
    {
        isGameRunning = false;
        Conductor.Instance?.StopSong();
        StopAllFeedbackCoroutines(); // Stop animations on game over
        Debug.Log($"Game Over! Final Score: {score}, Max Combo: {maxCombo}");
        // Show results screen, etc.
    }


    // Called by HitZone when a note is successfully hit
    public void NoteHit(NoteController note, TimingAccuracy accuracy, double timeDifference)
    {
        if (!isGameRunning) return;

        int scoreToAdd = 0;
        switch (accuracy)
        {
            case TimingAccuracy.Perfect: scoreToAdd = perfectScore; break;
            case TimingAccuracy.Good: scoreToAdd = goodScore; break;
            case TimingAccuracy.Okay: scoreToAdd = okayScore; break;
        }

        score += scoreToAdd * (1 + currentCombo / 10); // Apply combo bonus example
        currentCombo++; // Use internal variable
        if (currentCombo > maxCombo) maxCombo = currentCombo;

        UpdateScoreUI(); // Update score display

        // --- CHANGED: Trigger animated feedback instead of direct UI updates ---
        string accuracyString = accuracy.ToString() + "!";
        string comboString = "Combo " + currentCombo;

        // Determine color based on accuracy
        Color feedbackColor = Color.white;
        switch (accuracy)
        {
            case TimingAccuracy.Perfect: feedbackColor = Color.cyan; break;
            case TimingAccuracy.Good: feedbackColor = Color.green; break;
            case TimingAccuracy.Okay: feedbackColor = Color.yellow; break;
        }

        // Show accuracy feedback with animation
        ShowFeedback(accuracyText, accuracyString, feedbackColor);

        // Show combo feedback with animation (only if combo > 1)
        if (currentCombo > 1)
        {
            ShowFeedback(comboText, comboString, Color.white);
        }
        else if (comboText != null)
        {
            // Hide combo text immediately if combo is 1 or less
            StopFeedbackCoroutine(comboText); // Stop any previous fadeout
            comboText.gameObject.SetActive(false);
        }
        // --- END CHANGED ---

        PlayHitEffect(note.LaneIndex); // Play particle effect

        //Debug.Log($"Hit: {accuracy} (+{scoreToAdd}), Combo: {currentCombo}, Score: {score}");
    }

    // Called by NoteController or HitZone when a note is missed
    public void NoteMissed(NoteController note)
    {
        if (!isGameRunning) return;

        if (currentCombo > 0) // Only process miss feedback if combo was active
        {
            Debug.Log($"Missed Note Lane {note.LaneIndex}! Combo Reset.");
            currentCombo = 0; // Reset combo on miss

            // --- CHANGED: Trigger animated feedback for "MISS" ---
            ShowFeedback(accuracyText, "MISS", Color.red);
            // --- END CHANGED ---

            // --- CHANGED: Stop combo animation & hide combo text immediately ---
            if (comboText != null)
            {
                StopFeedbackCoroutine(comboText);
                comboText.gameObject.SetActive(false);
            }
            // --- END CHANGED ---

        }
        else
        {
            Debug.Log($"Missed Note Lane {note.LaneIndex} (Combo already 0).");
            // Optionally show "MISS" feedback even if combo was 0
            // ShowFeedback(accuracyText, "MISS", Color.red);
        }
        currentCombo = 0; // Ensure combo is 0

        // Optionally play a miss sound/effect
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{score}"; // Use the score variable
        }
    }

    // --- REMOVED UpdateComboUI ---
    // --- REMOVED ShowAccuracy ---
    // --- REMOVED ClearAccuracyUI ---
    // --- REMOVED commented out FadeOutAccuracyText ---

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

    // --- ADDED: Feedback Animation Control Methods ---

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
    // --- END ADDED ---

} // End of GameManager class

// Make sure TimingAccuracy enum is accessible
// If it's not in its own file, you might need:
// public enum TimingAccuracy { Perfect, Good, Okay, Miss }