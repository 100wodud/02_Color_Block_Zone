using System.Collections.Generic;
using UnityEngine;

public partial class StageController
{
    private void CreateGhost(Brick brick)
    {
        DestroyGhost();
        _trayGrid?.Release(brick);

        _ghost = new GameObject("Ghost");
        _ghost.transform.SetParent(boardRoot, false);
        BuildPieceMesh(_ghost.transform, new List<Vector2Int>(brick.LocalCells), GhostMaterial(BrickColor.White));
        _ghostCells = _ghost.transform.Find("Cells");
        _ghostColors = null;
        PaintGhost(brick.CellColors);
        _ghost.SetActive(false);
    }

    private bool _ghostValid;
    private Vector2Int _ghostCell;
    private Transform _ghostCells;
    private List<BrickColor> _ghostColors;
    private Dictionary<BrickColor, Material> _ghostMats;

    // 칸마다 놓으면 바뀔 색을 칠한다. 바뀐 칸만 다시 칠한다 — 드래그 중 매 프레임 불린다
    private void PaintGhost(IReadOnlyList<BrickColor> colors)
    {
        if (_ghostCells == null) return;
        _ghostColors ??= new List<BrickColor>();
        for (int i = 0; i < colors.Count && i < _ghostCells.childCount; i++)
        {
            if (i < _ghostColors.Count && _ghostColors[i] == colors[i]) continue;
            ApplyMaterial(_ghostCells.GetChild(i).gameObject, BrickCellMaterial(GhostMaterial(colors[i])));
            if (i < _ghostColors.Count) _ghostColors[i] = colors[i];
            else _ghostColors.Add(colors[i]);
        }
    }

    private void UpdateGhost(Brick brick)
    {
        if (_ghost == null) return;

        bool overTray = brick.transform.position.z < _trayZoneThreshold;
        Vector3 pos = Vector3.zero;
        bool show = false;
        Vector2Int cell = default;

        bool onBoard = false;

        if (!overTray && TryFindNearestAnchor(brick, out var anchor))
        {
            pos = _board.CellToWorld(anchor);
            show = true;
            onBoard = true;
            cell = anchor;
        }
        else if (overTray && !brick.WasPlaced && TryFindTrayAnchor(brick, out var trayAnchor))
        {
            pos = _trayGrid.CellToWorld(trayAnchor);
            show = true;
        }

        if (onBoard && (!_ghostValid || cell != _ghostCell)) PaintGhost(_board.ZoneColors(brick.CellsAt(cell)));
        else if (!onBoard && _ghostValid) PaintGhost(brick.CellColors);

        if (onBoard)
        {
            if (!_ghostValid || cell != _ghostCell) SafeHaptic();
            _ghostValid = true;
            _ghostCell = cell;
        }
        else
        {
            _ghostValid = false;
        }

        if (show)
        {
            pos.y = _brickHeight;
            _ghost.transform.position = pos;
            _ghost.SetActive(true);
        }
        else
        {
            _ghost.SetActive(false);
        }
    }

    private void DestroyGhost()
    {
        _ghostValid = false;
        if (_ghost != null)
        {
            Destroy(_ghost);
            _ghost = null;
        }
    }

    private Material GhostMaterial(BrickColor color)
    {
        _ghostMats ??= new Dictionary<BrickColor, Material>();
        if (!_ghostMats.TryGetValue(color, out var m)) _ghostMats[color] = m = MakeGhostMaterial(color);
        return m;
    }

    private Material MakeGhostMaterial(BrickColor color)
    {
        var m = new Material(palette[(int)color]);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color;
        c.a = 0.35f;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.color = c;
        return m;
    }
}
