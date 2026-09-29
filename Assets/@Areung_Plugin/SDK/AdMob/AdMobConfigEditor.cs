#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Areung_Plugin.SDK.AdMob
{
    [CustomEditor(typeof(AdMobConfig))]
    public class AdMobConfigEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            AdMobConfig config = (AdMobConfig)target;

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Test Device CSV 변환", EditorStyles.boldLabel);

            if (config.testDeviceCSV != null)
            {
                if (GUILayout.Button("📥 CSV에서 Test Device ID 가져오기"))
                {
                    string text = config.testDeviceCSV.text;
                    string[] lines = text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                    var ids = new System.Collections.Generic.List<string>();
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        if (string.IsNullOrEmpty(line)) continue;
                        string[] parts = line.Split(',');
                        if (i == 0 && parts[0].ToLower().Contains("owner")) continue;
                        if (parts.Length > 1)
                        {
                            string id = parts[1].Trim();
                            if (!string.IsNullOrEmpty(id))
                                ids.Add(id);
                        }
                    }

                    Undo.RecordObject(config, "Import Test Keys");
                    config.testDeviceIds = ids.ToArray();
                    EditorUtility.SetDirty(config);
                    AssetDatabase.SaveAssets();

                    Debug.Log($"✅ {ids.Count}개의 Test Device ID가 설정되었습니다.");
                }

                // WHY: AdMob 의 테스트 기기 ID 는 기존 미디에이션이 쓰던 광고 식별자와 형식이 다르다.
                // 안드로이드는 광고 ID 의 MD5 해시라 logcat 에 찍히는 값을 그대로 받아야 한다
                EditorGUILayout.HelpBox(
                    "AdMob 테스트 기기 ID 는 앱 실행 로그에 찍히는 값입니다.\n" +
                    "Android: logcat 의 \"Use RequestConfiguration.Builder.setTestDeviceIds(...)\" 줄\n" +
                    "iOS: Xcode 콘솔의 GADMobileAds 테스트 기기 안내 줄",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("CSV를 위 필드에 드래그하세요.", MessageType.Info);
            }
        }
    }
}
#endif
