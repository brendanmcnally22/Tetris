using TMPro;
using UnityEngine;

public class TetrisManager : MonoBehaviour
{
    // super simple manager script:
    // - finds the board + a text label (if i forgot to drag them in)
    // - listens to the board score event
    // - whenever score changes, it updates the ui text so the player can see it
    // basically just "hook score -> show score" and nothing gameplay related lives here

    [Header("References")]
    public Board board;

    [Header("UI")]
    public TMP_Text scoreText;

    private void Awake()
    {
        // Auto-find if you forget to link
        if (board == null)
            board = FindFirstObjectByType<Board>();

        if (scoreText == null)
            scoreText = FindFirstObjectByType<TMP_Text>();
    }

    private void OnEnable()
    {
        if (board != null)
            board.OnScoreChanged += UpdateScoreUI;
    }

    private void OnDisable()
    {
        if (board != null)
            board.OnScoreChanged -= UpdateScoreUI;
    }

    private void Start()
    {
        if (board != null)
            UpdateScoreUI(board.score);
    }

    void UpdateScoreUI(int newScore)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {newScore}";
    }
}
