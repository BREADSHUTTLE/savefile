using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class FindObjectReferencesEditor : EditorWindow
{
    private GameObject targetObject;
    private Vector2 scrollPos;

    // 컴포넌트와 참조 필드명 같이 저장
    private readonly List<(Component component, string fieldName)> referencingComponents = new();

    // 타입별 필드 캐시(리플렉션 비용 절감)
    private static readonly Dictionary<System.Type, FieldInfo[]> _fieldCache = new();

    [MenuItem("Tools/🦫[CapyBara]🦫/SomeTimesUse/Find Object References In Scene(하이어라키 오브젝트 찾기)")]
    public static void ShowWindow()
    {
        GetWindow<FindObjectReferencesEditor>("Find References");
    }

    private void OnGUI()
    {
        GUILayout.Label("🔍 특정 오브젝트를 참조 중인 컴포넌트+필드명 찾기", EditorStyles.boldLabel);

        targetObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", targetObject, typeof(GameObject), true);

        if (GUILayout.Button("Find References"))
        {
            FindReferences();
        }

        if (referencingComponents.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label($"Found {referencingComponents.Count} references:", EditorStyles.boldLabel);
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            foreach (var (comp, fieldName) in referencingComponents)
            {
                if (comp == null) continue;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(comp.GetType().Name, comp, typeof(Component), true, GUILayout.Width(500));
                EditorGUILayout.LabelField($"→ {fieldName}", GUILayout.Width(400));
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }
    }

    private void FindReferences()
    {
        referencingComponents.Clear();

        if (targetObject == null)
        {
            Debug.LogWarning("🎯 Target GameObject가 없습니다.");
            return;
        }

        // 타깃 GO와 그 모든 컴포넌트의 InstanceID를 모두 허용(필드가 Component를 직접 참조해도 캐치)
        var targetIds = new HashSet<int> { targetObject.GetInstanceID() };
        foreach (var c in targetObject.GetComponents<Component>())
        {
            if (c != null) targetIds.Add(c.GetInstanceID());
        }

#if UNITY_2023_1_OR_NEWER
        var allComponents = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
#else
        var allComponents = Object.FindObjectsOfType<MonoBehaviour>(true);
#endif

        foreach (var comp in allComponents)
        {
            if (comp == null) continue;

            bool foundInThisComponent = false;

            // 1) 리플렉션 기반 스캔 (배열 / List<T> 포함)
            var type = comp.GetType();
            if (!_fieldCache.TryGetValue(type, out var fields))
            {
                fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                _fieldCache[type] = fields;
            }

            foreach (var field in fields)
            {
                // UnityEngine.Object 파생 또는 컬렉션으로 한정
                var ft = field.FieldType;

                // 단일 Object/Component/GameObject
                if (typeof(Object).IsAssignableFrom(ft))
                {
                    var val = field.GetValue(comp) as Object;
                    if (IsMatch(val, targetIds))
                    {
                        referencingComponents.Add((comp, field.Name));
                        foundInThisComponent = true;
                        // 굳이 같은 컴포넌트 내 중복표시 원치 않으면 다음 컴포넌트로
                        // break;  // <- 여러 필드도 보고싶으면 주석 유지(=break 사용 안 함)
                    }
                }
                // 배열 T[]
                else if (ft.IsArray)
                {
                    var elemType = ft.GetElementType();
                    if (elemType != null && typeof(Object).IsAssignableFrom(elemType))
                    {
                        var arr = field.GetValue(comp) as System.Array;
                        if (arr != null)
                        {
                            for (int i = 0; i < arr.Length; i++)
                            {
                                var o = arr.GetValue(i) as Object;
                                if (IsMatch(o, targetIds))
                                {
                                    referencingComponents.Add((comp, $"{field.Name}[{i}]"));
                                    foundInThisComponent = true;
                                    // break; // 배열 내 다수 매칭을 모두 보고싶으면 주석 유지
                                }
                            }
                        }
                    }
                }
                // List<T> 등 IList & IEnumerable 지원
                else if (typeof(IList).IsAssignableFrom(ft) || IsGenericList(ft))
                {
                    var elemType = GetIListElementType(ft);
                    if (elemType != null && typeof(Object).IsAssignableFrom(elemType))
                    {
                        var enumerable = field.GetValue(comp) as IEnumerable;
                        if (enumerable != null)
                        {
                            int idx = 0;
                            foreach (var item in enumerable)
                            {
                                var o = item as Object;
                                if (IsMatch(o, targetIds))
                                {
                                    referencingComponents.Add((comp, $"{field.Name}[{idx}]"));
                                    foundInThisComponent = true;
                                    // break; // 리스트 내 다수 매칭을 모두 보고싶으면 주석 유지
                                }
                                idx++;
                            }
                        }
                    }
                }
            }

            // 2) SerializedProperty 기반 스캔 (인스펙터에 보이는 직렬화 대상 전부 순회)
            //    - 배열/리스트 요소까지 전부 탐색
            SerializedObject so = new SerializedObject(comp);
            var it = so.GetIterator();

            // NextVisible 대신 Next 사용하면 숨김 필드 포함 더 폭넓게 순회 가능
            bool enterChildren = true;
            while (it.Next(enterChildren))
            {
                enterChildren = false;

                if (it.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (targetIds.Contains(it.objectReferenceInstanceIDValue))
                    {
                        referencingComponents.Add((comp, it.propertyPath));
                        foundInThisComponent = true;
                        // 계속 탐색(동일 컴포넌트 내 다수 표시 목적)
                    }
                }
                else if (it.isArray && it.propertyType == SerializedPropertyType.Generic)
                {
                    // 배열/리스트 요소 순회
                    for (int i = 0; i < it.arraySize; i++)
                    {
                        var element = it.GetArrayElementAtIndex(i);
                        if (element.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            if (targetIds.Contains(element.objectReferenceInstanceIDValue))
                            {
                                referencingComponents.Add((comp, $"{it.propertyPath}[{i}]"));
                                foundInThisComponent = true;
                                // 계속 탐색
                            }
                        }
                    }
                }
            }

            // 성능을 위해, 이 컴포넌트에서 이미 찾았어도 다른 컴포넌트로 넘어가자.
            // (동일 컴포넌트 내 전부 보고 싶으면 이 조건문은 의미 없음)
            if (foundInThisComponent)
            {
                // nothing; 루프는 계속 다른 컴포넌트로 이동
            }
        }
    }

    private static bool IsMatch(Object value, HashSet<int> targetIds)
    {
        if (value == null) return false;

        // GameObject/Component 모두 InstanceID 비교로 통일
        return targetIds.Contains(value.GetInstanceID());
    }

    private static bool IsGenericList(System.Type t)
    {
        return t.IsGenericType && typeof(IList).IsAssignableFrom(t);
    }

    private static System.Type GetIListElementType(System.Type listType)
    {
        if (listType.IsArray) return listType.GetElementType();
        if (listType.IsGenericType) return listType.GetGenericArguments()[0];
        return null;
    }
}
