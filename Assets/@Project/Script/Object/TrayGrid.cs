using System.Collections.Generic;
using UnityEngine;

// 판 아래 보관 칸. 새 조각이 여기 나온다. 트레이 안에서 자리를 바꿀 수는 있지만 판 조각을 되돌릴 수는 없다.
public class TrayGrid
{
    private readonly Dictionary<Vector2Int, Brick> _occupied = new();

    public int Cols { get; }
    public int Rows { get; }
    public Vector3 Origin { get; }
    public float CellSize { get; }
    public bool IsEmpty => _occupied.Count == 0;

    public TrayGrid(int cols, int rows, Vector3 origin, float cellSize)
    {
        Cols = cols;
        Rows = rows;
        Origin = origin;
        CellSize = cellSize;
    }

    public bool Contains(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Cols && c.y < Rows;
    public bool IsFree(Vector2Int c) => Contains(c) && !_occupied.ContainsKey(c);

    public Vector3 CellToWorld(Vector2Int c) =>
        Origin + new Vector3(c.x * CellSize, 0f, c.y * CellSize);

    public Vector2Int WorldToCell(Vector3 world)
    {
        float fx = (world.x - Origin.x) / CellSize;
        float fz = (world.z - Origin.z) / CellSize;
        return new Vector2Int(Mathf.RoundToInt(fx), Mathf.RoundToInt(fz));
    }

    public bool CanPlace(IReadOnlyList<Vector2Int> cells)
    {
        for (int i = 0; i < cells.Count; i++)
            if (!IsFree(cells[i])) return false;
        return true;
    }

    public bool CanFitAnywhere(IReadOnlyList<Vector2Int> localCells)
    {
        var cells = new Vector2Int[localCells.Count];
        for (int x = 0; x < Cols; x++)
            for (int y = 0; y < Rows; y++)
            {
                var anchor = new Vector2Int(x, y);
                for (int i = 0; i < cells.Length; i++) cells[i] = localCells[i] + anchor;
                if (CanPlace(cells)) return true;
            }
        return false;
    }

    public void Occupy(Brick brick, IReadOnlyList<Vector2Int> cells)
    {
        for (int i = 0; i < cells.Count; i++) _occupied[cells[i]] = brick;
    }

    public void Release(Brick brick)
    {
        var toRemove = new List<Vector2Int>();
        foreach (var kv in _occupied)
            if (kv.Value == brick) toRemove.Add(kv.Key);
        foreach (var c in toRemove) _occupied.Remove(c);
    }
}
