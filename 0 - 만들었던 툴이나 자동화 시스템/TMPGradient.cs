using System.Collections;
using UnityEngine;
using TMPro;

[AddComponentMenu("UI/Effects/TMP Gradient")]
[RequireComponent(typeof(TMP_Text))]
[ExecuteAlways]
public class TMPGradient : MonoBehaviour
{
    public enum GradientType
    {
        Vertical,
        Horizontal,
        PerCharacter
    }

    public enum BlendMode
    {
        Multiply,
        Override
    }

    public enum GradientMode
    {
        TwoColors,
        MultiColors
    }

    [SerializeField] private GradientType _gradientType = GradientType.Vertical;
    [SerializeField] private BlendMode _blendMode = BlendMode.Override;
    [SerializeField] private GradientMode _gradientMode = GradientMode.TwoColors;
    

    [SerializeField] private Color _colorTop = Color.white;
    [SerializeField] private Color _colorBottom = Color.black;
    

    [SerializeField] private Gradient _gradient;
    
    [SerializeField] [Range(-1f, 1f)] private float _offset = 0f;

    private TMP_Text _textComponent;
    private bool _isUpdating = false;

    public GradientType Type
    {
        get => _gradientType;
        set { _gradientType = value; ApplyGradient(); }
    }

    public BlendMode Blend
    {
        get => _blendMode;
        set { _blendMode = value; ApplyGradient(); }
    }

    public GradientMode Mode
    {
        get => _gradientMode;
        set { _gradientMode = value; ApplyGradient(); }
    }

    public Color ColorTop
    {
        get => _colorTop;
        set { _colorTop = value; ApplyGradient(); }
    }

    public Color ColorBottom
    {
        get => _colorBottom;
        set { _colorBottom = value; ApplyGradient(); }
    }

    public Gradient GradientColors
    {
        get
        {
            if (_gradient == null)
                _gradient = new Gradient();
            return _gradient;
        }
        set { _gradient = value; ApplyGradient(); }
    }

    public float Offset
    {
        get => _offset;
        set { _offset = value; ApplyGradient(); }
    }

    private void Reset()
    {
        InitializeDefaultGradient();
    }

    private void InitializeDefaultGradient()
    {
        if (_gradient == null)
            _gradient = new Gradient();
            
        GradientColorKey[] colorKeys = new GradientColorKey[4];
        colorKeys[0] = new GradientColorKey(Color.red, 0f);
        colorKeys[1] = new GradientColorKey(Color.yellow, 0.33f);
        colorKeys[2] = new GradientColorKey(Color.green, 0.66f);
        colorKeys[3] = new GradientColorKey(Color.blue, 1f);
        
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);
        
        _gradient.SetKeys(colorKeys, alphaKeys);
    }

    private void Awake()
    {
        _textComponent = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (_textComponent == null)
            _textComponent = GetComponent<TMP_Text>();

        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        ApplyGradient();
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    private void RestoreOriginalColors()
    {
        if (_textComponent == null)
            return;
        
        // 원본 색상으로 복원
        TMP_TextInfo textInfo = _textComponent.textInfo;
        if (textInfo == null || textInfo.meshInfo == null)
            return;

        Color32 originalColor = _textComponent.color;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
                continue;

            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            
            if (materialIndex >= textInfo.meshInfo.Length)
                continue;
                
            Color32[] colors = textInfo.meshInfo[materialIndex].colors32;
            if (colors == null)
                continue;

            for (int j = 0; j < 4; j++)
            {
                if (vertexIndex + j < colors.Length)
                    colors[vertexIndex + j] = originalColor;
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            if (textInfo.meshInfo[i].mesh != null)
            {
                textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
                _textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }
    }
    private void OnTextChanged(Object obj)
    {
        if (_isUpdating)
            return;

        if (obj == _textComponent)
            ApplyGradient();
    }

    public void ApplyGradient()
    {
        if (_textComponent == null || _isUpdating)
            return;

        _isUpdating = true;

        try
        {
            _textComponent.ForceMeshUpdate();

            TMP_TextInfo textInfo = _textComponent.textInfo;
            if (textInfo == null || textInfo.characterCount == 0)
                return;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                    continue;

                int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

                for (int j = 0; j < 4; j++)
                {
                    Vector3 v = vertices[vertexIndex + j];
                    minX = Mathf.Min(minX, v.x);
                    maxX = Mathf.Max(maxX, v.x);
                    minY = Mathf.Min(minY, v.y);
                    maxY = Mathf.Max(maxY, v.y);
                }
            }

            float width = maxX - minX;
            float height = maxY - minY;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                    continue;

                int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                Color32[] colors = textInfo.meshInfo[materialIndex].colors32;
                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

                float charMinY = float.MaxValue, charMaxY = float.MinValue;
                if (_gradientType == GradientType.PerCharacter)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        charMinY = Mathf.Min(charMinY, vertices[vertexIndex + j].y);
                        charMaxY = Mathf.Max(charMaxY, vertices[vertexIndex + j].y);
                    }
                }

                for (int j = 0; j < 4; j++)
                {
                    Vector3 pos = vertices[vertexIndex + j];
                    float factor = CalculateGradientFactor(pos.x, pos.y, minX, maxX, minY, maxY, charMinY, charMaxY, width, height);
                    
                    Color gradientColor;
                    if (_gradientMode == GradientMode.MultiColors && _gradient != null)
                        gradientColor = _gradient.Evaluate(factor);
                    else
                        gradientColor = Color.Lerp(_colorBottom, _colorTop, factor);

                    if (_blendMode == BlendMode.Multiply)
                        colors[vertexIndex + j] = MultiplyColor(colors[vertexIndex + j], gradientColor);
                    else
                        colors[vertexIndex + j] = gradientColor;
                }
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
                _textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private float CalculateGradientFactor(float x, float y, float minX, float maxX, float minY, float maxY, float charMinY, float charMaxY, float width, float height)
    {
        float factor = 0f;

        switch (_gradientType)
        {
            case GradientType.Vertical:
                factor = height > 0 ? (y - minY) / height : 0f;
                break;

            case GradientType.Horizontal:
                factor = width > 0 ? (x - minX) / width : 0f;
                break;

            case GradientType.PerCharacter:
                float charHeight = charMaxY - charMinY;
                factor = charHeight > 0 ? (y - charMinY) / charHeight : 0f;
                break;
        }

        factor = Mathf.Clamp01(factor + _offset);
        return factor;
    }

    private Color32 MultiplyColor(Color32 original, Color gradient)
    {
        return new Color32((byte)(original.r * gradient.r), (byte)(original.g * gradient.g), (byte)(original.b * gradient.b), (byte)(original.a * gradient.a));
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_textComponent == null)
            _textComponent = GetComponent<TMP_Text>();

        if (_isUpdating || !isActiveAndEnabled)
            return;

        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && _textComponent != null && !_isUpdating && isActiveAndEnabled)
                ApplyGradient();
        };
    }
#endif
}
