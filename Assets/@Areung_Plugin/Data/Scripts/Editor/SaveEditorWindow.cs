using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Areung_Plugin.Data.Scripts.Editor
{
    public static class SaveActionsMenu
    {
#if UNITY_EDITOR
        [MenuItem("Areung/Data/Remove Save", priority = 1)]
        private static void RemoveSave()
        {
            PlayerPrefs.DeleteAll();
            SaveController.DeleteSaveFile();

            Debug.Log("삭제 완료!");
        }
#endif
    }
    
    public class SaveEditorWindow : EditorWindow
    {
#if UNITY_EDITOR
        private List<Type> _saveObjectTypes;
        private int _selectedTab = 0;
        
        private ISaveObject _currentSaveObject;
        private object _target;
        private FieldInfo[] _fields;
        private Vector2 _scroll;

        [MenuItem("Areung/Data/Save Editor", priority = 0)]
        public static void ShowEditWindow()
        {
            GetWindow<SaveEditorWindow>("Save Editor");
        }

        private void OnEnable()
        {
            SaveController.EditorInit();
            
            _saveObjectTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(ISaveObject).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .OrderBy(t => t.Name)
                .ToList();
            
            LoadSaveObject(_saveObjectTypes[_selectedTab]);
        }
        
        private void LoadSaveObject(Type type)
        {
            // 리플렉션으로 SaveController.GetSaveObject<T>(string) 호출
            MethodInfo method = typeof(SaveController).GetMethod("GetSaveObject", new[] { typeof(string) });
            if (method != null)
            {
                MethodInfo generic = method.MakeGenericMethod(type);
                _currentSaveObject = (ISaveObject)generic.Invoke(null, new object[] { type.Name });
            }

            _target = _currentSaveObject;
            _fields = _target.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private void OnGUI()
        {
            if (_saveObjectTypes == null || _saveObjectTypes.Count == 0)
            {
                EditorGUILayout.LabelField("ISaveObject 데이터 클래스가 없습니다.");
                return;
            }
            
            string[] tabNames = _saveObjectTypes.Select(t => t.Name).ToArray();
            int newTab = GUILayout.Toolbar(_selectedTab, tabNames);
            if (newTab != _selectedTab)
            {
                _selectedTab = newTab;
                LoadSaveObject(_saveObjectTypes[_selectedTab]);
            }

            EditorGUILayout.LabelField("Save Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.BeginVertical();
            foreach (var field in _fields)
            {
                object value = field.GetValue(_target);
                System.Type type = field.FieldType;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(field.Name), GUILayout.Width(200));

                if (type == typeof(int))
                {
                    int newValue = EditorGUILayout.IntField((int)value);
                    field.SetValue(_target, newValue);
                }
                else if (type == typeof(float))
                {
                    float newValue = EditorGUILayout.FloatField((float)value);
                    field.SetValue(_target, newValue);
                }
                else if (type == typeof(string))
                {
                    string newValue = EditorGUILayout.TextField((string)value);
                    field.SetValue(_target, newValue);
                }
                else if (type == typeof(bool))
                {
                    bool newValue = EditorGUILayout.Toggle((bool)value);
                    field.SetValue(_target, newValue);
                }
                else
                {
                    EditorGUILayout.LabelField($"({type.Name}) 미지원 타입");
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();

            if (GUILayout.Button("💾 저장", GUILayout.Height(40)))
            {
                SaveController.MarkAsSaveIsRequired();
                SaveController.Save(true);
                Debug.Log("저장 완료!");
            }
        }
#endif
    }
}