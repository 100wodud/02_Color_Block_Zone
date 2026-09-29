using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// 스토어 스크린샷을 Game 뷰 그대로 찍는다 (HUD 포함, 에디터라 배너 광고는 안 뜬다).
// Game 뷰 해상도를 1080x1920 (폰) 또는 1440x2560 (태블릿) 으로 맞추고 플레이 중에 F9 를 누른다.
// 파일은 프로젝트 폴더의 StoreScreenshots/ 에 쌓인다 — Assets 밖이라 임포트되지 않는다
public static class StoreScreenshotTool
{
    private const string Folder = "StoreScreenshots";

    [MenuItem("Tools/Color Block Zone/Capture Store Screenshot _F9")]
    private static void Capture()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Store Screenshot", "플레이 중에 찍으세요.", "OK");
            return;
        }

        Directory.CreateDirectory(Folder);
        var size = Handles.GetMainGameViewSize();
        string path = Path.Combine(Folder, $"shot_{DateTime.Now:yyyyMMdd_HHmmss}_{(int)size.x}x{(int)size.y}.png");
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log($"[StoreScreenshot] {Path.GetFullPath(path)}");
    }

    [MenuItem("Tools/Color Block Zone/Open Store Screenshot Folder")]
    private static void OpenFolder()
    {
        Directory.CreateDirectory(Folder);
        EditorUtility.RevealInFinder(Path.GetFullPath(Folder));
    }
}
