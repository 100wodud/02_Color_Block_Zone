using System.Collections.Generic;
using UnityEngine;

// 무한 모드 보드. 칸마다 소속 구역이 있고, 놓인 조각은 칸 단위로 기억한다 —
// 구역이 지워질 때 조각의 일부만 사라질 수 있어서 조각 단위로는 관리할 수 없다.
public class Board
{
    public class Zone
    {
        public int Id;
        public List<Vector2Int> Cells;
        public BrickColor Color;
    }

    private readonly int[,] _zoneOf;
    private readonly Dictionary<Vector2Int, Brick> _occupied = new();

    public int Width { get; }
    public int Height { get; }
    public Vector3 Origin { get; }
    public float CellSize { get; }
    public IReadOnlyList<Zone> Zones { get; }

    public Board(int[,] zoneOf, BrickColor[] zoneColors, Vector3 origin, float cellSize)
    {
        _zoneOf = zoneOf;
        Width = zoneOf.GetLength(0);
        Height = zoneOf.GetLength(1);
        Origin = origin;
        CellSize = cellSize;

        var zones = new List<Zone>();
        for (int i = 0; i < zoneColors.Length; i++)
            zones.Add(new Zone { Id = i, Cells = new List<Vector2Int>(), Color = zoneColors[i] });
        foreach (var c in AllCells()) zones[_zoneOf[c.x, c.y]].Cells.Add(c);
        Zones = zones;
    }

    public IEnumerable<Vector2Int> AllCells()
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                yield return new Vector2Int(x, y);
    }

    public bool Contains(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Width && c.y < Height;
    public bool IsFree(Vector2Int c) => Contains(c) && !_occupied.ContainsKey(c);
    public int ZoneOf(Vector2Int c) => _zoneOf[c.x, c.y];
    public Brick BrickAt(Vector2Int c) => _occupied.TryGetValue(c, out var b) ? b : null;

    public Vector3 CellToWorld(Vector2Int c) =>
        Origin + new Vector3(c.x * CellSize, 0f, c.y * CellSize);

    public Vector2Int WorldToCell(Vector3 world)
    {
        float fx = (world.x - Origin.x) / CellSize;
        float fz = (world.z - Origin.z) / CellSize;
        return new Vector2Int(Mathf.RoundToInt(fx), Mathf.RoundToInt(fz));
    }

    // 구역은 따지지 않는다 — 어느 구역에나, 여러 구역에 걸쳐서도 놓을 수 있다
    public bool CanPlace(IReadOnlyList<Vector2Int> cells) => CanPlace(cells, null, null);

    // freed: 비어 있다고 치는 칸 (옮기려고 들어 올린 조각 자리), blocked: 찼다고 치는 칸
    public bool CanPlace(IReadOnlyList<Vector2Int> cells, ICollection<Vector2Int> freed, ICollection<Vector2Int> blocked)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            var c = cells[i];
            if (!Contains(c)) return false;
            if (blocked != null && blocked.Contains(c)) return false;
            if (_occupied.ContainsKey(c) && (freed == null || !freed.Contains(c))) return false;
        }
        return true;
    }

    public bool CanFitAnywhere(IReadOnlyList<Vector2Int> localCells) => CanFitAnywhere(localCells, null, null);

    public bool CanFitAnywhere(IReadOnlyList<Vector2Int> localCells, ICollection<Vector2Int> freed, ICollection<Vector2Int> blocked)
    {
        var cells = new Vector2Int[localCells.Count];
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                var anchor = new Vector2Int(x, y);
                for (int i = 0; i < cells.Length; i++) cells[i] = localCells[i] + anchor;
                if (CanPlace(cells, freed, blocked)) return true;
            }
        return false;
    }

    public BrickColor ZoneColorAt(Vector2Int c) => Zones[ZoneOf(c)].Color;

    public List<BrickColor> ZoneColors(IReadOnlyList<Vector2Int> cells)
    {
        var list = new List<BrickColor>(cells.Count);
        for (int i = 0; i < cells.Count; i++) list.Add(ZoneColorAt(cells[i]));
        return list;
    }

    public bool InOneZone(IReadOnlyList<Vector2Int> cells)
    {
        int zone = ZoneOf(cells[0]);
        for (int i = 1; i < cells.Count; i++)
            if (ZoneOf(cells[i]) != zone) return false;
        return true;
    }

    public void Occupy(Brick brick, IReadOnlyList<Vector2Int> cells)
    {
        for (int i = 0; i < cells.Count; i++) _occupied[cells[i]] = brick;
    }

    public void Free(Vector2Int c) => _occupied.Remove(c);

    public void Release(Brick brick)
    {
        var toRemove = new List<Vector2Int>();
        foreach (var kv in _occupied)
            if (kv.Value == brick) toRemove.Add(kv.Key);
        foreach (var c in toRemove) _occupied.Remove(c);
    }

    public bool IsZoneFull(Zone zone)
    {
        foreach (var c in zone.Cells)
            if (!_occupied.ContainsKey(c)) return false;
        return true;
    }

    public int FilledCount(Zone zone)
    {
        int n = 0;
        foreach (var c in zone.Cells)
            if (_occupied.ContainsKey(c)) n++;
        return n;
    }

    public int EmptyCount(Zone zone) => zone.Cells.Count - FilledCount(zone);
}
