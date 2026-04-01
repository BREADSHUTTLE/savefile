using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Image))]
[AddComponentMenu("UI/Effects/UI Image Glow")]
public class UIImageGlow : MonoBehaviour
{
    [Header("글로우 설정")]
    [SerializeField] private Color _glowColor = new Color(0.3f, 0.6f, 1f, 1f);
    [SerializeField] [Range(0f, 2f)] private float _glowIntensity = 1f;
    [SerializeField] [Range(0f, 100f)] private float _glowSize = 30f;
    [SerializeField] private Vector2 _glowOffset = Vector2.zero;
    
    [Header("글로우 크기 보정 (상하좌우)")]
    [SerializeField] private float _padLeft = 0f;
    [SerializeField] private float _padRight = 0f;
    [SerializeField] private float _padTop = 0f;
    [SerializeField] private float _padBottom = 0f;
    
    [Header("글로우 모양")]
    [SerializeField] [Range(0.5f, 20f)] private float _falloff = 2f;
    [SerializeField] [Range(0f, 0.3f)] private float _innerPadding = 0f;
    [SerializeField] [Range(0f, 0.5f)] private float _cornerRadius = 0.01f;
    [SerializeField] [Range(0f, 1f)] private float _cornerSoftness = 0f;
    
    [Header("애니메이션")]
    [SerializeField] private bool _enablePulse = false;
    [SerializeField] [Range(0.1f, 5f)] private float _pulseSpeed = 1f;
    [SerializeField] [Range(0f, 0.5f)] private float _pulseAmount = 0.2f;
    
    [Header("옵션")]
    [SerializeField] private bool _autoResize = true;
    [SerializeField] [Range(64, 512)] private int _textureSize = 128;
    
    [Header("스큐 동기화")]
    [SerializeField] private bool _debugSkew = false;

    private Image _targetImage;
    [System.NonSerialized] internal GameObject _glowObject;
    private Image _glowImage;
    private RectTransform _glowRect;
    private Texture2D _glowTexture;
    private Sprite _glowSprite;
    
    // UISkew 동기화용
    private UISkew _sourceSkew;
    private UISkew _glowSkew;
    
    private Vector2 _lastSize;
    private Color _lastColor;
    private float _lastFalloff;
    private float _lastInnerPadding;
    private float _lastCornerRadius;
    private float _lastCornerSoftness;
    private int _lastTextureSize;
    private float _baseIntensity;

    #region Properties
    public Color GlowColor
    {
        get => _glowColor;
        set
        {
            _glowColor = value;
            UpdateGlowColor();
        }
    }
    
    public float GlowIntensity
    {
        get => _glowIntensity;
        set
        {
            _glowIntensity = Mathf.Clamp(value, 0f, 2f);
            UpdateGlowColor();
        }
    }
    
    public float GlowSize
    {
        get => _glowSize;
        set
        {
            _glowSize = Mathf.Max(0f, value);
            UpdateGlowSize();
        }
    }
    
    public float Falloff
    {
        get => _falloff;
        set
        {
            _falloff = Mathf.Clamp(value, 0.5f, 20f);
            RegenerateTexture();
        }
    }
    
    public float InnerPadding
    {
        get => _innerPadding;
        set
        {
            _innerPadding = Mathf.Clamp(value, 0f, 0.3f);
            RegenerateTexture();
        }
    }
    
    public float CornerRadius
    {
        get => _cornerRadius;
        set
        {
            _cornerRadius = Mathf.Clamp(value, 0f, 0.5f);
            RegenerateTexture();
        }
    }
    
    public float CornerSoftness
    {
        get => _cornerSoftness;
        set
        {
            _cornerSoftness = Mathf.Clamp01(value);
            RegenerateTexture();
        }
    }
    
    public bool EnablePulse
    {
        get => _enablePulse;
        set => _enablePulse = value;
    }
    #endregion

    private string GlowObjectName => "ImageGlow_" + gameObject.name;

    private void Awake()
    {
        _targetImage = GetComponent<Image>();
        _baseIntensity = _glowIntensity;
        TryFindExistingGlow();
    }
    
    private void TryFindExistingGlow()
    {
        if (_glowObject != null || transform.parent == null)
            return;
            
        string glowName = GlowObjectName;
        Transform parent = transform.parent;
        int myIndex = transform.GetSiblingIndex();
        
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform sibling = parent.GetChild(i);
            if (sibling == transform)
                continue;
                
            bool isMyGlow = sibling.name == glowName;
            bool isOldGlow = sibling.name == "ImageGlow" && i == myIndex - 1;
            
            if (isMyGlow || isOldGlow)
            {
                _glowObject = sibling.gameObject;
                _glowRect = sibling.GetComponent<RectTransform>();
                _glowImage = sibling.GetComponent<Image>();
                
                if (isOldGlow)
                    _glowObject.name = glowName;
                
                if (_glowImage != null && _glowImage.sprite != null)
                {
                    _glowSprite = _glowImage.sprite;
                    _glowTexture = _glowSprite.texture;
                }
                break;
            }
        }
    }

    private void OnEnable()
    {
        _baseIntensity = _glowIntensity;
        
        if (_glowObject != null)
        {
            _glowObject.SetActive(true);
            if (_glowTexture == null || _glowSprite == null)
                GenerateGlowTexture();
            UpdateGlowSize();
            UpdateGlowColor();
        }
        else
        {
            CreateGlowObject();
        }
        
        _lastColor = _glowColor;
        _lastFalloff = _falloff;
        _lastInnerPadding = _innerPadding;
        _lastCornerRadius = _cornerRadius;
        _lastCornerSoftness = _cornerSoftness;
        _lastTextureSize = _textureSize;
    }

    private void OnDisable()
    {
        if (_glowObject != null)
            _glowObject.SetActive(false);
    }
    
    private void OnDestroy()
    {
        DestroyGlowObject();
    }

    private void LateUpdate()
    {
        if (_autoResize && _targetImage != null)
            CheckForSizeChanges();
        
        if (_enablePulse && Application.isPlaying)
            UpdatePulse();
        
        // UISkew 동기화
        SyncSkew();
    }
    
    private void SyncSkew()
    {
        if (_sourceSkew == null)
            _sourceSkew = GetComponent<UISkew>();
        
        if (_sourceSkew == null || _glowObject == null)
            return;
        
        // 글로우 오브젝트에 UISkew 값은 수동 조절
        if (_glowSkew == null)
        {
            _glowSkew = _glowObject.GetComponent<UISkew>();
            if (_glowSkew == null)
                _glowSkew = _glowObject.AddComponent<UISkew>();
        }
    }
    
    private void UpdatePulse()
    {
        float pulse = Mathf.Sin(Time.time * _pulseSpeed * Mathf.PI * 2f) * 0.5f + 0.5f;
        float intensity = _baseIntensity * (1f - _pulseAmount + pulse * _pulseAmount * 2f);
        
        if (_glowImage != null)
        {
            Color color = _glowColor;
            color.a *= Mathf.Clamp01(intensity);
            _glowImage.color = color;
        }
    }

    private void CheckForSizeChanges()
    {
        Vector2 currentSize = _targetImage.rectTransform.rect.size;
        if (_lastSize != currentSize)
        {
            _lastSize = currentSize;
            UpdateGlowSize();
        }
    }

    private void CreateGlowObject()
    {
        if (_targetImage == null)
            _targetImage = GetComponent<Image>();
            
        if (_targetImage == null || transform.parent == null)
            return;

        TryFindExistingGlow();

        if (_glowObject == null)
        {
            CleanupExistingGlowSiblings();
            
            _glowObject = new GameObject(GlowObjectName);
            _glowObject.transform.SetParent(transform.parent, false);
            
            int myIndex = transform.GetSiblingIndex();
            _glowObject.transform.SetSiblingIndex(myIndex);

            _glowRect = _glowObject.AddComponent<RectTransform>();
            _glowImage = _glowObject.AddComponent<Image>();
            _glowImage.raycastTarget = false;
        }

        RectTransform myRect = _targetImage.rectTransform;
        _glowRect.anchorMin = myRect.anchorMin;
        _glowRect.anchorMax = myRect.anchorMax;
        _glowRect.pivot = myRect.pivot;
        _glowRect.anchoredPosition = myRect.anchoredPosition + _glowOffset;
        _glowRect.sizeDelta = myRect.sizeDelta;

        if (_glowTexture == null || _glowSprite == null)
            GenerateGlowTexture();
        
        UpdateGlowSize();
        UpdateGlowColor();
    }
    
    private void CleanupExistingGlowSiblings()
    {
        if (transform.parent == null)
            return;
            
        string glowName = GlowObjectName;
        Transform parent = transform.parent;
        int myIndex = transform.GetSiblingIndex();
        
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform sibling = parent.GetChild(i);
            if (sibling == transform)
                continue;
                
            bool isMyGlow = sibling.name == glowName;
            bool isOldGlow = sibling.name == "ImageGlow" && i == myIndex - 1;
            
            if (isMyGlow || isOldGlow)
            {
                if (Application.isPlaying)
                    Destroy(sibling.gameObject);
                else
                    DestroyImmediate(sibling.gameObject);
            }
        }
    }

    private void GenerateGlowTexture()
    {
        if (_glowTexture != null)
        {
            if (Application.isPlaying)
                Destroy(_glowTexture);
            else
                DestroyImmediate(_glowTexture);
        }
        
        if (_glowSprite != null)
        {
            if (Application.isPlaying)
                Destroy(_glowSprite);
            else
                DestroyImmediate(_glowSprite);
        }

        int size = _textureSize;
        _glowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        _glowTexture.wrapMode = TextureWrapMode.Clamp;
        _glowTexture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        float halfSize = size / 2f;
        float innerSize = 0.75f - _innerPadding;
        float effectiveRadius = _cornerRadius;
        float fadeRange = 0.08f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - halfSize) / halfSize;
                float ny = (y - halfSize) / halfSize;
                float dist = RoundedRectSDF(nx, ny, innerSize, innerSize, effectiveRadius);
                
                float alpha = 0f;

                float glowRange = 1f - innerSize;
                
                if (dist < -fadeRange)
                {
                    alpha = 0f;
                }
                else if (dist < 0f)
                {
                    float fadeIn = (dist + fadeRange) / fadeRange; // 0 ~ 1
                    fadeIn = fadeIn * fadeIn * (3f - 2f * fadeIn); // smoothstep
                    alpha = fadeIn;
                }
                else
                {
                    float normalizedDist = dist / glowRange;
                    float gaussian = Mathf.Exp(-normalizedDist * normalizedDist * _falloff * 3f);
                    if (_cornerSoftness > 0f)
                    {
                        float absX = Mathf.Abs(nx);
                        float absY = Mathf.Abs(ny);
                        float diagonal = Mathf.Min(absX, absY) / (Mathf.Max(absX, absY) + 0.001f);
                        float cornerBoost = 1f + diagonal * _cornerSoftness * 0.5f;
                        gaussian *= cornerBoost;
                    }
                    
                    alpha = gaussian;
                }
                
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        _glowTexture.SetPixels(pixels);
        _glowTexture.Apply();

        _glowSprite = Sprite.Create(_glowTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);

        if (_glowImage != null)
            _glowImage.sprite = _glowSprite;
    }

    private float RoundedRectSDF(float x, float y, float halfWidth, float halfHeight, float radius)
    {
        // 최소 둥글기 적용 (너무 각지면 부자연스러움)
        float effectiveRadius = Mathf.Max(radius, 0.01f);
        
        float qx = Mathf.Abs(x) - halfWidth + effectiveRadius;
        float qy = Mathf.Abs(y) - halfHeight + effectiveRadius;
        
        float outsideDist = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
        float insideDist = Mathf.Min(Mathf.Max(qx, qy), 0f);
        
        return outsideDist + insideDist - effectiveRadius;
    }

    private void UpdateGlowSize()
    {
        if (_glowRect == null || _targetImage == null)
            return;

        RectTransform myRect = _targetImage.rectTransform;
        Vector2 imageSize = myRect.rect.size;
        Vector2 imagePivot = myRect.pivot;

        // 스큐 각도에 따른 수직 패딩 보정
        // 기울어진 모서리의 수직 거리가 glowSize가 되도록 축 정렬 패딩을 보정
        float glowPadX = _glowSize;
        float glowPadY = _glowSize;
        if (_sourceSkew != null && imageSize.x > 0f && imageSize.y > 0f)
        {
            float sx = _sourceSkew.SkewX;
            float sy = _sourceSkew.SkewY;
            if (sx != 0f)
                glowPadX = _glowSize * Mathf.Sqrt(imageSize.y * imageSize.y + sx * sx) / imageSize.y;
            if (sy != 0f)
                glowPadY = _glowSize * Mathf.Sqrt(imageSize.x * imageSize.x + sy * sy) / imageSize.x;
        }
        
        // 상하좌우 개별 보정 적용
        float totalLeft = glowPadX + _padLeft;
        float totalRight = glowPadX + _padRight;
        float totalTop = glowPadY + _padTop;
        float totalBottom = glowPadY + _padBottom;
        
        float width = imageSize.x + totalLeft + totalRight;
        float height = imageSize.y + totalTop + totalBottom;
        
        width = Mathf.Max(width, 10f);
        height = Mathf.Max(height, 10f);
        
        // 좌우/상하 비대칭 보정으로 인한 중심 이동
        float offsetX = (totalRight - totalLeft) * 0.5f;
        float offsetY = (totalTop - totalBottom) * 0.5f;
        
        // 이미지 중심 위치 계산 (로컬 좌표)
        Vector2 imageCenterOffset = new Vector2(imageSize.x * (0.5f - imagePivot.x), imageSize.y * (0.5f - imagePivot.y));
        
        // 글로우도 같은 중심에 위치하도록 설정
        Vector2 glowCenterOffset = new Vector2(width * (0.5f - imagePivot.x), height * (0.5f - imagePivot.y));
        
        // 중심 차이 보정
        Vector2 centerDiff = imageCenterOffset - glowCenterOffset;

        _glowRect.anchorMin = myRect.anchorMin;
        _glowRect.anchorMax = myRect.anchorMax;
        _glowRect.pivot = imagePivot;
        _glowRect.anchoredPosition = myRect.anchoredPosition + _glowOffset + centerDiff + new Vector2(offsetX, offsetY);
        _glowRect.sizeDelta = new Vector2(width, height);
        
        _lastSize = imageSize;
    }

    private void UpdateGlowColor()
    {
        if (_glowImage == null)
            return;

        Color finalColor = _glowColor;
        finalColor.a *= _glowIntensity;
        _glowImage.color = finalColor;
    }

    private void RegenerateTexture()
    {
        if (_glowImage != null)
            GenerateGlowTexture();
    }

    private void DestroyGlowObject()
    {
        if (_glowTexture != null)
        {
            if (Application.isPlaying)
                Destroy(_glowTexture);
            else
                SafeDestroyImmediate(_glowTexture);
            _glowTexture = null;
        }
        
        if (_glowSprite != null)
        {
            if (Application.isPlaying)
                Destroy(_glowSprite);
            else
                SafeDestroyImmediate(_glowSprite);
            _glowSprite = null;
        }

        if (_glowObject != null)
        {
            if (Application.isPlaying)
                Destroy(_glowObject);
            else
                SafeDestroyImmediate(_glowObject);
            _glowObject = null;
        }
        
        _glowImage = null;
        _glowRect = null;
    }
    
    private void SafeDestroyImmediate(Object obj)
    {
        if (obj == null) return;
        
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (obj != null)
                    DestroyImmediate(obj);
            };
            return;
        }
#endif
        DestroyImmediate(obj);
    }

    public void Refresh()
    {
        UpdateGlowSize();
        UpdateGlowColor();
    }
    
    public void Regenerate()
    {
        RegenerateTexture();
        UpdateGlowSize();
        UpdateGlowColor();
    }

    public void SetGlowColorImmediate(Color color)
    {
        _glowColor = color;
        UpdateGlowColor();
    }

    public void SetGlowIntensityImmediate(float intensity)
    {
        _glowIntensity = Mathf.Clamp01(intensity);
        _baseIntensity = _glowIntensity;
        UpdateGlowColor();
    }

    public void StartPulse()
    {
        _enablePulse = true;
        _baseIntensity = _glowIntensity;
    }

    public void StopPulse()
    {
        _enablePulse = false;
        UpdateGlowColor();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && isActiveAndEnabled)
            {
                if (_glowObject == null)
                {
                    CreateGlowObject();
                }
                else
                {
                    bool needRegenerate = false;
                    
                    if (_lastFalloff != _falloff)
                    {
                        _lastFalloff = _falloff;
                        needRegenerate = true;
                    }
                    
                    if (_lastInnerPadding != _innerPadding)
                    {
                        _lastInnerPadding = _innerPadding;
                        needRegenerate = true;
                    }
                    
                    if (_lastCornerRadius != _cornerRadius)
                    {
                        _lastCornerRadius = _cornerRadius;
                        needRegenerate = true;
                    }
                    
                    if (_lastCornerSoftness != _cornerSoftness)
                    {
                        _lastCornerSoftness = _cornerSoftness;
                        needRegenerate = true;
                    }
                    
                    if (_lastTextureSize != _textureSize)
                    {
                        _lastTextureSize = _textureSize;
                        needRegenerate = true;
                    }
                    
                    if (needRegenerate)
                        RegenerateTexture();

                    UpdateGlowSize();
                    UpdateGlowColor();
                }
            }
        };
    }
#endif
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(UIImageGlow))]
public class UIImageGlowEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        UnityEditor.EditorGUILayout.LabelField("글로우 설정", UnityEditor.EditorStyles.boldLabel);
        UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_glowColor"), new GUIContent("글로우 색상"));
        UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_glowIntensity"), new GUIContent("글로우 강도"));
        UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_glowSize"), new GUIContent("글로우 크기"));
        UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_glowOffset"), new GUIContent("글로우 오프셋"));
        
        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("글로우 크기 보정 (상하좌우)", UnityEditor.EditorStyles.boldLabel);
        UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_padLeft"), new GUIContent("왼쪽"));
        UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_padRight"), new GUIContent("오른쪽"));
        UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_padTop"), new GUIContent("위"));
        UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_padBottom"), new GUIContent("아래"));
        
        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("글로우 모양", UnityEditor.EditorStyles.boldLabel);
        UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_falloff"), new GUIContent("감쇠 (Falloff)", "글로우가 얼마나 빨리 흐려지는지"));
        UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_innerPadding"), new GUIContent("내부 여백", "이미지와 글로우 시작점 사이 간격"));
        UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_cornerRadius"), new GUIContent("모서리 둥글기", "낮을수록 각진 사각형, 높을수록 둥글게"));
        UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_cornerSoftness"), new GUIContent("모서리 밝기 보정", "대각선 모서리 글로우 밝기 보정"));
        
        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("애니메이션", UnityEditor.EditorStyles.boldLabel);
        var enablePulse = serializedObject.FindProperty("_enablePulse");
        UnityEditor.EditorGUILayout.PropertyField(enablePulse, new GUIContent("펄스 활성화"));
        if (enablePulse.boolValue)
        {
            UnityEditor.EditorGUI.indentLevel++;
            UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_pulseSpeed"), new GUIContent("펄스 속도"));
            UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_pulseAmount"), new GUIContent("펄스 양"));
            UnityEditor.EditorGUI.indentLevel--;
        }
        
        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("옵션", UnityEditor.EditorStyles.boldLabel);
        UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_autoResize"), new GUIContent("자동 크기 조절"));
        UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("_textureSize"), new GUIContent("텍스처 크기"));
        
        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("스큐 동기화 (UISkew 사용시)", UnityEditor.EditorStyles.boldLabel);
        var debugSkew = serializedObject.FindProperty("_debugSkew");
        if (debugSkew != null)
            UnityEditor.EditorGUILayout.PropertyField(debugSkew, 
                new GUIContent("스큐 디버그", "Console에 스큐 계산값 출력"));
        
        UnityEditor.EditorGUILayout.Space();
        if (GUILayout.Button("글로우 재생성"))
        {
            var glow = (UIImageGlow)target;
            glow.Regenerate();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
