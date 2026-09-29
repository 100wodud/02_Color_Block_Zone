using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public partial class StageController
{
    private readonly Color _boardTint = new(0.4f, 0.4f, 0.4f, 1f);
    private Dictionary<int, Material> _boardMats;
    private readonly Dictionary<Vector2Int, GameObject> _tiles = new();

    private Material BoardCellMaterial(int colorIndex)
    {
        _boardMats ??= new Dictionary<int, Material>();
        if (!_boardMats.TryGetValue(colorIndex, out var m))
        {
            m = CreateTintedMaterial(palette[colorIndex], _boardTint);
            _boardMats[colorIndex] = m;
        }
        return m;
    }

    private void BuildBoard()
    {
        int size = DefaultKey.BOARD_SIZE;
        var zoneOf = ZoneGenerator.Generate(size, size, DefaultKey.ZONE_COUNT, DefaultKey.ZONE_MIN_SIZE, DefaultKey.ZONE_MAX_SIZE);
        var colors = ZoneGenerator.Paint(ZoneGenerator.Adjacency(zoneOf, DefaultKey.ZONE_COUNT), DefaultKey.COLOR_COUNT);

        float half = (size - 1) * 0.5f;
        var boardBase = boardRoot != null ? boardRoot.position : Vector3.zero;
        var origin = boardBase + new Vector3(-half * _cellSize, 0f, boardZ * _cellSize - half * _cellSize);
        _board = new Board(zoneOf, colors, origin, _cellSize);

        _tiles.Clear();
        foreach (var c in _board.AllCells())
        {
            var zone = _board.Zones[_board.ZoneOf(c)];
            var go = SpawnBrickVisual($"Base_{c.x}_{c.y}", boardRoot, BoardCellMaterial((int)zone.Color), boardTilePrefab);
            go.transform.localPosition = _board.CellToWorld(c) - boardBase;
            go.transform.localScale = Vector3.one * 0.97f;
            _tiles[c] = go;
        }

        if (zoneWalls) AddZoneOutline(boardBase);

        // 칸 사이 틈으로 어두운 바닥이 보이고, 그 바깥을 밝은 테두리가 두른다
        var tiles = TileBounds(_tiles.Values);
        AddPanel(boardRoot, "BoardFloor", tiles, _cellSize * 0.08f, boardFloorColor, 0.001f);
        AddPanel(boardRoot, "BoardBorder", tiles, _cellSize * 0.22f, boardBorderColor, 0.01f);
    }

    // 구역 경계는 색이 아니라 구역 번호로 긋는다 — 구역 색이 바뀌어도 경계는 그대로다
    private void AddZoneOutline(Vector3 boardBase)
    {
        var mat = baseMaterial;
        float thickness = _cellSize * 0.03f;
        float y = 0f;
        float sy = 4f;
        float edge = _cellSize * 0.5f - thickness * 0.5f + 0.01f;
        float lengthScale = cellScale;
        var dirs = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        var placements = new List<Matrix4x4>();
        foreach (var c in _board.AllCells())
        {
            var local = _board.CellToWorld(c) - boardBase;
            foreach (var dir in dirs)
            {
                var n = c + dir;
                bool boundary = !_board.Contains(n) || _board.ZoneOf(n) != _board.ZoneOf(c);
                if (!boundary) continue;

                var pos = new Vector3(local.x + dir.x * edge, y, local.z + dir.y * edge);
                var scale = dir.x != 0
                    ? new Vector3(thickness / _cellSize, sy, lengthScale)
                    : new Vector3(lengthScale, sy, thickness / _cellSize);
                placements.Add(Matrix4x4.TRS(pos, Quaternion.identity, scale));
            }
        }

        CombineInto(boardRoot, "BoardOutline", mat, placements);
    }

    // 구역 색은 판을 만들 때 한 번 칠하고 끝까지 그대로 둔다 — 색은 규칙에 쓰이지 않고 구역을 구분하는 표시라서,
    // 바뀌면 어느 구역이 어디였는지 헷갈리기만 한다. 지워질 때는 바닥 칸만 한 번 튄다
    private void PunchZoneTiles(Board.Zone zone, float delay)
    {
        foreach (var c in zone.Cells)
        {
            if (!_tiles.TryGetValue(c, out var tile)) continue;
            var t = tile.transform;
            t.DOKill();
            t.localScale = Vector3.one * 0.97f;
            t.DOPunchScale(Vector3.one * 0.12f, 0.25f, 6, 0.5f).SetDelay(delay);
        }
    }
}
