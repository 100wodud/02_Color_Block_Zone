public partial class StageController
{
    private int _score;
    private int _combo;
    private int _moves;
    private int _bestAtStart;

    public int Moves => _moves;

    // 판을 시작할 때의 최고 기록을 넘었나 — 끝날 때 최고 기록을 저장하므로 저장 전 값과 비교한다
    public bool IsNewBest => _score > 0 && _score > _bestAtStart;

    public int Score => _score;
    public int Best => PlayerData.IsInitialized ? System.Math.Max(PlayerData.BestScore, _score) : _score;

    private void ResetScore()
    {
        _score = 0;
        _bestAtStart = PlayerData.IsInitialized ? PlayerData.BestScore : 0;
        _combo = 0;
        _revivesUsed = 0;
        _moves = DefaultKey.MOVES_START;
        StageEvents.RaiseScoreChanged(_score, Best);
        StageEvents.RaiseMovesChanged(_moves, 0);
    }

    private void AddMoves(int delta)
    {
        if (delta == 0) return;
        _moves = System.Math.Max(0, _moves + delta);
        StageEvents.RaiseMovesChanged(_moves, delta);
    }

    private void AddScore(int points)
    {
        if (points <= 0) return;
        _score += points;
        StageEvents.RaiseScoreChanged(_score, Best);
    }

    // 최고 점수는 판이 끝날 때만 저장한다 — 점수가 오를 때마다 저장하면 매 배치마다 세이브가 돈다
    private void CommitBestScore()
    {
        if (PlayerData.IsInitialized && _score > PlayerData.BestScore) PlayerData.BestScore = _score;
    }

    // 설정에서 다시 시작하거나 앱을 나가도 그 판 점수가 최고 기록에 남게 한다
    private void OnDestroy() => CommitBestScore();

    private int BumpCombo() => ++_combo;
    private void BreakCombo() => _combo = 0;

    private static float ComboMultiplier(int combo) => 1f + (combo - 1) * DefaultKey.SCORE_COMBO_STEP;
}
