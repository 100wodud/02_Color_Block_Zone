using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 남은 놓기 횟수 칸을 HUD 에 만든다. 최고 점수 칸(아이콘 + 숫자 틀)을 복사해서 바로 아래에 둔다.
// 한 번만 쓰는 도구다 — 만든 뒤 위치·아이콘은 씬에서 직접 다듬고 저장한다
public static class MovesHudCreator
{
    private const string FrameName = "MovesFrame";

    [MenuItem("Tools/Color Block Zone/Create Moves HUD")]
    private static void Create()
    {
        var hud = Object.FindFirstObjectByType<StageHud>(FindObjectsInactive.Include);
        if (hud == null)
        {
            EditorUtility.DisplayDialog("Moves HUD", "GameScene 을 열고 다시 실행하세요 (StageHud 를 못 찾음).", "OK");
            return;
        }

        var so = new SerializedObject(hud);
        var best = so.FindProperty("bestText").objectReferenceValue as TextMeshProUGUI;
        var score = so.FindProperty("scoreText").objectReferenceValue as TextMeshProUGUI;
        if (best == null)
        {
            EditorUtility.DisplayDialog("Moves HUD", "StageHud 의 bestText 가 비어 있어서 복사할 틀이 없습니다.", "OK");
            return;
        }

        var frame = best.rectTransform.parent as RectTransform;
        var existing = frame.parent.Find(FrameName);
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorUtility.DisplayDialog("Moves HUD", "이미 MovesFrame 이 있습니다.", "OK");
            return;
        }

        var clone = Object.Instantiate(frame.gameObject, frame.parent);
        clone.name = FrameName;
        Undo.RegisterCreatedObjectUndo(clone, "Create Moves HUD");

        var rect = (RectTransform)clone.transform;
        rect.anchoredPosition = frame.anchoredPosition - new Vector2(0f, frame.sizeDelta.y + 15f);

        // 지우기 전에 잡아 둔다 — 형제를 지우면 인덱스가 당겨진다
        var moves = clone.transform.GetChild(best.transform.GetSiblingIndex()).GetComponent<TextMeshProUGUI>();

        // 점수 글자가 같은 틀 안에 있으면 복사본에서는 뺀다
        if (score != null && score.transform.parent == frame)
            Object.DestroyImmediate(clone.transform.GetChild(score.transform.GetSiblingIndex()).gameObject);

        moves.gameObject.name = "Moves";
        moves.text = DefaultKey.MOVES_START.ToString();

        so.FindProperty("movesText").objectReferenceValue = moves;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        Selection.activeGameObject = clone;
    }
}
