using System;

public static class StageEvents
{
    public static event Action<Brick> BrickPicked;
    public static event Action<Brick> BrickPlaced;
    public static event Action<Brick> BrickReturned;
    public static event Action StageLoaded;
    public static event Action StageFail;
    // (score, best)
    public static event Action<int, int> ScoreChanged;
    public static event Action<ZoneClearInfo> ZoneCleared;
    // (남은 횟수, 이번에 바뀐 양)
    public static event Action<int, int> MovesChanged;

    public static void RaiseBrickPicked(Brick b) => BrickPicked?.Invoke(b);
    public static void RaiseBrickPlaced(Brick b) => BrickPlaced?.Invoke(b);
    public static void RaiseBrickReturned(Brick b) => BrickReturned?.Invoke(b);
    public static void RaiseStageLoaded() => StageLoaded?.Invoke();
    public static void RaiseStageFail() => StageFail?.Invoke();
    public static void RaiseScoreChanged(int score, int best) => ScoreChanged?.Invoke(score, best);
    public static void RaiseZoneCleared(ZoneClearInfo info) => ZoneCleared?.Invoke(info);
    public static void RaiseMovesChanged(int moves, int delta) => MovesChanged?.Invoke(moves, delta);
}

public struct ZoneClearInfo
{
    public int ZoneId;
    public int Cells;
    public int MatchedCells;
    public bool Perfect;
    public int Combo;
    public int Points;
    public int MovesGained;
    public UnityEngine.Vector3 WorldCenter;
}
