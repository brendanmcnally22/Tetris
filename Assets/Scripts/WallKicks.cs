using UnityEngine;

public static class WallKicks
{
    // this file is just the srs "wall kick" offsets.
    // idea: when you rotate and it would collide, you try a few tiny nudges
    // and if any nudge makes the rotated shape fit, the rotation is allowed.

    // rotation states:
    // 0 = spawn, 1 = right, 2 = 180, 3 = left

    // kick table for j/l/s/t/z (and in our case also p, since we treat it like jltsz)
    // each row is one rotation change (ex: 0->1) and it contains 5 attempts
    // attempt 0 is always (0,0) aka "try rotate in place first"
    private static readonly Vector2Int[,] JLSTZ = new Vector2Int[,]
    {
        // 0->1 (cw)
        { new(0,0), new(-1,0), new(-1, 1), new(0,-2), new(-1,-2) },
        // 1->0 (ccw)
        { new(0,0), new(1,0),  new(1,-1),  new(0, 2), new(1, 2)  },

        // 1->2
        { new(0,0), new(1,0),  new(1,-1),  new(0, 2), new(1, 2)  },
        // 2->1
        { new(0,0), new(-1,0), new(-1, 1), new(0,-2), new(-1,-2) },

        // 2->3
        { new(0,0), new(1,0),  new(1, 1),  new(0,-2), new(1,-2) },
        // 3->2
        { new(0,0), new(-1,0), new(-1,-1), new(0, 2), new(-1, 2) },

        // 3->0
        { new(0,0), new(-1,0), new(-1,-1), new(0, 2), new(-1, 2) },
        // 0->3
        { new(0,0), new(1,0),  new(1, 1),  new(0,-2), new(1,-2) },
    };

    // i piece has its own kick table (its long so it needs diff offsets)
    private static readonly Vector2Int[,] I = new Vector2Int[,]
    {
        // 0->1
        { new(0,0), new(-2,0), new(1,0),  new(-2,-1), new(1, 2) },
        // 1->0
        { new(0,0), new(2,0),  new(-1,0), new(2, 1),  new(-1,-2) },

        // 1->2
        { new(0,0), new(-1,0), new(2,0),  new(-1, 2), new(2,-1) },
        // 2->1
        { new(0,0), new(1,0),  new(-2,0), new(1,-2),  new(-2, 1) },

        // 2->3
        { new(0,0), new(2,0),  new(-1,0), new(2, 1),  new(-1,-2) },
        // 3->2
        { new(0,0), new(-2,0), new(1,0),  new(-2,-1), new(1, 2) },

        // 3->0
        { new(0,0), new(1,0),  new(-2,0), new(1,-2),  new(-2, 1) },
        // 0->3
        { new(0,0), new(-1,0), new(2,0),  new(-1, 2), new(2,-1) },
    };

    // the tables are stored as rows in this order:
    // 0->1, 1->0, 1->2, 2->1, 2->3, 3->2, 3->0, 0->3
    // so this function just maps (from,to) into the correct row index.
    private static int GetIndex(int from, int to)
    {
        if (from == 0 && to == 1) return 0;
        if (from == 1 && to == 0) return 1;
        if (from == 1 && to == 2) return 2;
        if (from == 2 && to == 1) return 3;
        if (from == 2 && to == 3) return 4;
        if (from == 3 && to == 2) return 5;
        if (from == 3 && to == 0) return 6;
        if (from == 0 && to == 3) return 7;

        // shouldnt really happen, but returning 0 keeps it from crashing
        return 0;
    }

    public static Vector2Int GetKick(Tetromino t, int fromState, int toState, int attempt)
    {
        // o doesnt need kicks bc it rotates in place (square)
        if (t == Tetromino.O) return Vector2Int.zero;

        // pick which row of the kick table we need for this rotation change
        int row = GetIndex(fromState, toState);

        // i has its own table, everything else uses the jltsz table
        // (p uses this too in our project, since its not i or o)
        var table = (t == Tetromino.I) ? I : JLSTZ;

        // attempt is 0..4 (piece.cs loops 5 times)
        return table[row, attempt];
    }
}
