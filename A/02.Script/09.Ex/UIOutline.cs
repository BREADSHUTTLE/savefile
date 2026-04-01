using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Image))]
[AddComponentMenu("UI/Effects/UI Outline")]
public class UIOutline : MonoBehaviour
{
    [SerializeField] private Color _outlineColor = Color.white;
    [SerializeField] [Range(1f, 10f)] private float _outlineWidth = 2f;
    
    [SerializeField] private bool _useDirectionalColors = false;
    [SerializeField] private Color _topColor = Color.white;
    [SerializeField] private Color _bottomColor = Color.white;
    [SerializeField] private Color _leftColor = Color.white;
    [SerializeField] private Color _rightColor = Color.white;
    
    [SerializeField] private bool _top = true;
    [SerializeField] private bool _bottom = true;
    [SerializeField] private bool _left = true;
    [SerializeField] private bool _right = true;
    
    [SerializeField] private Shader _shader;

    private Image _targetImage;
    private Material _material;
    private Material _originalMaterial;
    private bool _applied = false;
    
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    private static readonly int UseDirectionalColorsId = Shader.PropertyToID("_UseDirectionalColors");
    private static readonly int TopColorId = Shader.PropertyToID("_TopColor");
    private static readonly int BottomColorId = Shader.PropertyToID("_BottomColor");
    private static readonly int LeftColorId = Shader.PropertyToID("_LeftColor");
    private static readonly int RightColorId = Shader.PropertyToID("_RightColor");
    private static readonly int OutlineTopId = Shader.PropertyToID("_OutlineTop");
    private static readonly int OutlineBottomId = Shader.PropertyToID("_OutlineBottom");
    private static readonly int OutlineLeftId = Shader.PropertyToID("_OutlineLeft");
    private static readonly int OutlineRightId = Shader.PropertyToID("_OutlineRight");

    public Color OutlineColor
    {
        get => _outlineColor;
        set { _outlineColor = value; UpdateMaterial(); }
    }
    
    public float OutlineWidth
    {
        get => _outlineWidth;
        set { _outlineWidth = Mathf.Clamp(value, 1f, 10f); UpdateMaterial(); }
    }
    
    public bool UseDirectionalColors
    {
        get => _useDirectionalColors;
        set { _useDirectionalColors = value; UpdateMaterial(); }
    }
    
    public Color TopColor
    {
        get => _topColor;
        set { _topColor = value; UpdateMaterial(); }
    }
    
    public Color BottomColor
    {
        get => _bottomColor;
        set { _bottomColor = value; UpdateMaterial(); }
    }
    
    public Color LeftColor
    {
        get => _leftColor;
        set { _leftColor = value; UpdateMaterial(); }
    }
    
    public Color RightColor
    {
        get => _rightColor;
        set { _rightColor = value; UpdateMaterial(); }
    }
    
    public bool Top
    {
        get => _top;
        set { _top = value; UpdateMaterial(); }
    }
    
    public bool Bottom
    {
        get => _bottom;
        set { _bottom = value; UpdateMaterial(); }
    }
    
    public bool Left
    {
        get => _left;
        set { _left = value; UpdateMaterial(); }
    }
    
    public bool Right
    {
        get => _right;
        set { _right = value; UpdateMaterial(); }
    }

    private void Awake()
    {
        _targetImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void OnDisable()
    {
        Remove();
        Cleanup();
    }

    private void OnDestroy()
    {
        Remove();
        Cleanup();
    }

    private void Apply()
    {
        if (_targetImage == null)
            _targetImage = GetComponent<Image>();
            
        if (_targetImage == null)
            return;

        if (_shader == null)
            _shader = Shader.Find("UI/Outline");
        
        if (_shader == null)
        {
            Debug.LogError("UIOutline: Shader 없음. Inspector에서 할당해주세요.");
            return;
        }

        if (!_applied && _originalMaterial == null)
            _originalMaterial = _targetImage.material;

        if (_material == null)
        {
            _material = new Material(_shader);
            _material.name = "UIOutline (Instance)";
        }

        _targetImage.material = _material;
        _applied = true;
        
        UpdateMaterial();
    }

    private void Remove()
    {
        if (_targetImage == null)
            _targetImage = GetComponent<Image>();
            
        if (_targetImage != null && _applied)
        {
            _targetImage.material = _originalMaterial;
            _applied = false;
        }
    }

    private void Cleanup()
    {
        if (_material != null)
        {
            if (Application.isPlaying)
                Destroy(_material);
            else
                DestroyImmediate(_material);
            _material = null;
        }
    }

    private void UpdateMaterial()
    {
        if (_material == null)
            return;

        _material.SetColor(OutlineColorId, _outlineColor);
        _material.SetFloat(OutlineWidthId, _outlineWidth);
        _material.SetFloat(UseDirectionalColorsId, _useDirectionalColors ? 1f : 0f);
        _material.SetColor(TopColorId, _topColor);
        _material.SetColor(BottomColorId, _bottomColor);
        _material.SetColor(LeftColorId, _leftColor);
        _material.SetColor(RightColorId, _rightColor);
        _material.SetFloat(OutlineTopId, _top ? 1f : 0f);
        _material.SetFloat(OutlineBottomId, _bottom ? 1f : 0f);
        _material.SetFloat(OutlineLeftId, _left ? 1f : 0f);
        _material.SetFloat(OutlineRightId, _right ? 1f : 0f);
    }

#if UNITY_EDITOR
    private void Reset()
    {
        _shader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/02.Script/09.Ex/Shader/UIOutline.shader");
    }

    private void OnValidate()
    {
        if (_shader == null)
            _shader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/02.Script/09.Ex/Shader/UIOutline.shader");
        
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && isActiveAndEnabled)
            {
                if (_material == null)
                    Apply();
                else
                    UpdateMaterial();
            }
        };
    }
#endif
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(UIOutline))]
public class UIOutlineEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        UnityEditor.EditorGUILayout.LabelField("아웃라인", UnityEditor.EditorStyles.boldLabel);
        
        var useDirectional = serializedObject.FindProperty("_useDirectionalColors");
        
        if (!useDirectional.boolValue)
        {
            UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_outlineColor"), new GUIContent("Outline Color"));
        }
        
        UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_outlineWidth"), new GUIContent("Outline Width"));
        
        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("방향별 색상", UnityEditor.EditorStyles.boldLabel);
        UnityEditor.EditorGUILayout.PropertyField(useDirectional, new GUIContent("Use Directional Colors"));
        
        if (useDirectional.boolValue)
        {
            UnityEditor.EditorGUI.indentLevel++;
            UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_topColor"), new GUIContent("Top Color"));
            UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_bottomColor"), new GUIContent("Bottom Color"));
            UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_leftColor"), new GUIContent("Left Color"));
            UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_rightColor"), new GUIContent("Right Color"));
            UnityEditor.EditorGUI.indentLevel--;
        }
        
        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("방향 선택", UnityEditor.EditorStyles.boldLabel);
        UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_top"), new GUIContent("Top"));
        UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_bottom"), new GUIContent("Bottom"));
        UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_left"), new GUIContent("Left"));
        UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_right"), new GUIContent("Right"));
        
        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("셰이더", UnityEditor.EditorStyles.boldLabel);
        UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_shader"), new GUIContent("Shader"));
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
