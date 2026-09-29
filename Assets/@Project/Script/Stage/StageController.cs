using System.Collections.Generic;
using Areung_Plugin.Core;
using Areung_Plugin.Input;
using Areung_Plugin.SDK;
using UnityEngine;

public enum StageState { NotStarted, Playing, Failed }

// 무한 모드 한 판. 판은 랜덤 구역으로 나뉘고, 구역이 꽉 차면 그 구역만 지워진다.
// 트레이 조각을 하나도 놓을 수 없으면 끝난다.
public partial class StageController : MonoBehaviour
{
    #region Serialized

    [Header("Scene refs")]
    [SerializeField] private Transform boardRoot;
    [SerializeField] private Transform trayRoot;

    [Header("Brick assets")]
    [SerializeField] private GameObject brickPrefab;
    [SerializeField] private GameObject boardTilePrefab;
    [SerializeField] private Material[] palette;
    [SerializeField] private Material[] outlinePalette;
    [SerializeField] private Material baseMaterial;

    [Header("Layout")]
    [SerializeField] private float boardZ = 1.5f;
    // 판 아래 모서리와 트레이 맨 윗줄 사이 거리(칸). 판을 트레이보다 위로 띄우려면 이걸 늘린다 —
    // boardZ 는 트레이·카메라가 같이 따라와서 화면에서는 안 바뀐다
    [SerializeField] private float boardTrayGap = 2.5f;
    [SerializeField] private float cameraTilt = 75f;
    [SerializeField] private float dragLiftCells = 1.5f;
    [SerializeField] private float cellScale = 1.01f;
    // 화면 위쪽 이만큼(비율)은 HUD(점수·BEST·남은 횟수) 자리로 비워 두고 판을 그 아래에 잡는다
    [SerializeField, Range(0f, 0.4f)] private float hudTopReserve = 0.2f;
    // 판과 트레이를 꽉 맞게 잡은 뒤 카메라를 이만큼 더 멀리 잡고, 늘어난 여백은 전부 아래로 몰아서
    // 화면 아래 배너 광고 자리를 만든다. iPhone 13 Pro 에서 배너(홈 인디케이터 포함 화면의 약 12%)를
    // 피하려면 1.11 이상이어야 한다 — 1.15 면 폰 약 14%, iPad 약 13% 가 빈다
    [SerializeField] private float screenZoomOut = 1.15f;

    // 짙은 남색 화면 — 판은 밝은 테두리 + 어두운 바닥, 트레이는 어두운 패널 위 칸
    [Header("Look")]
    [SerializeField] private Color backgroundColor = new(0.05f, 0.07f, 0.26f, 1f);
    [SerializeField] private Color boardBorderColor = new(0.55f, 0.6f, 0.75f, 1f);
    [SerializeField] private Color boardFloorColor = new(0.06f, 0.08f, 0.2f, 1f);
    [SerializeField] private Color trayPanelColor = new(0.1f, 0.14f, 0.36f, 1f);
    [SerializeField] private Color trayCellColor = new(0.24f, 0.28f, 0.42f, 1f);

    // 컨셉 이미지는 모서리가 둥근 캔디 블록이고, 구역 경계 벽이나 조각 위 테두리 막대가 없다
    [SerializeField] private bool roundedBlocks = true;
    [SerializeField, Range(0.05f, 0.45f)] private float blockRoundness = 0.2f;
    [SerializeField] private bool pieceOutlines = false;
    [SerializeField] private bool zoneWalls = false;

    #endregion

    private Camera cam => Initializer.Instance != null ? Initializer.Instance.mainCamera : Camera.main;

    private InputManager _input;
    private Board _board;
    private readonly List<Brick> _bricks = new();
    private GameObject _ghost;
    private float _cellSize = 0.95f;
    private float _brickHeight = 0.5f;
    private readonly Color _outlineTint = new(0.7f, 0.7f, 0.7f, 1f);
    private readonly Color _brickTint = new(1f, 1f, 1f, 1f);

    public StageState State { get; private set; } = StageState.NotStarted;
    public Board Board => _board;
    public Camera ViewCamera => cam;

    public void StartGame()
    {
        _input = GetComponent<InputManager>();
        MeasureBrick();
        BuildBoard();
        BuildTray();
        FrameCamera();
        ResetScore();

        if (_input != null)
        {
            if (cam != null) _input.Camera = cam;
            _input.CanInteract = () => State == StageState.Playing;
        }

        State = StageState.Playing;
        StageEvents.RaiseStageLoaded();
        SpawnTrayIfEmpty();
        
    }

    public void TryPlace(Brick brick) => PlaceBrick(brick);
    public void BeginPreview(Brick brick)
    {
        PlaySelect();
        CreateGhost(brick);
    }
    public void UpdatePreview(Brick brick) => UpdateGhost(brick);
    public void EndPreview() => DestroyGhost();
}
