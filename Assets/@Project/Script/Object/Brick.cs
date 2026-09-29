using System.Collections.Generic;
using Areung_Plugin.Input;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

// 판에 놓은 뒤에도 다시 집어서 판 안에서 옮길 수 있다. 트레이로는 되돌릴 수 없다.
[DisallowMultipleComponent]
public class Brick : MonoBehaviour, IDragTarget
{
    private const float FollowLerp = 0.5f;

    private StageController _stage;
    private Board _board;
    private Camera _cam;
    private Transform _t;

    private List<Vector2Int> _localCells;
    // LocalCells 와 같은 순서. 트레이에선 전부 흰색이고, 판에 놓으면 칸마다 그 칸의 구역 색이 된다
    private List<BrickColor> _cellColors;
    private Vector3 _trayHome;
    private float _placedY;
    private float _liftY;

    private Vector3 _grabOffset;
    private bool _held;

    public IReadOnlyList<BrickColor> CellColors => _cellColors;

    // 마지막으로 놓을 때 조각 전체가 한 구역 안에 들어갔나 — 이 칸들이 보너스(맞춤 칸)를 받는다
    public bool InOneZone { get; private set; }
    public bool Placed { get; private set; }
    public Vector2Int PlacedAnchor { get; private set; }
    public Vector2Int TrayAnchor { get; set; }
    public bool WasPlaced { get; private set; }
    public Vector2Int PrevBoardAnchor { get; private set; }

    // 배치 점수는 트레이에서 판으로 처음 놓을 때 한 번만 준다 — 판과 트레이를 오가며 점수를 쌓지 못하게
    public bool Scored { get; set; }
    public IReadOnlyList<Vector2Int> LocalCells => _localCells;

    public void Setup(StageController stage, Board board, Camera cam, List<Vector2Int> localCells,
        BrickColor color, Vector3 trayHome, float placedY, float liftY)
    {
        _stage = stage;
        _board = board;
        _cam = cam;
        _localCells = localCells;
        _cellColors = new List<BrickColor>(localCells.Count);
        for (int i = 0; i < localCells.Count; i++) _cellColors.Add(color);
        _trayHome = trayHome;
        _placedY = placedY;
        _liftY = liftY;
        _t = transform;
        _t.position = trayHome;
    }

    public List<Vector2Int> CellsAt(Vector2Int anchor)
    {
        var list = new List<Vector2Int>(_localCells.Count);
        for (int i = 0; i < _localCells.Count; i++) list.Add(_localCells[i] + anchor);
        return list;
    }

    public Vector2Int CurrentAnchor() => _board.WorldToCell(_t.position);

    public void OnSelect(Vector3 worldPos)
    {
        _held = true;
        _t.DOKill();
        _t.localScale = Vector3.one;

        WasPlaced = Placed;
        if (Placed)
        {
            PrevBoardAnchor = PlacedAnchor;
            _board.Release(this);
            Placed = false;
        }

        var g = PointerGround(_placedY);
        _grabOffset = new Vector3(_t.position.x - g.x, 0f, _t.position.z - g.z);
        var p = _t.position; p.y = _liftY; _t.position = p;

        _stage.BeginPreview(this);
        StageEvents.RaiseBrickPicked(this);
    }

    public void OnMove(Vector3 worldPos)
    {
        if (!_held) return;
        var g = PointerGround(_placedY);
        var target = new Vector3(g.x + _grabOffset.x, _liftY, g.z + _grabOffset.z);
        _t.position = Vector3.Lerp(_t.position, target, FollowLerp);
        _stage.UpdatePreview(this);
    }

    public void OnDeselect(Vector3 worldPos)
    {
        if (!_held) return;
        _held = false;
        _stage.TryPlace(this);
    }

    public void PlaceAt(Vector2Int anchor)
    {
        Placed = true;
        PlacedAnchor = anchor;
        Vector3 world = _board.CellToWorld(anchor);
        world.y = _placedY;
        _t.DOKill();
        _t.DOMove(world, 0.12f).SetEase(Ease.OutQuad);
    }

    // 구역이 지워지면 연출 전에 제자리로 붙인다 — 이동 중에 칸을 떼어내면 엉뚱한 곳에서 터진다
    public void SnapToPlaced()
    {
        _t.DOKill();
        Vector3 world = _board.CellToWorld(PlacedAnchor);
        world.y = _placedY;
        _t.position = world;
        _t.localScale = Vector3.one;
    }

    public void SetLocalCells(List<Vector2Int> cells, List<BrickColor> colors)
    {
        _localCells = cells;
        _cellColors = colors;
    }

    public void SetCellColors(List<BrickColor> colors, bool inOneZone)
    {
        _cellColors = colors;
        InOneZone = inOneZone;
    }

    // 한 구역 안에 딱 맞게 넣었을 때 — 내려앉는 이동(0.12초)이 끝난 뒤 살짝 줄었다 돌아온다.
    // 조각 원점이 첫 칸 모서리라 그냥 크기만 바꾸면 한쪽으로 쏠린다. 조각 가운데를 기준으로 줄인다
    public void PlayFitSquash(float cellSize)
    {
        var c = PieceGenerator.Center(_localCells) * cellSize;
        var center = new Vector3(c.x, 0f, c.y);
        Vector3 home = _board.CellToWorld(PlacedAnchor);
        home.y = _placedY;

        const float min = 0.88f;
        DOTween.Sequence()
            .AppendInterval(0.12f)
            .Append(DOVirtual.Float(1f, min, 0.08f, s => ScaleAround(home, center, s)).SetEase(Ease.OutQuad))
            .Append(DOVirtual.Float(min, 1f, 0.22f, s => ScaleAround(home, center, s)).SetEase(Ease.OutBack))
            .SetTarget(_t);
    }

    private void ScaleAround(Vector3 home, Vector3 center, float s)
    {
        _t.localScale = Vector3.one * s;
        _t.position = home + center * (1f - s);
    }

    public void MoveToTray(Vector3 home)
    {
        Placed = false;
        _trayHome = home;
        _t.DOKill();
        _t.DOMove(home, 0.2f).SetEase(Ease.OutBack);
        _t.DOScale(1f, 0.2f);
    }

    public void PlaySpawn(float dropHeight, float delay)
    {
        _t.DOKill();
        _t.position = _trayHome + Vector3.up * dropHeight;
        _t.localScale = Vector3.one * 0.5f;
        _t.DOMove(_trayHome, 0.45f).SetDelay(delay).SetEase(Ease.OutBounce);
        _t.DOScale(1f, 0.35f).SetDelay(delay).SetEase(Ease.OutBack);
    }

    private Vector3 PointerGround(float planeY)
    {
        if (_cam == null) return _t.position;
        Vector2 screen = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
        var ray = _cam.ScreenPointToRay(screen);
        var plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
        return plane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : _t.position;
    }
}
