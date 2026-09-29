#if UNITY_EDITOR && ENABLE_GPGS_SDK
using GooglePlayGames.Editor;
using UnityEditor;
using UnityEngine;

namespace Areung_Plugin.SDK.Social.EditorTools
{
    // GPGS 셋업 창과 같은 일을 하되 입력값을 에셋에 남깁니다. 그 창은 프로젝트 설정에만 넣어서
    // 다른 사람이 클론하면 클라이언트 ID 와 XML 을 다시 받아와야 합니다.
    [CustomEditor(typeof(SocialConfig))]
    public class SocialConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var config = (SocialConfig)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("GPGS 셋업", EditorStyles.boldLabel);

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorGUILayout.HelpBox("빌드 타겟이 Android 일 때만 실행할 수 있습니다.", MessageType.Info);
                return;
            }

            string blocker = Validate(config);
            if (blocker != null) EditorGUILayout.HelpBox(blocker, MessageType.Warning);

            using (new EditorGUI.DisabledScope(blocker != null))
            {
                if (GUILayout.Button("GPGS 셋업 실행"))
                    Run(config);
            }

            EditorGUILayout.HelpBox(
                "리소스 XML 을 파싱해 상수 클래스를 만들고 AndroidManifest 와 Play Services Resolver 까지 갱신합니다.",
                MessageType.None);
        }

        // 실행 못 하는 이유. 없으면 null. 웹 클라이언트 ID 는 선택입니다.
        private static string Validate(SocialConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.gpgsResourcesDefinition))
                return "리소스 XML 이 비어 있습니다. Play Console 의 '리소스 가져오기' 내용을 붙여넣으세요.";

            if (string.IsNullOrWhiteSpace(config.gpgsConstantsClassName))
                return "상수 클래스 이름이 비어 있습니다.";

            if (string.IsNullOrWhiteSpace(config.gpgsConstantsDirectory))
                return "상수 클래스 폴더가 비어 있습니다.";

            return null;
        }

        private static void Run(SocialConfig config)
        {
            bool ok = GPGSAndroidSetupUI.PerformSetup(
                config.gpgsWebClientId?.Trim(),
                config.gpgsConstantsDirectory.Trim(),
                config.gpgsConstantsClassName.Trim(),
                config.gpgsResourcesDefinition,
                null);

            if (ok)
                Debug.Log("[Social] GPGS 셋업 완료 — 상수 클래스와 AndroidManifest 가 갱신되었습니다.");
            else
                Debug.LogError("[Social] GPGS 셋업 실패 — 웹 클라이언트 ID 형식이나 리소스 XML 의 앱 ID 가 어긋난 경우가 대부분입니다.");
        }
    }
}
#endif
