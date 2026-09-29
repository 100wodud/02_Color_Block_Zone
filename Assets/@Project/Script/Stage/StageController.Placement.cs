using System.Collections.Generic;
using Areung_Plugin.Settings;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public partial class StageController
{
    private int _revivesUsed;

    public bool CanRevive => State == StageState.Failed && _revivesUsed < DefaultKey.REVIVE_PER_GAME;

    private void PlaceBrick(Brick brick)
    {
        DestroyGhost();

        if (State != StageState.Playing)
        {
            RestorePrev(brick);
            return;
        }

        bool overTray = brick.transform.position.z < _trayZoneThreshold;

        if (!overTray && TryFindNearestAnchor(brick, out var anchor))
        {
            var cells = brick.CellsAt(anchor);
            // 집었다가 같은 자리에 내려놓으면 횟수를 쓰지 않는다
            if (!brick.WasPlaced || anchor != brick.PrevBoardAnchor) AddMoves(-1);
            brick.transform.SetParent(boardRoot, true);
            brick.PlaceAt(anchor);
            _board.Occupy(brick, cells);
            bool fit = _board.InOneZone(cells);
            RecolorBrick(brick, _board.ZoneColors(cells), fit);
            if (fit) brick.PlayFitSquash(_cellSize);
            PlayBlockHit();
            SafeHaptic();
            StageEvents.RaiseBrickPlaced(brick);

            if (!brick.Scored)
            {
                brick.Scored = true;
                AddScore(cells.Count * DefaultKey.SCORE_PER_CELL_PLACED);
            }
            ResolveZones(cells);
        }
        // 트레이 안에서 자리만 바꾼다. 판 조각을 트레이로 빼는 건 막는다 — 빼 둘 수 있으면
        // 판과 트레이를 오가며 끝없이 버틸 수 있어서 게임 오버가 안 난다 (시뮬에서 대부분의 판이 그랬다)
        else if (overTray && !brick.WasPlaced && TryFindTrayAnchor(brick, out var trayAnchor))
        {
            brick.transform.SetParent(trayRoot, true);
            brick.TrayAnchor = trayAnchor;
            _trayGrid.Occupy(brick, brick.CellsAt(trayAnchor));
            brick.MoveToTray(_trayGrid.CellToWorld(trayAnchor));
            SafeSfx(AudioLibrarySounds.Drop, 0.6f);
            StageEvents.RaiseBrickReturned(brick);
        }
        else
        {
            RestorePrev(brick);
        }

        SpawnTrayIfEmpty();
        CheckGameOver();
    }

    // 둘 곳이 없으면 집기 전 자리로 돌아간다 — 판에서 집었으면 판으로, 트레이에서 집었으면 트레이로
    private void RestorePrev(Brick brick)
    {
        if (brick.WasPlaced)
        {
            brick.PlaceAt(brick.PrevBoardAnchor);
            _board.Occupy(brick, brick.CellsAt(brick.PrevBoardAnchor));
        }
        else
        {
            _trayGrid.Occupy(brick, brick.CellsAt(brick.TrayAnchor));
            brick.MoveToTray(_trayGrid.CellToWorld(brick.TrayAnchor));
        }
        StageEvents.RaiseBrickReturned(brick);
    }

    private bool TryFindNearestAnchor(Brick brick, out Vector2Int best)
    {
        const int range = 2;
        var current = brick.CurrentAnchor();
        Vector3 pos = brick.transform.position;
        var pieceXZ = new Vector2(pos.x, pos.z);

        best = current;
        float bestDist = float.MaxValue;
        bool found = false;

        for (int dx = -range; dx <= range; dx++)
            for (int dy = -range; dy <= range; dy++)
            {
                var cand = new Vector2Int(current.x + dx, current.y + dy);
                if (!_board.CanPlace(brick.CellsAt(cand))) continue;

                var world = _board.CellToWorld(cand);
                float d = (new Vector2(world.x, world.z) - pieceXZ).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = cand;
                    found = true;
                }
            }

        return found;
    }

    #region Zone Clear

    // 방금 놓은 칸이 걸친 구역만 본다 — 다른 구역은 이번 배치로 상태가 바뀌지 않는다
    private void ResolveZones(List<Vector2Int> placedCells)
    {
        var touched = new HashSet<int>();
        foreach (var c in placedCells) touched.Add(_board.ZoneOf(c));

        var full = new List<Board.Zone>();
        foreach (int id in touched)
            if (_board.IsZoneFull(_board.Zones[id])) full.Add(_board.Zones[id]);

        if (full.Count == 0)
        {
            BreakCombo();
            return;
        }

        int combo = BumpCombo();
        foreach (var zone in full)
        {
            var info = ScoreZone(zone, combo);
            ClearZone(zone);
            AddScore(info.Points);
            AddMoves(info.MovesGained);
            StageEvents.RaiseZoneCleared(info);
            if (info.Perfect) PlayPerfect();
        }
        PlayRegionComplete();
    }

    private ZoneClearInfo ScoreZone(Board.Zone zone, int combo)
    {
        int matched = 0;
        var center = Vector3.zero;
        foreach (var c in zone.Cells)
        {
            var b = _board.BrickAt(c);
            // 한 구역 안에 딱 맞게 놓인 조각의 칸만 센다 — 경계에 걸친 조각은 색은 같아도 보너스가 없다
            if (b != null && b.InOneZone) matched++;
            center += _board.CellToWorld(c);
        }
        center /= zone.Cells.Count;

        bool perfect = matched == zone.Cells.Count;
        int points = (zone.Cells.Count + matched * (DefaultKey.SCORE_MATCH_MULTIPLIER - 1)) * DefaultKey.SCORE_PER_CELL_CLEAR;
        if (perfect) points *= DefaultKey.SCORE_PERFECT_MULTIPLIER;
        points = Mathf.RoundToInt(points * ComboMultiplier(combo));

        return new ZoneClearInfo
        {
            ZoneId = zone.Id,
            Cells = zone.Cells.Count,
            MatchedCells = matched,
            Perfect = perfect,
            Combo = combo,
            Points = points,
            MovesGained = perfect ? DefaultKey.MOVES_PER_PERFECT : DefaultKey.MOVES_PER_CLEAR,
            WorldCenter = center,
        };
    }

    // 조각은 구역 경계에 걸쳐 있을 수 있어서 구역 안의 칸만 떼어낸다
    private void ClearZone(Board.Zone zone)
    {
        var removed = new Dictionary<Brick, HashSet<Vector2Int>>();
        foreach (var c in zone.Cells)
        {
            var b = _board.BrickAt(c);
            if (b == null) continue;
            if (!removed.TryGetValue(b, out var set)) removed[b] = set = new HashSet<Vector2Int>();
            set.Add(c - b.PlacedAnchor);
            _board.Free(c);
        }

        float wave = 0.03f;
        var origin = _board.CellToWorld(zone.Cells[0]);
        foreach (var kv in removed) SplitBrick(kv.Key, kv.Value, origin, wave);

        PunchZoneTiles(zone, 0.15f);
    }

    private void SplitBrick(Brick brick, HashSet<Vector2Int> removedLocal, Vector3 waveOrigin, float wave)
    {
        brick.SnapToPlaced();

        var oldLocal = new List<Vector2Int>(brick.LocalCells);
        var oldColors = brick.CellColors;
        var keep = new List<Vector2Int>();
        var keepColors = new List<BrickColor>();
        var cellsRoot = brick.transform.Find("Cells");

        // SetParent 로 떼어내면 뒤 칸들의 인덱스가 당겨지므로 먼저 목록을 받아 둔다
        var children = new List<Transform>();
        if (cellsRoot != null)
            foreach (Transform child in cellsRoot) children.Add(child);

        for (int i = 0; i < oldLocal.Count; i++)
        {
            if (!removedLocal.Contains(oldLocal[i]))
            {
                keep.Add(oldLocal[i]);
                keepColors.Add(oldColors[i]);
                continue;
            }
            if (i >= children.Count) continue;

            // 떼어낸 칸은 판에 남겨 두고 터뜨린다 — 조각을 다시 만들면 자식이 전부 새로 생긴다
            var cell = children[i];
            cell.SetParent(boardRoot, true);
            float delay = Vector3.Distance(cell.position, waveOrigin) * wave;
            PopCell(cell, delay);
        }

        if (keep.Count == 0)
        {
            _bricks.Remove(brick);
            Destroy(brick.gameObject);
            return;
        }

        brick.SetLocalCells(keep, keepColors);
        AddPieceColliders(brick.gameObject, keep);
        RebuildBrickVisual(brick);
    }

    private void RecolorBrick(Brick brick, List<BrickColor> colors, bool inOneZone)
    {
        bool same = true;
        for (int i = 0; i < colors.Count && same; i++) same = colors[i] == brick.CellColors[i];
        brick.SetCellColors(colors, inOneZone);
        if (!same) RebuildBrickVisual(brick);
    }

    // 같은 프레임에 구역 두 개가 지워지면 다음 SplitBrick 이 새 "Cells" 를 찾아야 한다.
    // Destroy 는 프레임 끝에 지워지므로 DiscardChild 가 이름부터 바꿔 둔다
    private void RebuildBrickVisual(Brick brick)
    {
        DiscardChild(brick.transform.Find("Cells"));
        DiscardChild(brick.transform.Find("Outline"));

        BuildPieceVisual(brick.transform, new List<Vector2Int>(brick.LocalCells), brick.CellColors);
    }

    private static void DiscardChild(Transform child)
    {
        if (child == null) return;
        child.name += "_Discarded";
        child.gameObject.SetActive(false);
        Destroy(child.gameObject);
    }

    private void PopCell(Transform cell, float delay)
    {
        cell.DOKill();
        float rise = _brickHeight * 1.5f;
        DOTween.Sequence()
            .AppendInterval(delay)
            .Append(cell.DOMoveY(cell.position.y + rise, 0.18f).SetEase(Ease.OutQuad))
            .Join(cell.DOScale(cell.localScale * 1.15f, 0.18f))
            .Append(cell.DOScale(0f, 0.15f).SetEase(Ease.InBack))
            .OnComplete(() => { if (cell != null) Destroy(cell.gameObject); });
    }

    #endregion

    #region Game Over / Revive

    // 횟수를 다 썼거나 (구역을 지워 받은 횟수까지 더한 뒤에 본다),
    // 트레이 조각을 판에 놓을 수 없고 판 조각 하나를 판 안에서 옮겨도 자리가 안 나면 끝이다.
    // 조각 하나 옮기기까지만 본다 — 플레이어가 한눈에 찾을 수 있는 수가 그 정도다
    private void CheckGameOver()
    {
        if (State != StageState.Playing || (_moves > 0 && HasMove())) return;
        State = StageState.Failed;
        OnGameOver().Forget();
    }

    private bool HasMove()
    {
        var tray = new List<Brick>(TrayBricks());
        foreach (var b in tray)
            if (_board.CanFitAnywhere(b.LocalCells)) return true;

        foreach (var moved in BoardBricks())
        {
            var from = new HashSet<Vector2Int>(moved.CellsAt(moved.PlacedAnchor));
            foreach (var t in tray)
                foreach (var anchor in _board.AllCells())
                {
                    var cells = t.CellsAt(anchor);
                    if (!_board.CanPlace(cells, from, null)) continue;
                    if (_board.CanFitAnywhere(moved.LocalCells, from, new HashSet<Vector2Int>(cells))) return true;
                }
        }
        return false;
    }

    private async UniTaskVoid OnGameOver()
    {
        CommitBestScore();
        PlayGameOver();
        await UniTask.Delay(700);
        StageEvents.RaiseStageFail();
    }

    // 횟수가 떨어져서 끝났으면 횟수를 준다. 판이 막혀 있으면 가장 많이 찬 구역부터
    // 점수 없이 비워 준다 — 그래도 안 들어가면 더 비운다
    public bool Revive()
    {
        if (!CanRevive) return false;
        _revivesUsed++;

        if (_moves <= 0) AddMoves(DefaultKey.REVIVE_MOVES);
        if (HasMove())
        {
            BreakCombo();
            State = StageState.Playing;
            return true;
        }

        var zones = new List<Board.Zone>(_board.Zones);
        zones.Sort((a, b) => _board.FilledCount(b).CompareTo(_board.FilledCount(a)));

        int cleared = 0;
        foreach (var zone in zones)
        {
            if (_board.FilledCount(zone) == 0) break;
            if (cleared >= DefaultKey.REVIVE_CLEAR_ZONES && HasMove()) break;
            ClearZone(zone);
            cleared++;
        }

        BreakCombo();
        PlayRegionComplete();
        State = StageState.Playing;
        return true;
    }

    // 개발자 메뉴용 — 판을 전부 비운다
    public void DebugClearBoard()
    {
        foreach (var zone in _board.Zones) ClearZone(zone);
        if (State == StageState.Failed) State = StageState.Playing;
    }

    #endregion

    #region Feedback

    private static void SafeSfx(AudioLibrarySounds sound, float volume)
    {
        try { Setting.PlaySFX(sound, volume); } catch { }
    }

    private static void SafeHaptic()
    {
        try { Setting.HapticWeak(); } catch { }
    }

    private static void SafeHapticSoft()
    {
        try { Setting.HapticSoft(); } catch { }
    }

    private static readonly AudioLibrarySounds[] _blockHits =
    {
        AudioLibrarySounds.BlockHit1, AudioLibrarySounds.BlockHit2, AudioLibrarySounds.BlockHit3,
        AudioLibrarySounds.BlockHit4, AudioLibrarySounds.BlockHit5, AudioLibrarySounds.BlockHit6,
        AudioLibrarySounds.BlockHit7, AudioLibrarySounds.BlockHit8, AudioLibrarySounds.BlockHit9,
    };

    private static void PlayBlockHit()
        => SafeSfx(_blockHits[UnityEngine.Random.Range(0, _blockHits.Length)], 0.8f);

    private void PlaySelect()
    {
        SafeSfx(AudioLibrarySounds.ui_click, 0.8f);
        SafeHaptic();
    }

    private void PlayRegionComplete()
    {
        SafeSfx(AudioLibrarySounds.Pop1, 1.0f);
        SafeHapticSoft();
    }

    private void PlayPerfect()
    {
        SafeSfx(AudioLibrarySounds.Select4, 0.8f);
        SafeHapticSoft();
    }

    private void PlayGameOver()
    {
        SafeSfx(AudioLibrarySounds.Drop, 0.8f);
        SafeHapticSoft();
    }

    #endregion
}
