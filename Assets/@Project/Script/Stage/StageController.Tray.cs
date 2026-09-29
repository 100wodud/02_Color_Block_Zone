using System.Collections.Generic;
using UnityEngine;

public partial class StageController
{
    private TrayGrid _trayGrid;
    private float _trayZoneThreshold;
    private float _placedY;
    private float _liftY;
    private int _pieceSerial;

    // 한 번에 뽑는 조각 셋 중 하나도 판에 안 들어가면 바로 끝나 버린다 — 몇 번 다시 뽑아 본다
    private const int SpawnRerolls = 8;

    private void BuildTray()
    {
        _bricks.Clear();
        _placedY = _brickHeight;
        _liftY = _placedY + _cellSize * dragLiftCells;

        int cols = DefaultKey.TRAY_COLS;
        int rows = DefaultKey.TRAY_ROWS;

        float boardBottom = _board.CellToWorld(Vector2Int.zero).z - _cellSize * 0.5f;
        float topRowZ = boardBottom - _cellSize * boardTrayGap;
        var origin = new Vector3(-(cols - 1) * 0.5f * _cellSize, _placedY, topRowZ - (rows - 1) * _cellSize);
        _trayGrid = new TrayGrid(cols, rows, origin, _cellSize);
        _trayZoneThreshold = boardBottom - _cellSize * 0.5f;

        var gridRoot = new GameObject("TrayGrid").transform;
        gridRoot.SetParent(trayRoot, false);
        var cellMat = CreateColoredMaterial(baseMaterial, trayCellColor);
        var cells = new List<GameObject>();
        for (int col = 0; col < cols; col++)
            for (int row = 0; row < rows; row++)
            {
                var tile = SpawnBrickVisual($"TrayCell_{col}_{row}", gridRoot, cellMat);
                cells.Add(tile);
                var pos = _trayGrid.CellToWorld(new Vector2Int(col, row));
                pos.y = 0f;
                tile.transform.position = pos;
                tile.transform.localScale = Vector3.one * 0.97f;
            }

        AddPanel(gridRoot, "TrayPanel", TileBounds(cells), _cellSize * 0.35f, trayPanelColor, 0.001f);
    }

    // 트레이가 완전히 비었을 때만 새 조각을 채운다
    private void SpawnTrayIfEmpty()
    {
        if (State != StageState.Playing || !_trayGrid.IsEmpty) return;

        List<(List<Vector2Int> cells, Vector2Int anchor)> packed = null;
        for (int roll = 0; roll <= SpawnRerolls; roll++)
        {
            var shapes = new List<List<Vector2Int>>();
            bool anyFits = false;
            for (int i = 0; i < DefaultKey.TRAY_SPAWN_COUNT; i++)
            {
                var shape = PieceGenerator.RandomShape();
                shapes.Add(shape);
                if (_board.CanFitAnywhere(shape)) anyFits = true;
            }

            var result = TrayPacker.Pack(shapes, _trayGrid.Cols, _trayGrid.Rows);
            if (result == null) continue;
            packed = result;
            if (anyFits) break;
        }
        if (packed == null) return;

        float drop = _cellSize * (dragLiftCells + 2f);
        for (int i = 0; i < packed.Count; i++)
        {
            var (cells, anchor) = packed[i];
            var brick = CreateBrick(cells, BrickColor.White, anchor);
            brick.PlaySpawn(drop, i * 0.06f);
        }
    }

    private Brick CreateBrick(List<Vector2Int> local, BrickColor color, Vector2Int trayAnchor)
    {
        var root = new GameObject($"Piece_{_pieceSerial++}_{color}");
        root.transform.SetParent(trayRoot, false);
        AddPieceColliders(root, local);

        var brick = root.AddComponent<Brick>();
        brick.Setup(this, _board, cam, local, color, _trayGrid.CellToWorld(trayAnchor), _placedY, _liftY);
        brick.TrayAnchor = trayAnchor;
        BuildPieceVisual(root.transform, local, brick.CellColors);
        _trayGrid.Occupy(brick, brick.CellsAt(trayAnchor));
        _bricks.Add(brick);
        return brick;
    }

    private void AddPieceColliders(GameObject root, IReadOnlyList<Vector2Int> local)
    {
        foreach (var box in root.GetComponents<BoxCollider>()) Destroy(box);
        foreach (var lc in local)
        {
            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(lc.x * _cellSize, _brickHeight * 0.5f, lc.y * _cellSize);
            box.size = new Vector3(_cellSize * 1.05f, _brickHeight * 1.4f, _cellSize * 1.05f);
        }
    }

    private bool TryFindTrayAnchor(Brick brick, out Vector2Int best)
    {
        best = brick.TrayAnchor;
        var pos = brick.transform.position;
        var xz = new Vector2(pos.x, pos.z);
        float bestDist = float.MaxValue;
        bool found = false;

        for (int col = 0; col < _trayGrid.Cols; col++)
            for (int row = 0; row < _trayGrid.Rows; row++)
            {
                var cand = new Vector2Int(col, row);
                if (!_trayGrid.CanPlace(brick.CellsAt(cand))) continue;

                var world = _trayGrid.CellToWorld(cand);
                float d = (new Vector2(world.x, world.z) - xz).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = cand;
                    found = true;
                }
            }

        return found;
    }

    private IEnumerable<Brick> TrayBricks()
    {
        foreach (var b in _bricks)
            if (b != null && !b.Placed) yield return b;
    }

    private IEnumerable<Brick> BoardBricks()
    {
        foreach (var b in _bricks)
            if (b != null && b.Placed) yield return b;
    }
}
