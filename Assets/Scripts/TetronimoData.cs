using System;
using UnityEngine;
using UnityEngine.Tilemaps;

//where i store all the tetris data :D 
public enum Tetromino { I, O, T, L, J, S, Z, P }

[Serializable]
public struct TetrominoData
{
    public Tetromino tetromino;
    public Vector2Int[] cells;
    public Tile tile;
}
