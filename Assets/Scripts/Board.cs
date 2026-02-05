using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Board : MonoBehaviour
{
    // tetris is fun, and is the 2nd overall bestselling game of all time :D
    // list of all piece defs (shape + tile) that can spawn
    public TetrominoData[] tetrominos;

    // where we draw the blocks
    public Tilemap tilemap;

    // board dims (10x20 classic)
    public Vector2Int boardSize = new Vector2Int(10, 20);

    // prefab for the falling piece object
    public Piece piecePrefab;

    // score is just tracked here and ui listens to the event
    public int score;
    public event Action<int> OnScoreChanged;

    // i wanted a "puzzle start" board so i can show the p piece has a purpose
    public bool usePresetBoard = true;

    // i kept rng as a fallback, but for the assignment i needed a repeatable demo
    // so i added a set spawn order for pieces (makes testing way easier too)
    public bool useSequence = true;
    public bool loopSequence = true;

    // this is the exact spawn order i need for my scenario
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
    // I had a hard time setting up the board with my pieces, so I just opted for one big color, this is just for testing purposes and the sake of
    // this is the board i start with:
    // '#' means there is already a locked block there
    // '.' means empty
    // rows go top -> bottom (20 rows)
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
        // load the preset first so the player spawns into the puzzle layout
        if (usePresetBoard)
            LoadPresetBoard(PRESET_BOARD);

        // then spawn the first piece
        SpawnPiece();

        // push score to ui at start
        OnScoreChanged?.Invoke(score);
    }

    private Tetromino GetNextTetromino()
    {
        // if we're using a scriptable order, return the next one
        // i did this bc rng kept not giving me p when i needed it for the demo
        if (useSequence && sequence != null && sequence.Length > 0)
        {
            Tetromino t = sequence[sequenceIndex];

            sequenceIndex++;

            // loop back to the start so it doesnt run out
            // if loopSequence is off, it just keeps spawning the last one
            if (sequenceIndex >= sequence.Length)
                sequenceIndex = loopSequence ? 0 : sequence.Length - 1;

            return t;
        }

        // fallback rng if i turn sequencing off
        return tetrominos[UnityEngine.Random.Range(0, tetrominos.Length)].tetromino;
    }

    public void SpawnPiece()
    {
        // make a new falling piece object
        Piece piece = Instantiate(piecePrefab, transform);

        // choose what spawns next (sequence or rng)
        Tetromino chosen = GetNextTetromino();
        piece.Initialize(this, chosen);

        // spawn near the top.
        // bug i hit earlier: p is taller than normal pieces (it has a cell at y=2),
        // so my old "top - 2" spawn put part of it out of bounds and instant game over.
        // this fixes it by measuring the piece height and placing it so it fits.
        int top = boardSize.y / 2;

        int maxCellY = 0;
        for (int i = 0; i < piece.cells.Length; i++)
            maxCellY = Mathf.Max(maxCellY, piece.cells[i].y);

        // set y so the highest block lands on top-1 (still inside the board)
        piece.position = new Vector2Int(0, (top - 1) - maxCellY);
        piece.ResetStepTimer();

        // if spawn is blocked, we just end the game right away
        // (either out of bounds or colliding with an existing tile)
        if (!IsValidPosition(piece.position, piece.cells))
        {
            Debug.Log("GAME OVER (spawn blocked)");
            Destroy(piece.gameObject);
            return;
        }

        // draw it on the tilemap
        Set(piece);
    }

    private void LoadPresetBoard(string[] rowsTopToBottom)
    {
        // safety checks
        if (rowsTopToBottom == null || rowsTopToBottom.Length == 0)
            return;

        // clear anything that was there from last play
        tilemap.ClearAllTiles();

        int width = boardSize.x;
        int height = boardSize.y;

        // only load as many rows as we can fit
        int rowsToLoad = Mathf.Min(height, rowsTopToBottom.Length);

        for (int r = 0; r < rowsToLoad; r++)
        {
            string row = rowsTopToBottom[r];
            if (string.IsNullOrEmpty(row)) continue;

            int colsToLoad = Mathf.Min(width, row.Length);

            for (int c = 0; c < colsToLoad; c++)
            {
                // only place tiles where there is a '#'
                if (row[c] != '#') continue;

                // map the text grid into our centered board coords
                // left edge is -width/2, right edge is width/2 - 1
                int x = -width / 2 + c;

                // top row of the string should become y = top-1 in the board
                int y = (height / 2 - 1) - r;

                tilemap.SetTile(new Vector3Int(x, y, 0), GetLockedTile());
            }
        }
    }

    private TileBase GetLockedTile()
    {
        // i didnt want to add another inspector field just for "locked" tiles,
        // so i just reuse the first tile in the tetromino list for the preset blocks.
        // (they're just obstacles anyway, color doesnt matter for the mechanic)
        if (tetrominos != null && tetrominos.Length > 0)
            return tetrominos[0].tile;

        return null;
    }

    public void Clear(Piece piece)
    {
        // erase the current falling piece so we can redraw it after movement/rotation
        for (int i = 0; i < piece.cells.Length; i++)
        {
            Vector3Int pos = (Vector3Int)(piece.cells[i] + piece.position);
            tilemap.SetTile(pos, null);
        }
    }

    public void Set(Piece piece)
    {
        // draw the falling piece at its current position
        for (int i = 0; i < piece.cells.Length; i++)
        {
            Vector3Int pos = (Vector3Int)(piece.cells[i] + piece.position);
            tilemap.SetTile(pos, piece.data.tile);
        }
    }

    public bool IsValidPosition(Vector2Int position, Vector2Int[] cells)
    {
        // board bounds in the same coord system as the tilemap
        int left = -boardSize.x / 2;
        int right = boardSize.x / 2;
        int bottom = -boardSize.y / 2;
        int top = boardSize.y / 2;

        for (int i = 0; i < cells.Length; i++)
        {
            Vector3Int cellPos = (Vector3Int)(cells[i] + position);

            // out of bounds = invalid move/rotation
            if (cellPos.x < left || cellPos.x >= right ||
                cellPos.y < bottom || cellPos.y >= top)
                return false;

            // collision with any existing tile = invalid
            if (tilemap.HasTile(cellPos))
                return false;
        }

        return true;
    }

    public int CheckBoard()
    {
        // scan the board for full rows, clear them, then drop stuff down
        // i kept it tilemap-based so i dont have to maintain a separate grid array too
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

        // clear the full rows
        foreach (int y in clearedLines)
        {
            for (int x = left; x < right; x++)
                tilemap.SetTile(new Vector3Int(x, y, 0), null);
        }

        // this drops everything above each cleared row down by 1
        // sort is important so we drop in a consistent direction
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
        // basic line clear scoring (no levels yet)
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
