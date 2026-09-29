using UnityEngine;

// 판이 끝나면 StageFail_Popup 을 띄운다. 무한 모드라 클리어는 없다. Owns no game state.
public class GameUI : MonoBehaviour
{
    public void Initialize() { }

    private void OnEnable() => StageEvents.StageFail += OnStageFail;
    private void OnDisable() => StageEvents.StageFail -= OnStageFail;

    private void OnStageFail() => OpenPopup("StageFail_Popup");

    private static void OpenPopup(string name)
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.Popup != null) gm.Popup.OpenPopup(name);
        else Debug.LogWarning($"[GameUI] GameManager/Popup not ready — cannot open {name}.");
    }
}
