public static class DefaultKey
{
    
    #region Option Configure
    
    public const string GOOGLE_PLAY_URL = "https://play.google.com/store/apps/details?id=com.areung.block.zone.aos";

    // TODO: iOS 출시하면 교체
    public const string APPLE_STORE_URL = "";

    public const string PRIVATE_POLICY_URL = "https://sites.google.com/view/areung-private-policy";
    public const string TERMS_SERVICE_URL = "https://sites.google.com/view/areung-terms-of-service";

    #endregion

    #region Board

    public const int BOARD_SIZE = 8;

    // 8x8 을 9개로 나누면 구역당 7칸 안팎 — 조각 두 개 정도로 채울 수 있는 크기다.
    // 시뮬(Tools/BalanceSim/colorless.py)에서 6개(7~14칸)는 한 판이 조각 30~70개로 너무 짧았다
    public const int ZONE_COUNT = 9;
    public const int ZONE_MIN_SIZE = 4;
    public const int ZONE_MAX_SIZE = 10;

    // BrickColor 앞에서부터 이만큼만 쓴다 (Red, Blue, Green, Yellow)
    public const int COLOR_COUNT = 4;

    #endregion

    #region Tray

    // 원래 게임의 트레이 크기. 새 조각이 여기 나온다 — 판 조각을 다시 빼 둘 수는 없다
    public const int TRAY_COLS = 7;
    public const int TRAY_ROWS = 3;

    // 트레이가 완전히 비면 이만큼 새로 나온다
    public const int TRAY_SPAWN_COUNT = 3;

    #endregion

    #region Moves

    // 판에 놓을 때마다 1회 쓴다 (판 조각을 옮기는 것도 1회). 0이 되면 끝난다.
    // 시뮬(colorless.py)에서 30회 시작 / 구역 +2 가 초보 4~5분, 잘하는 사람 10분 정도였다.
    // 50회 / +2 는 횟수가 거의 안 줄어서 판이 막혀 죽을 때까지 간다
    public const int MOVES_START = 30;
    public const int MOVES_PER_CLEAR = 2;
    public const int MOVES_PER_PERFECT = 3;

    // 이만큼 이하로 남으면 HUD 숫자를 경고색으로 바꾼다
    public const int MOVES_WARN = 5;

    #endregion

    #region Score

    public const int SCORE_PER_CELL_PLACED = 1;
    public const int SCORE_PER_CELL_CLEAR = 10;

    // 한 구역 안에만 놓은 조각(구역 색으로 바뀐 조각)의 칸은 이 배수만큼 더 받는다
    public const int SCORE_MATCH_MULTIPLIER = 2;

    // 구역 전체가 그런 칸이면 PERFECT — 구역 점수에 한 번 더 곱한다
    public const int SCORE_PERFECT_MULTIPLIER = 2;

    // 연속으로 구역을 지우면 1회마다 배수가 이만큼 오른다 (x1, x1.5, x2 ...)
    public const float SCORE_COMBO_STEP = 0.5f;

    #endregion

    #region Revive

    public const int REVIVE_PER_GAME = 1;

    // 이어하기 하면 가장 많이 찬 구역부터 이만큼 비워준다 (판이 막혀서 끝났을 때)
    public const int REVIVE_CLEAR_ZONES = 2;

    // 횟수가 떨어져서 끝났을 때 이어하기로 받는 횟수
    public const int REVIVE_MOVES = 10;

    #endregion

    #region Resources
    
    #endregion
}
