using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class JourneyLedger : MonoBehaviour
{
    public static JourneyLedger Instance;

    public Text scoreText;
    public Text killText;
    public Color pulseColor = new Color(1f, 0.82f, 0.18f);
    public float pulseSeconds = 0.18f;

    private Color restingScoreColor;
    private int score;
    private int defeats;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Lives with the scene so a restart gets a fresh score and HUD references.
        Instance = this;
    }

    void Start()
    {
        if (scoreText != null)
            restingScoreColor = scoreText.color;

        DrawHud();
    }

    public void AwardEnemyDefeat(int points)
    {
        score += Mathf.Max(0, points);
        defeats++;
        DrawHud();

        if (scoreText != null)
            StartCoroutine(PulseScoreText());
    }

    void DrawHud()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;

        if (killText != null)
            killText.text = "Defeated: " + defeats;
    }

    public int GetScore()
    {
        return score;
    }

    public int GetKills()
    {
        return defeats;
    }

    public void ResetLedger()
    {
        score = 0;
        defeats = 0;
        DrawHud();
    }

    IEnumerator PulseScoreText()
    {
        scoreText.color = pulseColor;
        yield return new WaitForSeconds(pulseSeconds);
        scoreText.color = restingScoreColor;
    }
}
