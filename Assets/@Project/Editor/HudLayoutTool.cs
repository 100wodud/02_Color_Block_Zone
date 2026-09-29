using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 플레이 화면 HUD 를 컨셉 이미지대로 옮긴다:
// 왼쪽 위 점수 박스 · 그 옆 BEST 알약 · 오른쪽 설정 버튼 · 아래 가운데 큰 하늘색 남은 횟수.
// 다시 실행해도 같은 결과가 나온다 (이미 만든 박스는 재사용). 만든 뒤 씬에서 다듬고 저장한다
public static class HudLayoutTool
{
    private const string FramePath = "Assets/@Areung_Plugin/Resource/@UI/Resources/ResourcesData/Sprites/Components/Frame/BasicFrame_Round_12.png";

    private static readonly Color BoxColor = new(0.1f, 0.14f, 0.36f, 1f);
    private static readonly Color PillColor = new(0.14f, 0.19f, 0.45f, 1f);
    private static readonly Color MovesColor = new(0.35f, 0.85f, 1f, 1f);

    [MenuItem("Tools/Color Block Zone/Apply Gameplay HUD Layout")]
    private static void Apply()
    {
        var hud = Object.FindFirstObjectByType<StageHud>(FindObjectsInactive.Include);
        if (hud == null)
        {
            EditorUtility.DisplayDialog("HUD Layout", "GameScene 을 열고 다시 실행하세요 (StageHud 를 못 찾음).", "OK");
            return;
        }

        var so = new SerializedObject(hud);
        var setting = so.FindProperty("setting").objectReferenceValue as Button;
        var score = so.FindProperty("scoreText").objectReferenceValue as TextMeshProUGUI;
        var best = so.FindProperty("bestText").objectReferenceValue as TextMeshProUGUI;
        var moves = so.FindProperty("movesText").objectReferenceValue as TextMeshProUGUI;
        if (setting == null || score == null || best == null || moves == null)
        {
            EditorUtility.DisplayDialog("HUD Layout",
                "StageHud 의 setting / scoreText / bestText / movesText 중 비어 있는 게 있습니다.\n" +
                "movesText 가 비었으면 먼저 Tools > Color Block Zone > Create Moves HUD 를 실행하세요.", "OK");
            return;
        }

        var top = (RectTransform)setting.transform.parent;
        var oldParents = new[] { score.transform.parent, best.transform.parent, moves.transform.parent };

        // 설정 버튼과 같은 높이에 박스를 둔다. 설정 버튼은 Top 아래 모서리 기준이다
        var s = (RectTransform)setting.transform;
        float rowY = s.anchoredPosition.y + (0.5f - s.pivot.y) * s.sizeDelta.y;

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath);
        var scoreBox = Box(top, "ScoreBox", sprite, BoxColor, new Vector2(40f, rowY), new Vector2(220f, 100f));
        var bestPill = Box(top, "BestPill", sprite, PillColor, new Vector2(280f, rowY), new Vector2(300f, 76f));

        FitInto(score, scoreBox, 60f);
        FitInto(best, bestPill, 34f);

        Undo.SetTransformParent(moves.transform, top, "HUD Layout");
        Undo.RecordObject(moves.rectTransform, "HUD Layout");
        Undo.RecordObject(moves, "HUD Layout");
        var mr = moves.rectTransform;
        mr.anchorMin = mr.anchorMax = new Vector2(0.5f, 0f);
        mr.pivot = new Vector2(0.5f, 1f);
        mr.anchoredPosition = new Vector2(0f, -10f);
        mr.sizeDelta = new Vector2(400f, 120f);
        mr.localScale = Vector3.one;
        moves.enableAutoSizing = false;
        moves.fontSize = 96f;
        moves.alignment = TextAlignmentOptions.Center;
        moves.color = MovesColor;

        // 글자를 빼낸 옛 틀(시계 아이콘 틀, MovesFrame)은 끈다. 지우지는 않는다
        foreach (var p in oldParents)
        {
            if (p == null || p == top || p == scoreBox || p == bestPill) continue;
            Undo.RecordObject(p.gameObject, "HUD Layout");
            p.gameObject.SetActive(false);
        }

        so.FindProperty("bestFormat").stringValue = "BEST {0}";
        so.FindProperty("normalColor").colorValue = MovesColor;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        Selection.activeGameObject = top.gameObject;
    }

    private static RectTransform Box(RectTransform parent, string name, Sprite sprite, Color color, Vector2 pos, Vector2 size)
    {
        var rect = parent.Find(name) as RectTransform;
        if (rect == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = parent.gameObject.layer;
            Undo.RegisterCreatedObjectUndo(go, "HUD Layout");
            rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
        }

        var img = rect.GetComponent<Image>();
        Undo.RecordObject(img, "HUD Layout");
        img.sprite = sprite;
        img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        img.color = color;
        img.raycastTarget = false;

        Undo.RecordObject(rect, "HUD Layout");
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        return rect;
    }

    private static void FitInto(TextMeshProUGUI text, RectTransform box, float maxSize)
    {
        Undo.SetTransformParent(text.transform, box, "HUD Layout");
        Undo.RecordObject(text.rectTransform, "HUD Layout");
        Undo.RecordObject(text, "HUD Layout");

        var r = text.rectTransform;
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.offsetMin = new Vector2(12f, 6f);
        r.offsetMax = new Vector2(-12f, -6f);
        r.localScale = Vector3.one;

        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = maxSize;
    }
}
