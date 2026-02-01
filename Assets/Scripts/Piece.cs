using UnityEngine;

public class Piece : MonoBehaviour
{
    // the piece data we pulled from the board's tetromino list (cells + tile)
    public TetrominoData data;

    // reference back to the board so we can ask "is this position valid" and set tiles
    public Board board;

    // local cell coords for this piece (relative to position)
    public Vector2Int[] cells;

    // board position of the piece (added to each cell when drawing/checking)
    public Vector2Int position;

    // simple gravity timer: every stepDelay seconds we try to move down by 1
    public float stepDelay = 1.0f;
    private float stepTime;

    // rotation state for srs (0 spawn, 1 right, 2 back, 3 left)
    // needed so wall kicks can use the correct table row
    private int rotationState;

    public void Initialize(Board board, Tetromino tetromino)
    {
        // store board ref
        this.board = board;

        // find the matching tetromino data in the board array
        // (so the piece can get its shape + tile)
        for (int i = 0; i < board.tetrominos.Length; i++)
        {
            if (board.tetrominos[i].tetromino == tetromino)
            {
                data = board.tetrominos[i];
                break;
            }
        }

        // copy the cell list into our own array so we can rotate it safely
        // (rotating modifies cells, so i dont want to mutate the original data)
        cells = new Vector2Int[data.cells.Length];
        for (int i = 0; i < data.cells.Length; i++)
            cells[i] = data.cells[i];

        // board decides the actual spawn pos, but we default to zero here
        position = Vector2Int.zero;

        // reset rotation state on spawn
        rotationState = 0;

        // start gravity timer
        ResetStepTimer();
    }

    public void ResetStepTimer()
    {
        // next time we should auto-fall
        stepTime = Time.time + stepDelay;
    }

    private void Update()
    {
        // we erase the piece first, then apply movement/rotation, then redraw it
        // if we dont clear first, we'd leave behind ghost tiles
        board.Clear(this);

        // --- input ---
        Vector2Int move = Vector2Int.zero;

        // i kept it simple: a/d for left/right, s for soft drop
        if (Input.GetKeyDown(KeyCode.A))
            move = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D))
            move = Vector2Int.right;
        else if (Input.GetKeyDown(KeyCode.S))
            move = Vector2Int.down;

        // q/e for rotate (ccw/cw)
        // earlier issue: rotating near walls felt bad, so i added wall kicks (srs)
        if (Input.GetKeyDown(KeyCode.E))
            Rotate(+1);
        else if (Input.GetKeyDown(KeyCode.Q))
            Rotate(-1);

        // apply manual move (if any)
        if (move != Vector2Int.zero)
        {
            bool locked = Move(move);
            if (locked) return; // we destroyed the piece, so bail out
        }

        // --- gravity ---
        // every stepDelay seconds, try to move down by 1
        if (Time.time >= stepTime)
        {
            stepTime = Time.time + stepDelay;

            bool locked = Move(Vector2Int.down);
            if (locked) return;
        }

        // redraw the piece in its final spot for this frame
        board.Set(this);
    }

    // returns true if the move caused a lock (so the piece got destroyed)
    bool Move(Vector2Int translation)
    {
        Vector2Int newPos = position + translation;

        // if the board says its valid, just move there
        if (board.IsValidPosition(newPos, cells))
        {
            position = newPos;

            // small feel thing: if you soft drop, reset gravity so it doesnt "double tick"
            // (without this, pressing s right before a gravity tick can feel like it skips)
            if (translation == Vector2Int.down)
                ResetStepTimer();

            return false;
        }

        // if we cant move down, that means we've landed -> lock the piece
        // this was one of the main things i had to keep stable when adding new shapes
        if (translation == Vector2Int.down)
        {
            board.Set(this);         // commit piece to the board
            board.CheckBoard();      // clear lines + score
            board.SpawnPiece();      // spawn the next one
            Destroy(gameObject);     // remove this falling piece object
            return true;
        }

        // sideways move failed -> just ignore it
        return false;
    }

    // rotate with srs style wall kicks
    void Rotate(int direction) // +1 = cw, -1 = ccw
    {
        int from = rotationState;
        int to = Wrap(rotationState + direction, 4);

        // build a rotated copy first so we can test it without committing
        Vector2Int[] rotatedCells = new Vector2Int[cells.Length];

        for (int i = 0; i < cells.Length; i++)
        {
            Vector2Int c = cells[i];

            // math rotate around origin:
            // cw: (x,y) -> (y, -x)
            // ccw: (x,y) -> (-y, x)
            rotatedCells[i] = (direction > 0)
                ? new Vector2Int(c.y, -c.x)
                : new Vector2Int(-c.y, c.x);
        }

        // try the 5 kick offsets from the table
        // real problem i hit: without kicks, rotation fails a ton near walls / stacks
        // with kicks, it feels more like actual tetris behavior
        for (int attempt = 0; attempt < 5; attempt++)
        {
            Vector2Int kick = WallKicks.GetKick(data.tetromino, from, to, attempt);
            Vector2Int testPos = position + kick;

            // if the rotated shape fits at this kicked position, accept it
            if (board.IsValidPosition(testPos, rotatedCells))
            {
                cells = rotatedCells;
                position = testPos;
                rotationState = to;
                return;
            }
        }

        // if none of the kicks worked, rotation just fails (keep old cells)
    }

    // keeps rotation state in range 0..3
    // (so turning left from 0 becomes 3, etc)
    int Wrap(int value, int max)
    {
        if (value < 0) return value + max;
        if (value >= max) return value - max;
        return value;
    }
}
