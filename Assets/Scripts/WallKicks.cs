using UnityEngine;

public static class WallKicks
{
    // Rotation states (SRS):
    // 0 = spawn, 1 = right, 2 = 180, 3 = left

    // JLSTZ kick table (5 tests each)
    private static readonly Vector2Int[,] JLSTZ = new Vector2Int[,]
    {
        // 0->1
        { new(0,0), new(-1,0), new(-1, 1), new(0,-2), new(-1,-2) },
        // 1->0
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

    // I kick table (5 tests each)
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

    // Order in our tables:
    // 0->1, 1->0, 1->2, 2->1, 2->3, 3->2, 3->0, 0->3
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
        return 0;
    }

    public static Vector2Int GetKick(Tetromino t, int fromState, int toState, int attempt)
    {
        // O doesn't need kicks (it rotates “in place”)
        if (t == Tetromino.O) return Vector2Int.zero;

        int row = GetIndex(fromState, toState);

        var table = (t == Tetromino.I) ? I : JLSTZ;
        return table[row, attempt];
    }
}
