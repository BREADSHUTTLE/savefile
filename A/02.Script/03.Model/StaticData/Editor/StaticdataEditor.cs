using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using Cysharp.Threading.Tasks;
using Google.Apis.Sheets.v4;
using Google.Apis.Json;
using CAPYBARA.Definition;
using System.IO;

namespace CAPYBARA.Editor
{
    public class StaticdataEditor : EditorWindow
    {
        private const string AppName = "CapyBaraStaticdataEditor";

        private static class ReflectionHelper
        {
            public static string GetFieldName(FieldInfo field)
            {
                return $"{field.FieldType.Name} {field.Name}";
            }
            public static bool IsDictionaryType(Type type)
            {
                return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
            }

            public static HashSet<string> GetFieldNames(Type rootType)
            {
                HashSet<string> names = new HashSet<string>();
                var rootFields = rootType.GetFields();
                foreach (var rootField in rootFields)
                {
                    if (IsDictionaryType(rootField.FieldType)) continue;
                    names.Add(GetFieldName(rootField));
                }
                return names;
            }


            static Dictionary<Type, string> _typeAlias = new Dictionary<Type, string>{
                {typeof(bool), "bool"},
                {typeof(byte), "byte"},
                {typeof(char), "char"},
                {typeof(decimal), "decimal"},
                {typeof(double), "double"},
                {typeof(float), "float"},
                {typeof(int), "int"},
                {typeof(long), "long"},
                {typeof(object), "object"},
                {typeof(sbyte), "sbyte"},
                {typeof(short), "short"},
                {typeof(string), "string"},
                {typeof(uint), "uint"},
                {typeof(ulong), "ulong"},
                {typeof(void), "void"}
            };

            //???? : https://stackoverflow.com/questions/56352299/gettype-return-int-instead-of-system-int32
            public static string GetTypeNameOrAlias(Type type)
            {
                var nullbase = Nullable.GetUnderlyingType(type);
                if (nullbase != null)
                    return GetTypeNameOrAlias(nullbase) + "?";

                if (type.BaseType == typeof(System.Array))
                    return GetTypeNameOrAlias(type.GetElementType()) + "[]";

                if (_typeAlias.TryGetValue(type, out string alias))
                    return alias;

                return type.Name;
            }
        }

        private Vector2 _scrollPositionForCheckedField;
        private Dictionary<string, bool> _isCheckedField = new Dictionary<string, bool>();
        private bool _isLoading = false;
        private bool _showsLog = false;

        private const int HeaderSizeY = 3;
        private const int DefaultSize = 1000;


        [MenuItem("Tools/🦫[CapyBara]🦫/🐰[CapybaraStaticDataEditor]스태틱데이터 관리")]
        public static void OpenWindow()
        {
            EditorWindow.GetWindow(typeof(StaticdataEditor));
        }

        public Type GetStaticDataWrapperType()
        {
            var typeName = $"CAPYBARA.Definition.StaticDataWrapper,Assembly-CSharp";
            var rootType = Type.GetType(typeName);
            var _type = typeof(CAPYBARA.Definition.StaticDataWrapper);
            return Type.GetType(typeName);
            //return _type;
        }

        private void OnGUI()
        {
            _showsLog = EditorGUILayout.Toggle("shows Log", _showsLog);
            GUILayout.Space(30);
            #region Sheet
            GUILayout.Label($"Static Data");
            GUILayout.Label($"version : {Application.version}");
            GUILayout.Space(10);

            var dataFieldNames = ReflectionHelper.GetFieldNames(GetStaticDataWrapperType());

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Select");
            if (GUILayout.Button("ALL Select"))
            {
                foreach (var fieldName in dataFieldNames)
                {
                    if (!_isCheckedField.ContainsKey(fieldName)) _isCheckedField.Add(fieldName, true);
                    _isCheckedField[fieldName] = true;
                }
            }
            else if (GUILayout.Button("All UnSelect"))
            {
                foreach (var fieldName in dataFieldNames)
                {
                    if (!_isCheckedField.ContainsKey(fieldName)) _isCheckedField.Add(fieldName, false);
                    _isCheckedField[fieldName] = false;
                }
            }
            GUILayout.EndHorizontal();
            _scrollPositionForCheckedField = GUILayout.BeginScrollView(_scrollPositionForCheckedField,
                GUILayout.Width(450), GUILayout.Height(500));
            EditorGUIUtility.labelWidth = 400;
            foreach (var fieldName in dataFieldNames)
            {
                if (!_isCheckedField.ContainsKey(fieldName)) _isCheckedField.Add(fieldName, false);
                _isCheckedField[fieldName] = EditorGUILayout.Toggle(fieldName, _isCheckedField[fieldName]);
            }

            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("build to sheet"))
            {
                if (_isLoading) return;
                BuildToSheetAsync().Forget();
            }
            if (GUILayout.Button("sheet to jsonFile"))
            {
                if (_isLoading) return;
                SheetToJsonAsync().Forget();
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(30);


            #endregion
        }

        async UniTask BuildToSheetAsync()
        {
            var sheetAPI = new CAPYBARASheetAPI(AppName);


            var staticDataWrapperType = GetStaticDataWrapperType();
            var staticDataWrapperFields = staticDataWrapperType.GetFields();
            foreach (var field in staticDataWrapperFields)
            {
                var fieldType = field.FieldType;
                if (ReflectionHelper.IsDictionaryType(fieldType)) continue;
                string sheetName = ReflectionHelper.GetFieldName(field);
                if (_isCheckedField[sheetName])
                {
                    FieldInfo[] fieldInfos;
                    if (fieldType.IsArray)
                    {
                        var elementType = fieldType.GetElementType();
                        fieldInfos = elementType.GetFields();
                    }
                    else
                    {
                        fieldInfos = fieldType.GetFields();
                    }


                    if (_showsLog)
                        Debug.Log($"[BuildToSheetAsync] sheetName:{sheetName}");
                    IList<IList<object>> bodyValue = new List<IList<object>>();

                    for (int y = 0; y < DefaultSize + HeaderSizeY; y++)
                    {
                        var row = new List<object>();
                        row.Add(string.Empty);
                        row.Add(string.Empty);
                        row.Add(string.Empty);
                        bodyValue.Add(row);
                    }
                    for (int y = 0; y < DefaultSize; y++)
                    {
                        var yy = HeaderSizeY + y;
                        bodyValue[yy][2] = y;
                    }

                    int rowIndex = 0;
                    bodyValue[rowIndex++][0] = "name";
                    bodyValue[rowIndex++][0] = sheetName;
                    bodyValue[rowIndex++][0] = "last build time";
                    bodyValue[rowIndex++][0] = DateTime.Now;

                    foreach (var fieldInfo in fieldInfos)
                    {
                        var type = fieldInfo.FieldType;
                        bodyValue[1].Add(ReflectionHelper.GetTypeNameOrAlias(type));
                        bodyValue[2].Add(fieldInfo.Name);
                    }

                    await sheetAPI.Update(sheetName, bodyValue);
                }
            }
        }
        // 셀 값 안전하게 가져오기 (범위 초과 방지)
        private object GetCellValue(IList<IList<object>> sheet, int y, int x, object defaultValue = null)
        {
            if (y < 0 || y >= sheet.Count) return defaultValue;
            if (x < 0 || x >= sheet[y].Count) return defaultValue;
            return sheet[y][x] ?? defaultValue;
        }
        
        private string GetCellString(IList<IList<object>> sheet, int y, int x, string defaultValue = "")
        {
            var value = GetCellValue(sheet, y, x);
            return value?.ToString() ?? defaultValue;
        }

        async UniTask SheetToJsonAsync()
        {
            var sheetAPI = new CAPYBARASheetAPI(AppName);

            var staticDataWrapperType = GetStaticDataWrapperType();
            var staticDataWrapperFields = staticDataWrapperType.GetFields();
            foreach (var dataCommonField in staticDataWrapperFields)
            {
                var fieldType = dataCommonField.FieldType;
                if (ReflectionHelper.IsDictionaryType(fieldType)) continue;
                string sheetName = ReflectionHelper.GetFieldName(dataCommonField);

                if (_isCheckedField[sheetName])
                {
                    Debug.Log($"[SheetToJsonAsync] start: {sheetName}]");
                    FieldInfo[] fieldInfos;
                    bool isArray = false;
                    if (fieldType.IsArray) //|| typeof(IEnumerable).IsAssignableFrom(type) || type.IsGenericType
                    {
                        isArray = true;
                        var elementType = fieldType.GetElementType();
                        fieldInfos = elementType.GetFields();
                    }
                    else
                    {
                        isArray = false;
                        fieldInfos = fieldType.GetFields();
                    }

                    var sheet = await sheetAPI.Read(sheetName);
                    List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
                    for (int y = HeaderSizeY; y < sheet.Count; y++)
                    {
                        if (GetCellString(sheet, y, 3) == "[END]")
                        {
                            break;
                        }
                        Dictionary<string, object> row = new Dictionary<string, object>();
                        int x = 3;
                        foreach (var fieldInfo in fieldInfos)
                        {
                            var cellValue = GetCellValue(sheet, y, x);
                            var cellString = cellValue?.ToString() ?? "";

                            if (_showsLog)
                                Debug.Log($"[SheetToJsonAsync] {sheetName} : {fieldInfo.FieldType} {fieldInfo.Name}");
                            if (fieldInfo.FieldType.IsEnum)
                            {
                                var spl = cellString.Split('.');
                                row.Add(fieldInfo.Name, spl.Length > 1 ? spl[1] : spl[0]); // enum을 문자열로 저장 (순서 변경에 안전)
                            }
                            else if (fieldInfo.FieldType.IsArray)
                            {
                                var elementType = fieldInfo.FieldType.GetElementType();
                                if (elementType == typeof(int))
                                {
                                    row.Add(fieldInfo.Name, string.IsNullOrEmpty(cellString) ? new int[0] : NewtonsoftJsonSerializer.Instance.Deserialize<IEnumerable<int>>(cellString));
                                }
                                else if (elementType == typeof(string))
                                {
                                    var str = cellString.Replace("\\n", "\n");
                                    row.Add(fieldInfo.Name, str);
                                }
                                else if (elementType == typeof(float))
                                {
                                    row.Add(fieldInfo.Name, string.IsNullOrEmpty(cellString) ? new float[0] : NewtonsoftJsonSerializer.Instance.Deserialize<IEnumerable<float>>(cellString));
                                }
                                else if (elementType == typeof(double))
                                {
                                    row.Add(fieldInfo.Name, string.IsNullOrEmpty(cellString) ? new double[0] : NewtonsoftJsonSerializer.Instance.Deserialize<IEnumerable<double>>(cellString));
                                }
                                else if (elementType == typeof(bool))
                                {
                                    row.Add(fieldInfo.Name, string.IsNullOrEmpty(cellString) ? new bool[0] : NewtonsoftJsonSerializer.Instance.Deserialize<IEnumerable<bool>>(cellString));
                                }
                                else if (elementType == typeof(long))
                                {
                                    row.Add(fieldInfo.Name, string.IsNullOrEmpty(cellString) ? new long[0] : NewtonsoftJsonSerializer.Instance.Deserialize<IEnumerable<long>>(cellString));
                                }
                                else if (elementType.IsEnum)
                                {
                                    if (string.IsNullOrEmpty(cellString) || cellString.Length < 2)
                                    {
                                        row.Add(fieldInfo.Name, new string[0]);
                                    }
                                    else
                                    {
                                        var text = cellString.Substring(1, cellString.Length - 2);
                                        var splitValues = text.Split(',');
                                        var values = new string[splitValues.Length]; // 문자열 배열로 저장
                                        for (int i = 0; i < values.Length; i++)
                                        {
                                            var spl = splitValues[i].Split('.');
                                            values[i] = spl.Length > 1 ? spl[1] : spl[0]; // enum을 문자열로 저장 (순서 변경에 안전)
                                        }
                                        row.Add(fieldInfo.Name, values);
                                    }
                                }
                            }
                            else
                            {
                                if (fieldInfo.FieldType == typeof(string))
                                {
                                    var str = cellString.Replace("\\n", "\n");
                                    row.Add(fieldInfo.Name, str);
                                }
                                else if (fieldInfo.FieldType.IsClass && fieldInfo.FieldType != typeof(string)
                                         || (fieldInfo.FieldType.IsValueType && !fieldInfo.FieldType.IsPrimitive && !fieldInfo.FieldType.IsEnum))
                                {
                                    var json = cellString;
                                    object value;
                                    if (string.IsNullOrWhiteSpace(json))
                                    {
                                        // 비었으면 기본 인스턴스
                                        value = Activator.CreateInstance(fieldInfo.FieldType);
                                    }
                                    else
                                    {
                                        try
                                        {
                                            value = NewtonsoftJsonSerializer.Instance.Deserialize(json, fieldInfo.FieldType);
                                        }
                                        catch (Exception e)
                                        {
                                            Debug.LogWarning($"[SheetToJsonAsync] {sheetName} : {fieldInfo.FieldType} 역직렬화 실패 -> 기본값. {e.Message}");
                                            value = Activator.CreateInstance(fieldInfo.FieldType);
                                        }
                                    }
                                    row.Add(fieldInfo.Name, value);
                                }
                                else
                                {
                                    if (cellValue != null)
                                    {
                                        row.Add(fieldInfo.Name, Convert.ChangeType(cellValue, fieldInfo.FieldType));
                                    }
                                    else
                                    {
                                        row.Add(fieldInfo.Name, fieldInfo.FieldType.IsValueType ? Activator.CreateInstance(fieldInfo.FieldType) : null);
                                    }
                                }
                            }
                            x++;
                        }
                        list.Add(row);
                    }
                    string fulltext = string.Empty;
                    if (isArray)
                    {
                        fulltext = NewtonsoftJsonSerializer.Instance.Serialize(list);
                    }
                    else
                    {

                        fulltext = NewtonsoftJsonSerializer.Instance.Serialize(list[0]);
                    }

                    await FileWriteAsync($"./Assets/13.CapyBara/Resources/JsonData/{dataCommonField.Name}.json", fulltext);
                }
            }
        }

        async UniTask FileWriteAsync(string path, string body)
        {
            using (StreamWriter outputFile = new StreamWriter(path))
            {
                await outputFile.WriteAsync(body);
            }

            if (_showsLog)
                Debug.Log($"[FileWriteAsync] path:{path}");
        }

    }

}
