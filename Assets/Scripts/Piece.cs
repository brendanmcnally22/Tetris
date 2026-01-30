using UnityEngine;

public class Piece : MonoBehaviour
{
    public TetrominoData data;
    public Board board;
    public Vector2Int[] cells;
    public Vector2Int position;

    [Header("Fall Settings")]
    public float stepDelay = 1.0f; // seconds per auto-fall tick (tune this)
    private float stepTime;

    // --- NEW: SRS rotation state (0,1,2,3) ---
    private int rotationState;

    public void Initialize(Board board, Tetromino tetromino)
    {
        this.board = board;

        for (int i = 0; i < board.tetrominos.Length; i++)
        {
            if (board.tetrominos[i].tetromino == tetromino)
            {
                data = board.tetrominos[i];
                break;
            }
        }

        cells = new Vector2Int[data.cells.Length];
        for (int i = 0; i < data.cells.Length; i++)
            cells[i] = data.cells[i];

        position = Vector2Int.zero;

        // --- NEW: reset rotation state on spawn ---
        rotationState = 0;

        ResetStepTimer();
    }

    public void ResetStepTimer()
    {
        stepTime = Time.time + stepDelay;
    }

    private void Update()
    {
        // Clear current drawn tiles
        board.Clear(this);

        // ----- INPUT -----
        Vector2Int move = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.A))
            move = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D))
            move = Vector2Int.right;
        else if (Input.GetKeyDown(KeyCode.S))
            move = Vector2Int.down;

        // --- UPDATED: rotation with direction + wall kicks ---
        if (Input.GetKeyDown(KeyCode.E))
            Rotate(+1); // clockwise
        else if (Input.GetKeyDown(KeyCode.Q))
            Rotate(-1); // counterclockwise

        // Apply manual move (if any)
        if (move != Vector2Int.zero)
        {
            bool locked = Move(move);
            if (locked) return; // piece destroyed, stop Update
        }

        // ----- GRAVITY -----
        if (Time.time >= stepTime)
        {
            stepTime = Time.time + stepDelay;

            bool locked = Move(Vector2Int.down);
            if (locked) return;
        }

        // Draw piece in new spot
        board.Set(this);
    }

    // Returns true if the piece locked (and got destroyed)
    bool Move(Vector2Int translation)
    {
        Vector2Int newPos = position + translation;

        if (board.IsValidPosition(newPos, cells))
        {
            position = newPos;

            // If you moved down manually, prevent “double-fall” feeling
            if (translation == Vector2Int.down)
                ResetStepTimer();

            return false;
        }

        // If we failed to move DOWN, we lock
        if (translation == Vector2Int.down)
        {
            board.Set(this);         // commit it
            board.CheckBoard();      // clear lines + score
            board.SpawnPiece();      // new piece
            Destroy(gameObject);     // delete this piece
            return true;
        }

        return false;
    }

    // --- UPDATED: rotate with SRS wall kicks ---
    void Rotate(int direction) // +1 CW, -1 CCW
    {
        int from = rotationState;
        int to = Wrap(rotationState + direction, 4);

        // Make rotated copy
        Vector2Int[] rotatedCells = new Vector2Int[cells.Length];

        for (int i = 0; i < cells.Length; i++)
        {
            Vector2Int c = cells[i];

            // CW: (x,y)->(y,-x), CCW: (x,y)->(-y,x)
            rotatedCells[i] = (direction > 0)
                ? new Vector2Int(c.y, -c.x)
                : new Vector2Int(-c.y, c.x);
        }

        // Try SRS wall kicks (5 attempts)
        for (int attempt = 0; attempt < 5; attempt++)
        {
            Vector2Int kick = WallKicks.GetKick(data.tetromino, from, to, attempt);
            Vector2Int testPos = position + kick;

            if (board.IsValidPosition(testPos, rotatedCells))
            {
                cells = rotatedCells;
                position = testPos;
                rotationState = to;
                return;
            }
        }

        // If none worked, rotation fails (do nothing)
    }

    // --- NEW: helper to wrap rotation state 0..3 ---
    int Wrap(int value, int max)
    {
        if (value < 0) return value + max;
        if (value >= max) return value - max;
        return value;
    }
}
