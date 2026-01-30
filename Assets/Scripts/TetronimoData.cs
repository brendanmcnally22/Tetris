using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum Tetromino { I, O, T, L, J, S, Z, P }

[Serializable]
public struct TetrominoData
{
    public Tetromino tetromino;
    public Vector2Int[] cells;
    public Tile tile;
}
