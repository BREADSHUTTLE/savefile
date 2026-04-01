using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Image))]
[AddComponentMenu("UI/Effects/UI Triangle Shadow")]
public class UITriangleShadow : MonoBehaviour
{
    [Header("삼각형 설정")]
    [SerializeField] private Color _shadowColor = new Color(0, 0, 0, 0.3f);
    [SerializeField] [Range(0f, 1f)] private float _shadowIntensity = 0.5f;
    
    [Header("모양 설정")]
    [SerializeField] [Range(-45f, 45f)] private float _diagonalAngle = 0f;
    [SerializeField] [Range(-0.5f, 0.5f)] private float _diagonalOffset = 0f;
    [SerializeField] [Range(0f, 0.5f)] private float _softness = 0.02f;
    
    [Header("크기 설정")]
    [SerializeField] private Vector2 _scale = new Vector2(0f, 0.02f);  // X, Y 축소 비율 (0 = 원본, 0.5 = 50% 축소)
    [SerializeField] private Vector2 _offset = Vector2.zero;  // 위치 오프셋
    [SerializeField] [Range(1, 4)] private int _quality = 2;  // 텍스처 품질 배수 (높을수록 부드러움)
    
    [Header("하이라이트")]
    [SerializeField] private bool _enableHighlight = true;
    [SerializeField] private bool _highlightBrightSide = true;   // 밝은 쪽 (위) 하이라이트
    [SerializeField] private bool _highlightDarkSide = true;     // 어두운 쪽 (아래) 하이라이트
    [SerializeField] private Color _highlightColor = new Color(1, 1, 1, 0.3f);
    [SerializeField] [Range(0.01f, 0.5f)] private float _highlightWidth = 0.03f;  // 더 넓게 퍼질 수 있도록 범위 확대
    [SerializeField] [Range(0.5f, 3f)] private float _highlightFalloff = 1f;  // 퍼짐 정도 조절
    [SerializeField] [Range(0.01f, 0.5f)] private float _highlightFadeOut = 0.1f;  // 끈 쪽 페이드아웃 거리

    private Image _parentImage;
    private RectTransform _parentRect;
    private GameObject _shadowObject;
    private Image _shadowImage;
    private RectTransform _shadowRect;
    private Texture2D _shadowTexture;
    private Sprite _shadowSprite;
    
    // 변경 감지용
    private Color _lastShadowColor;
    private float _lastIntensity;
    private float _lastSoftness;
    private bool _lastEnableHighlight;
    private bool _lastHighlightBrightSide;
    private bool _lastHighlightDarkSide;
    private Color _lastHighlightColor;
    private float _lastHighlightWidth;
    private float _lastHighlightFalloff;
    private float _lastHighlightFadeOut;
    private float _lastDiagonalAngle;
    private float _lastDiagonalOffset;
    private Vector2 _lastScale;
    private int _lastQuality;
    private Vector2 _lastOffset;
    private Sprite _lastSourceSprite;
    private Vector2 _lastSize;

    #region Properties
    public Color ShadowColor
    {
        get => _shadowColor;
        set { _shadowColor = value; GenerateShadowTexture(); }
    }
    
    public float ShadowIntensity
    {
        get => _shadowIntensity;
        set { _shadowIntensity = Mathf.Clamp01(value); GenerateShadowTexture(); }
    }
    
    public float DiagonalAngle
    {
        get => _diagonalAngle;
        set { _diagonalAngle = value; GenerateShadowTexture(); }
    }
    
    public float DiagonalOffset
    {
        get => _diagonalOffset;
        set { _diagonalOffset = value; GenerateShadowTexture(); }
    }
    
    public Vector2 Scale
    {
        get => _scale;
        set { _scale = new Vector2(Mathf.Clamp(value.x, 0f, 0.5f), Mathf.Clamp(value.y, 0f, 0.5f)); UpdateShadowTransform(); }
    }
    
    public float HighlightWidth
    {
        get => _highlightWidth;
        set { _highlightWidth = Mathf.Clamp(value, 0.01f, 0.5f); GenerateShadowTexture(); }
    }
    
    public float HighlightFalloff
    {
        get => _highlightFalloff;
        set { _highlightFalloff = Mathf.Clamp(value, 0.5f, 3f); GenerateShadowTexture(); }
    }
    #endregion

    private void Awake()
    {
        _parentImage = GetComponent<Image>();
        _parentRect = GetComponent<RectTransform>();
        
        // 기존에 있는 shadow 오브젝트 찾아서 재활용
        TryFindExistingShadow();
    }
    
    private void TryFindExistingShadow()
    {
        if (_shadowObject != null)
            return;
            
        Transform existing = transform.Find("TriangleShadow");
        if (existing != null)
        {
            _shadowObject = existing.gameObject;
            _shadowRect = existing.GetComponent<RectTransform>();
            _shadowImage = existing.GetComponent<Image>();
            
            // 스프라이트에서 텍스처 참조 복원
            if (_shadowImage != null && _shadowImage.sprite != null)
            {
                _shadowSprite = _shadowImage.sprite;
                _shadowTexture = _shadowSprite.texture;
            }
        }
    }

    private void OnEnable()
    {
        // 프리팹 모드에서 Awake가 호출 안 될 수 있으므로 여기서도 찾기
        if (_shadowObject == null)
            TryFindExistingShadow();
            
        if (_shadowObject != null)
        {
            _shadowObject.SetActive(true);
            // 텍스처가 없을 때만 재생성 (재활용)
            if (_shadowTexture == null || _shadowSprite == null)
                GenerateShadowTexture();
            UpdateShadowTransform();
        }
        else
        {
            CreateShadowObject();
        }
        SaveLastValues();
    }

    private void OnDisable()
    {
        if (_shadowObject != null)
            _shadowObject.SetActive(false);
    }
    
    private void OnDestroy()
    {
        CleanupTexture();
        _shadowObject = null;
        _shadowImage = null;
        _shadowRect = null;
    }

    private void LateUpdate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorLateUpdate();
#endif
    }
    
#if UNITY_EDITOR
    private void EditorLateUpdate()
    {
        if (_shadowRect == null || _parentImage == null)
            return;
            
        bool needRegenerate = false;
        bool needUpdateTransform = false;

        if (_parentImage.sprite != _lastSourceSprite)
        {
            needRegenerate = true;
            _lastSourceSprite = _parentImage.sprite;
        }

        Vector2 currentSize = _parentRect.rect.size;
        if (currentSize != _lastSize)
        {
            needRegenerate = true;  // 크기 변하면 텍스처도 재생성
            _lastSize = currentSize;
        }

        if (_scale != _lastScale || _offset != _lastOffset)
        {
            needUpdateTransform = true;
            _lastScale = _scale;
            _lastOffset = _offset;
        }
        
        if (needRegenerate)
        {
            GenerateShadowTexture();
            SaveLastValues();
        }
        
        if (needUpdateTransform)
        {
            UpdateShadowTransform();
            SaveLastValues();
        }
    }
#endif

    private void SaveLastValues()
    {
        _lastShadowColor = _shadowColor;
        _lastIntensity = _shadowIntensity;
        _lastSoftness = _softness;
        _lastEnableHighlight = _enableHighlight;
        _lastHighlightBrightSide = _highlightBrightSide;
        _lastHighlightDarkSide = _highlightDarkSide;
        _lastHighlightColor = _highlightColor;
        _lastHighlightWidth = _highlightWidth;
        _lastHighlightFalloff = _highlightFalloff;
        _lastHighlightFadeOut = _highlightFadeOut;
        _lastDiagonalAngle = _diagonalAngle;
        _lastDiagonalOffset = _diagonalOffset;
        _lastScale = _scale;
        _lastQuality = _quality;
        _lastOffset = _offset;
        _lastSourceSprite = _parentImage != null ? _parentImage.sprite : null;
        _lastSize = _parentRect != null ? _parentRect.rect.size : Vector2.zero;
    }

    private void CreateShadowObject()
    {
        if (_parentImage == null)
            _parentImage = GetComponent<Image>();
        if (_parentRect == null)
            _parentRect = GetComponent<RectTransform>();
            
        if (_parentImage == null)
            return;

        // 기존 오브젝트 먼저 찾아서 재활용
        TryFindExistingShadow();
        
        if (_shadowObject != null)
        {
            // 기존 오브젝트 있으면 재활용
            if (_shadowTexture == null || _shadowSprite == null)
                GenerateShadowTexture();
            UpdateShadowTransform();
            return;
        }

        // 중복 정리 후 새로 생성
        CleanupExistingShadowChildren();

        _shadowObject = new GameObject("TriangleShadow");
        _shadowObject.transform.SetParent(transform, false);
        _shadowObject.transform.SetAsFirstSibling();

        _shadowRect = _shadowObject.AddComponent<RectTransform>();

        _shadowRect.anchorMin = Vector2.zero;
        _shadowRect.anchorMax = Vector2.one;
        _shadowRect.offsetMin = Vector2.zero;
        _shadowRect.offsetMax = Vector2.zero;
        _shadowRect.pivot = new Vector2(0.5f, 0.5f);

        _shadowImage = _shadowObject.AddComponent<Image>();
        _shadowImage.raycastTarget = false;

        GenerateShadowTexture();
        UpdateShadowTransform();
    }

    private void GenerateShadowTexture()
    {
        if (_parentImage == null || _parentImage.sprite == null || _shadowImage == null)
            return;
            
        CleanupTexture();

        Sprite sourceSprite = _parentImage.sprite;
        Texture2D sourceTexture = GetReadableTexture(sourceSprite);
        
        if (sourceTexture == null)
            return;

        Rect spriteRect = sourceSprite.textureRect;
        int srcX = Mathf.RoundToInt(spriteRect.x);
        int srcY = Mathf.RoundToInt(spriteRect.y);
        int srcW = Mathf.RoundToInt(spriteRect.width);
        int srcH = Mathf.RoundToInt(spriteRect.height);
        
        // 버튼의 실제 렌더링 크기로 텍스처 생성 (대각선 왜곡 방지)
        // quality 배수로 해상도 증가 (계단 현상 감소)
        int texW = Mathf.Max(64, Mathf.RoundToInt(_parentRect.rect.width * _quality));
        int texH = Mathf.Max(64, Mathf.RoundToInt(_parentRect.rect.height * _quality));
        
        _shadowTexture = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        _shadowTexture.wrapMode = TextureWrapMode.Clamp;
        _shadowTexture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[texW * texH];
        Vector4 border = sourceSprite.border;
        
        float angleRad = _diagonalAngle * Mathf.Deg2Rad;
        float cosA = Mathf.Cos(angleRad);
        float sinA = Mathf.Sin(angleRad);
        
        for (int y = 0; y < texH; y++)
        {
            for (int x = 0; x < texW; x++)
            {
                float uvX = (float)x / (texW - 1);
                float uvY = (float)y / (texH - 1);
                
                float srcUvX = GetSlicedUV(uvX, border.x, border.z, srcW, _parentRect.rect.width);
                float srcUvY = GetSlicedUV(uvY, border.y, border.w, srcH, _parentRect.rect.height);
                
                float sampleXf = srcUvX * (srcW - 1);
                float sampleYf = srcUvY * (srcH - 1);
                float sourceAlpha = SampleAlphaBilinear(sourceTexture, srcX, srcY, srcW, srcH, sampleXf, sampleYf);
                
                float nx = uvX - 0.5f;
                float ny = uvY - 0.5f;
                
                float rx = nx * cosA - ny * sinA;
                float ry = nx * sinA + ny * cosA;
                float diag = rx + ry + _diagonalOffset;
                
                float shadowAlpha = 0f;
                float highlightAlpha = 0f;

                if (diag < 0)
                {
                    if (_softness < 0.001f)
                        shadowAlpha = 1f;
                    else
                        shadowAlpha = Mathf.Clamp01(-diag / _softness);
                }

                if (_enableHighlight)
                {
                    float distToLine = Mathf.Abs(diag);
                    if (distToLine < _highlightWidth)
                    {
                        float t = distToLine / _highlightWidth;
                        highlightAlpha = Mathf.Pow(1f - t, _highlightFalloff);
                        
                        if (!_highlightBrightSide && diag > 0)
                        {
                            float fadeT = diag / _highlightFadeOut;
                            highlightAlpha *= Mathf.Clamp01(1f - fadeT);
                        }
                        else if (!_highlightDarkSide && diag < 0)
                        {
                            float fadeT = -diag / _highlightFadeOut;
                            highlightAlpha *= Mathf.Clamp01(1f - fadeT);
                        }
                    }
                }
                
                Color finalColor = Color.clear;
                
                if (shadowAlpha > 0.01f)
                {
                    finalColor = new Color(
                        _shadowColor.r,
                        _shadowColor.g,
                        _shadowColor.b,
                        shadowAlpha * _shadowColor.a * _shadowIntensity
                    );
                }
                
                if (highlightAlpha > 0.01f && _enableHighlight)
                {
                    float hlA = highlightAlpha * _highlightColor.a;
                    
                    finalColor.r = Mathf.Lerp(finalColor.r, _highlightColor.r, highlightAlpha);
                    finalColor.g = Mathf.Lerp(finalColor.g, _highlightColor.g, highlightAlpha);
                    finalColor.b = Mathf.Lerp(finalColor.b, _highlightColor.b, highlightAlpha);
                    finalColor.a = Mathf.Max(finalColor.a, hlA);
                }

                finalColor.a *= sourceAlpha;
                
                pixels[y * texW + x] = finalColor;
            }
        }

        _shadowTexture.SetPixels(pixels);
        _shadowTexture.Apply();

        _shadowSprite = Sprite.Create(
            _shadowTexture, 
            new Rect(0, 0, texW, texH), 
            new Vector2(0.5f, 0.5f), 
            100f
        );

        _shadowImage.sprite = _shadowSprite;
        _shadowImage.type = Image.Type.Simple;  // 대각선 왜곡 방지
        _shadowImage.color = Color.white;
        
        if (sourceTexture != _parentImage.sprite.texture)
        {
            if (Application.isPlaying)
                Destroy(sourceTexture);
            else
                DestroyImmediate(sourceTexture);
        }
    }

    private float SampleAlphaBilinear(Texture2D tex, int srcX, int srcY, int srcW, int srcH, float x, float y)
    {
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        int x1 = x0 + 1;
        int y1 = y0 + 1;
        
        float tx = x - x0;
        float ty = y - y0;
        
        x0 = Mathf.Clamp(x0, 0, srcW - 1);
        x1 = Mathf.Clamp(x1, 0, srcW - 1);
        y0 = Mathf.Clamp(y0, 0, srcH - 1);
        y1 = Mathf.Clamp(y1, 0, srcH - 1);

        float a00 = tex.GetPixel(srcX + x0, srcY + y0).a;
        float a10 = tex.GetPixel(srcX + x1, srcY + y0).a;
        float a01 = tex.GetPixel(srcX + x0, srcY + y1).a;
        float a11 = tex.GetPixel(srcX + x1, srcY + y1).a;

        float a0 = Mathf.Lerp(a00, a10, tx);
        float a1 = Mathf.Lerp(a01, a11, tx);
        return Mathf.Lerp(a0, a1, ty);
    }
    
    private float GetSlicedUV(float renderUV, float borderLow, float borderHigh, float texSize, float renderSize)
    {
        if (borderLow + borderHigh < 1f)
            return renderUV;
        
        float renderBorderLow = borderLow / renderSize;
        float renderBorderHigh = borderHigh / renderSize;
        float texBorderLow = borderLow / texSize;
        float texBorderHigh = borderHigh / texSize;

        if (renderUV < renderBorderLow)
        {
            return renderUV * (texBorderLow / renderBorderLow);
        }
        else if (renderUV > 1f - renderBorderHigh)
        {
            float fromEnd = 1f - renderUV;
            float texFromEnd = fromEnd * (texBorderHigh / renderBorderHigh);
            return 1f - texFromEnd;
        }
        else
        {
            float innerRenderStart = renderBorderLow;
            float innerRenderEnd = 1f - renderBorderHigh;
            float innerTexStart = texBorderLow;
            float innerTexEnd = 1f - texBorderHigh;
            
            float t = (renderUV - innerRenderStart) / (innerRenderEnd - innerRenderStart);
            return innerTexStart + t * (innerTexEnd - innerTexStart);
        }
    }
    
    private Texture2D GetReadableTexture(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
            return null;
            
        Texture2D source = sprite.texture;
        
        try
        {
            source.GetPixel(0, 0);
            return source;
        }
        catch
        {
            RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height);
            Graphics.Blit(source, rt);
            
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            
            Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            readable.Apply();
            
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            
            return readable;
        }
    }

    private void UpdateShadowTransform()
    {
        if (_shadowRect == null || _parentImage == null)
            return;

        _shadowRect.anchorMin = Vector2.zero;
        _shadowRect.anchorMax = Vector2.one;
        _shadowRect.offsetMin = Vector2.zero;
        _shadowRect.offsetMax = Vector2.zero;
        
        float scaleX = 1f - _scale.x;
        float scaleY = 1f - _scale.y;
        _shadowRect.localScale = new Vector3(scaleX, scaleY, 1f);
        
        _shadowRect.localRotation = Quaternion.identity;
        _shadowRect.anchoredPosition = _offset;
    }

    private void CleanupTexture()
    {
        if (_shadowTexture != null)
        {
            if (Application.isPlaying)
                Destroy(_shadowTexture);
            else
                DestroyImmediate(_shadowTexture);
            _shadowTexture = null;
        }
        
        if (_shadowSprite != null)
        {
            if (Application.isPlaying)
                Destroy(_shadowSprite);
            else
                DestroyImmediate(_shadowSprite);
            _shadowSprite = null;
        }
    }

    private void CleanupExistingShadowChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == "TriangleShadow")
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }
    
    private void DestroyShadowObject()
    {
        CleanupTexture();

        if (_shadowObject != null)
        {
            if (Application.isPlaying)
                Destroy(_shadowObject);
            else
                DestroyImmediate(_shadowObject);
            _shadowObject = null;
        }
        
        _shadowImage = null;
        _shadowRect = null;
    }

    public void Refresh()
    {
        GenerateShadowTexture();
        UpdateShadowTransform();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && isActiveAndEnabled)
            {
                if (_shadowObject == null)
                {
                    CreateShadowObject();
                }
                else
                {
                    bool needRegenerate = false;
                    
                    if (_lastSoftness != _softness ||
                        _lastEnableHighlight != _enableHighlight ||
                        _lastHighlightBrightSide != _highlightBrightSide ||
                        _lastHighlightDarkSide != _highlightDarkSide ||
                        _lastHighlightColor != _highlightColor ||
                        _lastHighlightWidth != _highlightWidth ||
                        _lastHighlightFalloff != _highlightFalloff ||
                        _lastHighlightFadeOut != _highlightFadeOut ||
                        _lastDiagonalAngle != _diagonalAngle ||
                        _lastDiagonalOffset != _diagonalOffset ||
                        _lastQuality != _quality ||
                        _lastShadowColor != _shadowColor ||
                        _lastIntensity != _shadowIntensity)
                    {
                        needRegenerate = true;
                    }
                    
                    SaveLastValues();
                    
                    if (needRegenerate)
                        GenerateShadowTexture();

                    UpdateShadowTransform();
                }
            }
        };
    }
#endif
}
