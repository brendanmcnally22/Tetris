using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Board : MonoBehaviour
{
    public TetrominoData[] tetrominos;
    public Tilemap tilemap;
    public Vector2Int boardSize = new Vector2Int(10, 20);
    public Piece piecePrefab;

    [Header("Scoring")]
    public int score;
    public event Action<int> OnScoreChanged;

    [Header("Preset Board State")]
    public bool usePresetBoard = true;

    [Header("Piece Sequencing (No RNG)")]
    public bool useSequence = true;
    public bool loopSequence = true;

    [Tooltip("Exact order pieces will spawn in (edit in Inspector).")]
    public Tetromino[] sequence =
{
    Tetromino.P,
    Tetromino.P,
    Tetromino.T,
    Tetromino.L,
    Tetromino.I,
    Tetromino.O,
    Tetromino.P
};


    private int sequenceIndex = 0;

    // '#' = filled, '.' = empty (20 rows top->bottom, 10 columns)
    private static readonly string[] PRESET_BOARD = new string[]
    {
        "..........",
        "..........",
        "..........",
        "..........",
        "..........",
        "..########",
        "..########",
        ".#########",
        "#######...",
        "########..",
        "...#######",
        "#.########",
        "#######..#",
        "########.#",
        "########.#",
        "....######",
        "########..",
        "########..",
        "#####...##",
        "######..##",
    };

    private void Start()
    {
        if (usePresetBoard)
            LoadPresetBoard(PRESET_BOARD);

        SpawnPiece();
        OnScoreChanged?.Invoke(score);
    }

    private Tetromino GetNextTetromino()
    {
        if (useSequence && sequence != null && sequence.Length > 0)
        {
            Tetromino t = sequence[sequenceIndex];

            sequenceIndex++;
            if (sequenceIndex >= sequence.Length)
                sequenceIndex = loopSequence ? 0 : sequence.Length - 1;

            return t;
        }

        // Fallback RNG
        return tetrominos[UnityEngine.Random.Range(0, tetrominos.Length)].tetromino;
    }

    public void SpawnPiece()
    {
        Piece piece = Instantiate(piecePrefab, transform);

        Tetromino chosen = GetNextTetromino();
        piece.Initialize(this, chosen);

        // --- Spawn at TOP of the grid (AUTO-FIT HEIGHT) ---
        int top = boardSize.y / 2;

        int maxCellY = 0;
        for (int i = 0; i < piece.cells.Length; i++)
            maxCellY = Mathf.Max(maxCellY, piece.cells[i].y);

        // Place so highest block lands on (top - 1)
        piece.position = new Vector2Int(0, (top - 1) - maxCellY);
        piece.ResetStepTimer();

        // Game Over check (spawn blocked or out-of-bounds)
        if (!IsValidPosition(piece.position, piece.cells))
        {
            Debug.Log("GAME OVER (spawn blocked)");
            Destroy(piece.gameObject);
            return;
        }

        Set(piece);
    }

    private void LoadPresetBoard(string[] rowsTopToBottom)
    {
        if (rowsTopToBottom == null || rowsTopToBottom.Length == 0)
            return;

        tilemap.ClearAllTiles();

        int width = boardSize.x;
        int height = boardSize.y;

        int rowsToLoad = Mathf.Min(height, rowsTopToBottom.Length);

        for (int r = 0; r < rowsToLoad; r++)
        {
            string row = rowsTopToBottom[r];
            if (string.IsNullOrEmpty(row)) continue;

            int colsToLoad = Mathf.Min(width, row.Length);

            for (int c = 0; c < colsToLoad; c++)
            {
                if (row[c] != '#') continue;

                int x = -width / 2 + c;
                int y = (height / 2 - 1) - r;

                tilemap.SetTile(new Vector3Int(x, y, 0), GetLockedTile());
            }
        }
    }

    // Uses your existing tetromino tiles as a “locked” tile source.
    private TileBase GetLockedTile()
    {
        if (tetrominos != null && tetrominos.Length > 0)
            return tetrominos[0].tile;

        return null;
    }

    public void Clear(Piece piece)
    {
        for (int i = 0; i < piece.cells.Length; i++)
        {
            Vector3Int pos = (Vector3Int)(piece.cells[i] + piece.position);
            tilemap.SetTile(pos, null);
        }
    }

    public void Set(Piece piece)
    {
        for (int i = 0; i < piece.cells.Length; i++)
        {
            Vector3Int pos = (Vector3Int)(piece.cells[i] + piece.position);
            tilemap.SetTile(pos, piece.data.tile);
        }
    }

    public bool IsValidPosition(Vector2Int position, Vector2Int[] cells)
    {
        int left = -boardSize.x / 2;
        int right = boardSize.x / 2;
        int bottom = -boardSize.y / 2;
        int top = boardSize.y / 2;

        for (int i = 0; i < cells.Length; i++)
        {
            Vector3Int cellPos = (Vector3Int)(cells[i] + position);

            if (cellPos.x < left || cellPos.x >= right ||
                cellPos.y < bottom || cellPos.y >= top)
                return false;

            if (tilemap.HasTile(cellPos))
                return false;
        }

        return true;
    }

    // Returns how many lines were cleared
    public int CheckBoard()
    {
        List<int> clearedLines = new List<int>();

        int left = -boardSize.x / 2;
        int right = boardSize.x / 2;
        int bottom = -boardSize.y / 2;
        int top = boardSize.y / 2;

        for (int y = bottom; y < top; y++)
        {
            bool full = true;

            for (int x = left; x < right; x++)
            {
                if (!tilemap.HasTile(new Vector3Int(x, y, 0)))
                {
                    full = false;
                    break;
                }
            }

            if (full)
                clearedLines.Add(y);
        }

        if (clearedLines.Count == 0)
            return 0;

        // Clear the full lines
        foreach (int y in clearedLines)
        {
            for (int x = left; x < right; x++)
                tilemap.SetTile(new Vector3Int(x, y, 0), null);
        }

        // Drop everything above each cleared line down
        clearedLines.Sort();

        foreach (int clearedY in clearedLines)
        {
            for (int y = clearedY + 1; y < top; y++)
            {
                for (int x = left; x < right; x++)
                {
                    Vector3Int from = new Vector3Int(x, y, 0);
                    Vector3Int to = new Vector3Int(x, y - 1, 0);

                    TileBase tile = tilemap.GetTile(from);
                    tilemap.SetTile(to, tile);
                    tilemap.SetTile(from, null);
                }
            }
        }

        AddScore(clearedLines.Count);
        return clearedLines.Count;
    }

    void AddScore(int linesCleared)
    {
        int points = linesCleared switch
        {
            1 => 100,
            2 => 300,
            3 => 500,
            4 => 800,
            _ => linesCleared * 200
        };

        score += points;
        OnScoreChanged?.Invoke(score);
    }
}
