#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Areung_Plugin.Editor
{
    public class FirebaseDependencyInstaller : EditorWindow
    {
        private Vector2 _scroll; 
        
        private class PackageInfoUI
        {
            public string Name;
            public string Description;
            public string Source;
            public string Status = "대기 중";
            public bool IsInstalling = false;
            public bool IsInstalled = false;
            public AddRequest Request;
        }

        private List<PackageInfoUI> _packageList = new();
        private ListRequest _listRequest;

        private bool _isAllInstalling = false;
        private Queue<PackageInfoUI> _installQueue = new();

        [MenuItem("Areung/SDK/Firebase Installer", priority = 3)]
        public static void ShowWindow()
        {
            GetWindow<FirebaseDependencyInstaller>("Firebase 패키지");
        }

        private void OnEnable()
        {
            _packageList = new List<PackageInfoUI>
            {
                new()
                {
                    Name = "* App",
                    Description = "필수 설치",
                    Source = "https://github.com/GameWorkstore/com.google.firebase.app.git"
                },
                new()
                {
                    Name = "* Crashlytics",
                    Description = "실시간 비정상 종료 보고 도구",
                    Source = "https://github.com/GameWorkstore/com.google.firebase.crashlytics.git"
                },
                new()
                {
                    Name = "* Analytics",
                    Description = "이벤트 로깅",
                    Source = "https://github.com/GameWorkstore/com.google.firebase.analytics.git"
                },
                new()
                {
                    Name = "RemoteConfig",
                    Description = "A/B테스트",
                    Source = "https://github.com/GameWorkstore/com.google.firebase.remote-config.git"
                },
                new()
                {
                    Name = "Auth",
                    Description = "사용자 인증",
                    Source = "https://github.com/GameWorkstore/com.google.firebase.auth.git"
                },
                new()
                {
                    Name = "Messaging",
                    Description = "FCM 메시징 서비스",
                    Source = "https://github.com/GameWorkstore/com.google.firebase.messaging.git"
                },
            };

            _listRequest = Client.List(true);
            EditorApplication.update += WaitForPackageList;
        }

        private void OnDisable()
        {
            EditorApplication.update -= WaitForPackageList;
            EditorApplication.update -= UpdateProgress;
        }

        private void WaitForPackageList()
        {
            if (!_listRequest.IsCompleted) return;

            EditorApplication.update -= WaitForPackageList;

            foreach (var pkg in _packageList)
            {
                if (IsPackageAlreadyInstalled(pkg.Source, _listRequest.Result))
                {
                    pkg.Status = "설치됨";
                    pkg.IsInstalled = true;
                }
            }

            EditorApplication.update += UpdateProgress;
            Repaint();
        }

        private bool IsPackageAlreadyInstalled(string pkgSource, IEnumerable<UnityEditor.PackageManager.PackageInfo> installed)
        {
            foreach (var p in installed)
            {
                if (pkgSource.Contains(p.name) || p.packageId.Contains(pkgSource) || pkgSource == p.name)
                    return true;
            }
            return false;
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14
            };
            GUILayout.Label("필수 패키지 설치기", titleStyle);

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(_isAllInstalling))
            {
                if (GUILayout.Button("전체 다운로드", GUILayout.Width(120), GUILayout.Height(30)))
                {
                    StartBatchInstallation();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(20);
            _scroll = GUILayout.BeginScrollView(_scroll);

            foreach (var pkg in _packageList)
            {
                GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(0, 0, 5, 5),
                    fixedHeight = 100
                };

                GUILayout.BeginVertical(boxStyle);
                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();

                GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13,
                };
                GUILayout.Label(pkg.Name, nameStyle);

                GUIStyle descStyle = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true,
                    margin = new RectOffset(0, 0, 0, 20)
                };
                GUILayout.Label(pkg.Description, descStyle);
                GUILayout.Label(pkg.Source, EditorStyles.miniLabel);

                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                GUILayout.BeginVertical(GUILayout.Width(100));
                GUILayout.FlexibleSpace();

                if (!pkg.IsInstalling && !pkg.IsInstalled && pkg.Status == "대기 중")
                {
                    GUIStyle installButtonStyle = new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 12,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        fixedHeight = 24,
                        normal = {
                            textColor = Color.white,
                            background = MakeColorTexture(new Color(0.10f, 0.55f, 1f))
                        },
                        hover = {
                            textColor = Color.white,
                            background = MakeColorTexture(new Color(0.20f, 0.65f, 1.1f))
                        },
                        active = {
                            textColor = Color.white,
                            background = MakeColorTexture(new Color(0.05f, 0.45f, 0.9f))
                        },
                        border = new RectOffset(4, 4, 4, 4),
                        margin = new RectOffset(0, 0, 4, 4),
                        padding = new RectOffset(4, 4, 2, 2)
                    };

                    if (GUILayout.Button("Install", installButtonStyle))
                    {
                        pkg.Status = "⏳ 설치 중";
                        pkg.IsInstalling = true;
                        pkg.Request = Client.Add(pkg.Source);
                    }
                }
                else
                {
                    GUIStyle statusStyle = new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 12,
                        fontStyle = FontStyle.Bold,
                        fixedHeight = 24,
                        normal = {
                            textColor = Color.white,
                            background = MakeColorTexture(Color.gray)
                        }
                    };

                    GUILayout.Label(pkg.Status, statusStyle, GUILayout.Height(24));
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUILayout.EndScrollView();
        }

        private void UpdateProgress()
        {
            foreach (var pkg in _packageList)
            {
                if (pkg.IsInstalling && pkg.Request != null && pkg.Request.IsCompleted)
                {
                    pkg.IsInstalling = false;
                    pkg.IsInstalled = true;

                    if (pkg.Request.Status == StatusCode.Success)
                    {
                        pkg.Status = "설치됨";
                        string symbol = $"ENABLE_FIREBASE_{pkg.Name.ToUpper()}_SDK";
                        if (pkg.Name is "* App")
                        {
                            symbol = "ENABLE_FIREBASE_SDK";
                            AddScriptingDefineSymbolToAllPlatforms(symbol);
                        }
                    }
                    else
                        pkg.Status = "실패";

                    pkg.Request = null;
                    Repaint();

                    if (_isAllInstalling) InstallNextFromBatch();
                }
            }
        }

        private void StartBatchInstallation()
        {
            _isAllInstalling = true;
            _installQueue.Clear();

            foreach (var pkg in _packageList)
            {
                if (!pkg.IsInstalled && !pkg.IsInstalling && pkg.Status == "대기 중")
                    _installQueue.Enqueue(pkg);
            }

            InstallNextFromBatch();
        }

        private void InstallNextFromBatch()
        {
            if (_installQueue.Count == 0)
            {
                _isAllInstalling = false;
                return;
            }

            var pkg = _installQueue.Dequeue();
            pkg.Status = "⏳ 설치 중";
            pkg.IsInstalling = true;
            pkg.Request = Client.Add(pkg.Source);
        }

        private static Texture2D MakeColorTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
        
        private static void AddScriptingDefineSymbol(BuildTargetGroup targetGroup, string symbol)
        {
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            var defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            var defineList = new HashSet<string>(defines.Split(';'));

            if (!defineList.Contains(symbol))
            {
                defineList.Add(symbol);
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, string.Join(";", defineList));
            }
        }

        private static void AddScriptingDefineSymbolToAllPlatforms(string symbol)
        {
            AddScriptingDefineSymbol(BuildTargetGroup.Android, symbol);
            AddScriptingDefineSymbol(BuildTargetGroup.iOS, symbol);
        }
    }
}
#endif