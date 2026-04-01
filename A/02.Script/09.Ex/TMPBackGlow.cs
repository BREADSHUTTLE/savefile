using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
[AddComponentMenu("UI/TMP Back Glow")]
public class TMPBackGlow : MonoBehaviour
{
    [Header("글로우 설정")]
    [SerializeField] private Color _glowColor = new Color(0.3f, 0.5f, 0.8f, 0.5f);
    [SerializeField] [Range(0.5f, 5f)] private float _glowScaleX = 2f;
    [SerializeField] [Range(0.5f, 10f)] private float _glowScaleY = 4f;
    [SerializeField] [Range(0f, 1f)] private float _glowIntensity = 0.6f;
    [SerializeField] private Vector2 _glowOffset = Vector2.zero;
    
    [Header("모양 설정")]
    [SerializeField] [Range(0f, 1f)] private float _softness = 0.7f;
    [SerializeField] private bool _ellipse = true;
    [SerializeField] [Range(0.3f, 3f)] private float _aspectRatio = 1.5f;
    
    [Header("옵션")]
    [SerializeField] private bool _autoResize = true;
    [SerializeField] [Range(64, 256)] private int _textureSize = 128;

    private TMP_Text _tmpText;
    [System.NonSerialized] internal GameObject _glowObject;
    private Image _glowImage;
    private RectTransform _glowRect;
    private Texture2D _glowTexture;
    private Sprite _glowSprite;
    
    private Vector2 _lastTextSize;
    private Color _lastColor;
    private float _lastSoftness;
    private bool _lastEllipse;
    private float _lastAspectRatio;
    private int _lastTextureSize;

    #region Properties
    public Color GlowColor
    {
        get => _glowColor;
        set { _glowColor = value; UpdateGlowColor(); }
    }
    
    public float GlowScaleX
    {
        get => _glowScaleX;
        set { _glowScaleX = value; UpdateGlowSize(); }
    }
    
    public float GlowScaleY
    {
        get => _glowScaleY;
        set { _glowScaleY = value; UpdateGlowSize(); }
    }
    
    public float GlowIntensity
    {
        get => _glowIntensity;
        set { _glowIntensity = value; UpdateGlowColor(); }
    }
    
    public float Softness
    {
        get => _softness;
        set { _softness = value; RegenerateTexture(); }
    }
    #endregion

    private string GlowObjectName => "BackGlow_" + gameObject.name;

    private void Awake()
    {
        _tmpText = GetComponent<TMP_Text>();
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
                
            // 새 이름 또는 기존 "BackGlow" 이름 (내 바로 앞에 있는 것)
            bool isMyGlow = sibling.name == glowName;
            bool isOldGlow = sibling.name == "BackGlow" && i == myIndex - 1;
            
            if (isMyGlow || isOldGlow)
            {
                _glowObject = sibling.gameObject;
                _glowRect = sibling.GetComponent<RectTransform>();
                _glowImage = sibling.GetComponent<Image>();
                
                // 새 이름으로 변경
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
        // 기존 Glow 오브젝트가 있으면 활성화, 없으면 생성
        if (_glowObject != null)
        {
            _glowObject.SetActive(true);
            // 텍스처가 없을 때만 재생성 (재활용)
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
        _lastSoftness = _softness;
        _lastEllipse = _ellipse;
        _lastAspectRatio = _aspectRatio;
        _lastTextureSize = _textureSize;
    }

    private void OnDisable()
    {
        // 삭제하지 않고 비활성화만
        if (_glowObject != null)
            _glowObject.SetActive(false);
    }
    
    private void OnDestroy()
    {
        // 실제 삭제는 OnDestroy에서만
        DestroyGlowObject();
    }

    private void LateUpdate()
    {
        if (_autoResize && _tmpText != null)
            CheckForSizeChanges();
    }

    private void CheckForSizeChanges()
    {
        Vector2 currentSize = _tmpText.rectTransform.rect.size;
        if (_lastTextSize != currentSize)
        {
            _lastTextSize = currentSize;
            UpdateGlowSize();
        }
    }

    private void CreateGlowObject()
    {
        if (_tmpText == null)
            _tmpText = GetComponent<TMP_Text>();
            
        if (_tmpText == null || transform.parent == null)
            return;

        // 기존 Glow 찾아서 재활용
        TryFindExistingGlow();

        // 없으면 새로 생성
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

        RectTransform myRect = _tmpText.rectTransform;
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
                
            // 새 이름 또는 기존 "BackGlow" 이름 (내 바로 앞에 있는 것만)
            bool isMyGlow = sibling.name == glowName;
            bool isOldGlow = sibling.name == "BackGlow" && i == myIndex - 1;
            
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
        float center = size / 2f;
        float maxDist = center;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                
                float dist;
                if (_ellipse)
                    dist = Mathf.Sqrt((dx * dx) / (_aspectRatio * _aspectRatio) + dy * dy);
                else
                    dist = Mathf.Sqrt(dx * dx + dy * dy);
                
                float alpha = 0f;
                if (dist < 1f)
                {
                    float falloff = 1f - _softness * 0.5f;
                    alpha = Mathf.Pow(1f - dist, falloff);
                    
                    alpha *= Mathf.Exp(-dist * dist * (2f - _softness));
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

    private void UpdateGlowSize()
    {
        if (_glowRect == null || _tmpText == null)
            return;

        RectTransform myRect = _tmpText.rectTransform;
        Vector2 textSize = myRect.rect.size;
        float width = textSize.x * _glowScaleX;
        float height = textSize.y * _glowScaleY;
        
        width = Mathf.Max(width, 50f);
        height = Mathf.Max(height, 50f);

        _glowRect.anchorMin = myRect.anchorMin;
        _glowRect.anchorMax = myRect.anchorMax;
        _glowRect.pivot = myRect.pivot;
        _glowRect.anchoredPosition = myRect.anchoredPosition + _glowOffset;
        _glowRect.sizeDelta = new Vector2(width, height);
        
        _lastTextSize = textSize;
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
        // 부모가 활성화/비활성화 중일 때는 delayCall로 지연 삭제
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

    public void SetGlowColorImmediate(Color color)
    {
        _glowColor = color;
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
                    
                    if (_lastSoftness != _softness)
                    {
                        _lastSoftness = _softness;
                        needRegenerate = true;
                    }
                    
                    if (_lastEllipse != _ellipse)
                    {
                        _lastEllipse = _ellipse;
                        needRegenerate = true;
                    }
                    
                    if (_lastAspectRatio != _aspectRatio)
                    {
                        _lastAspectRatio = _aspectRatio;
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
