using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Image))]
[AddComponentMenu("UI/Effects/UI Drop Shadow")]
public class UIDropShadow : MonoBehaviour
{
    [Header("그림자 설정")]
    [SerializeField] private Color _shadowColor = new Color(0, 0, 0, 0.5f);
    [SerializeField] private Vector2 _shadowOffset = new Vector2(5f, -5f);
    
    [Header("크기 설정")]
    [SerializeField] private Vector2 _sizeOffset = new Vector2(10f, 10f);  // 픽셀 단위 크기 조절
    
    [Header("블러 설정")]
    [SerializeField] [Range(0, 20)] private int _blurRadius = 5;
    [SerializeField] [Range(1, 3)] private int _blurIterations = 1;

    private Image _sourceImage;
    private RectTransform _sourceRect;
    
    private GameObject _shadowObject;
    private Image _shadowImage;
    private RectTransform _shadowRect;
    private Texture2D _shadowTexture;
    private Sprite _shadowSprite;
    
    // 변경 감지용
    private Sprite _lastSourceSprite;
    private Image.Type _lastSourceType;
    private Color _lastShadowColor;
    private Vector2 _lastShadowOffset;
    private Vector2 _lastSizeOffset;
    private int _lastBlurRadius;
    private int _lastBlurIterations;
    private Vector2 _lastSize;

    #region Properties
    public Color ShadowColor
    {
        get => _shadowColor;
        set { _shadowColor = value; UpdateShadowColor(); }
    }
    
    public Vector2 ShadowOffset
    {
        get => _shadowOffset;
        set { _shadowOffset = value; UpdateShadowTransform(); }
    }
    
    public Vector2 SizeOffset
    {
        get => _sizeOffset;
        set { _sizeOffset = value; UpdateShadowTransform(); }
    }
    
    public int BlurRadius
    {
        get => _blurRadius;
        set { _blurRadius = Mathf.Clamp(value, 0, 20); GenerateBlurredSprite(); }
    }
    #endregion

    private void Awake()
    {
        _sourceImage = GetComponent<Image>();
        _sourceRect = GetComponent<RectTransform>();
        
        // 기존에 있는 shadow 오브젝트 찾아서 재활용
        TryFindExistingShadow();
    }
    
    private void TryFindExistingShadow()
    {
        if (_shadowObject != null || transform.parent == null)
            return;
            
        string shadowName = "DropShadow_" + gameObject.name;
        Transform parent = transform.parent;
        
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform sibling = parent.GetChild(i);
            if (sibling.name == shadowName && sibling != transform)
            {
                _shadowObject = sibling.gameObject;
                _shadowRect = sibling.GetComponent<RectTransform>();
                _shadowImage = sibling.GetComponent<Image>();
                
                // 스프라이트에서 텍스처 참조 복원
                if (_shadowImage != null && _shadowImage.sprite != null)
                {
                    _shadowSprite = _shadowImage.sprite;
                    _shadowTexture = _shadowSprite.texture;
                }
                break;
            }
        }
    }

    private void OnEnable()
    {
        if (_shadowObject != null)
        {
            _shadowObject.SetActive(true);
            // 텍스처가 없을 때만 재생성 (재활용)
            if (_shadowTexture == null || _shadowSprite == null)
                GenerateBlurredSprite();
            UpdateShadowTransform();
            UpdateShadowColor();
        }
        else
        {
            CreateShadow();
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
        {
            EditorLateUpdate();
            return;
        }
#endif
        RuntimeLateUpdate();
    }
    
    private void RuntimeLateUpdate()
    {
        if (_shadowRect == null || _sourceRect == null)
            return;
        
        Vector2 currentSize = _sourceRect.rect.size;
        if (currentSize != _lastSize)
        {
            _lastSize = currentSize;
            UpdateShadowTransform();
        }
    }
    
#if UNITY_EDITOR
    private void EditorLateUpdate()
    {
        if (_shadowRect == null || _sourceImage == null)
            return;
            
        bool needRegenerateSprite = false;
        bool needUpdateTransform = false;
        bool needUpdateColor = false;
        
        if (_sourceImage.sprite != _lastSourceSprite || _sourceImage.type != _lastSourceType)
        {
            needRegenerateSprite = true;
            _lastSourceSprite = _sourceImage.sprite;
            _lastSourceType = _sourceImage.type;
        }

        if (_blurRadius != _lastBlurRadius || _blurIterations != _lastBlurIterations)
            needRegenerateSprite = true;
        
        Vector2 currentSize = _sourceRect.rect.size;
        if (currentSize != _lastSize || _sizeOffset != _lastSizeOffset || _shadowOffset != _lastShadowOffset)
        {
            needUpdateTransform = true;
            _lastSize = currentSize;
        }
        
        if (_shadowColor != _lastShadowColor)
            needUpdateColor = true;
        
        if (needRegenerateSprite)
        {
            GenerateBlurredSprite();
            SaveLastValues();
        }
        
        if (needUpdateColor)
        {
            UpdateShadowColor();
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
        _lastSourceSprite = _sourceImage != null ? _sourceImage.sprite : null;
        _lastSourceType = _sourceImage != null ? _sourceImage.type : Image.Type.Simple;
        _lastShadowColor = _shadowColor;
        _lastShadowOffset = _shadowOffset;
        _lastSizeOffset = _sizeOffset;
        _lastBlurRadius = _blurRadius;
        _lastBlurIterations = _blurIterations;
        _lastSize = _sourceRect != null ? _sourceRect.rect.size : Vector2.zero;
    }

    private void CreateShadow()
    {
        if (_sourceImage == null)
            _sourceImage = GetComponent<Image>();
        if (_sourceRect == null)
            _sourceRect = GetComponent<RectTransform>();
            
        if (_sourceImage == null || transform.parent == null)
            return;


        CleanupExistingShadowSiblings();
        
        DestroyShadow();

        _shadowObject = new GameObject("DropShadow_" + gameObject.name);
        _shadowObject.transform.SetParent(transform.parent, false);

        _shadowObject.transform.SetSiblingIndex(transform.GetSiblingIndex());

        _shadowRect = _shadowObject.AddComponent<RectTransform>();
        
        _shadowImage = _shadowObject.AddComponent<Image>();
        _shadowImage.raycastTarget = false;
        
        GenerateBlurredSprite();
        UpdateShadowTransform();
        UpdateShadowColor();
    }


    private void CleanupExistingShadowSiblings()
    {
        if (transform.parent == null)
            return;
            
        string shadowName = "DropShadow_" + gameObject.name;
        Transform parent = transform.parent;
        
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform sibling = parent.GetChild(i);
            if (sibling.name == shadowName && sibling != transform)
            {
                if (Application.isPlaying)
                    Destroy(sibling.gameObject);
                else
                    DestroyImmediate(sibling.gameObject);
            }
        }
    }
    
    private void DestroyShadow()
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

    private void GenerateBlurredSprite()
    {
        if (_sourceImage == null || _sourceImage.sprite == null || _shadowImage == null)
            return;
        
        CleanupTexture();
        
        Sprite sourceSprite = _sourceImage.sprite;
        Texture2D sourceTexture = GetReadableTexture(sourceSprite);
        
        if (sourceTexture == null)
        {
            _shadowImage.sprite = sourceSprite;
            _shadowImage.type = _sourceImage.type;
            return;
        }
        
        Rect spriteRect = sourceSprite.textureRect;
        int srcX = Mathf.RoundToInt(spriteRect.x);
        int srcY = Mathf.RoundToInt(spriteRect.y);
        int srcW = Mathf.RoundToInt(spriteRect.width);
        int srcH = Mathf.RoundToInt(spriteRect.height);

        int margin = _blurRadius * 2;
        int newW = srcW + margin * 2;
        int newH = srcH + margin * 2;

        float[,] alphaMap = new float[newW, newH];
        
        for (int y = 0; y < newH; y++)
        {
            for (int x = 0; x < newW; x++)
            {
                int texX = srcX + (x - margin);
                int texY = srcY + (y - margin);
                
                if (texX >= srcX && texX < srcX + srcW && texY >= srcY && texY < srcY + srcH)
                    alphaMap[x, y] = sourceTexture.GetPixel(texX, texY).a;
                else
                    alphaMap[x, y] = 0f;
            }
        }
        
        for (int i = 0; i < _blurIterations; i++)
            alphaMap = ApplyGaussianBlur(alphaMap, newW, newH, _blurRadius);
        
        _shadowTexture = new Texture2D(newW, newH, TextureFormat.RGBA32, false);
        _shadowTexture.wrapMode = TextureWrapMode.Clamp;
        _shadowTexture.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[newW * newH];
        for (int y = 0; y < newH; y++)
        {
            for (int x = 0; x < newW; x++)
            {
                float alpha = alphaMap[x, y];
                pixels[y * newW + x] = new Color(1, 1, 1, alpha);
            }
        }
        
        _shadowTexture.SetPixels(pixels);
        _shadowTexture.Apply();
        
        Vector4 sourceBorder = sourceSprite.border;
        Vector4 newBorder = new Vector4(
            sourceBorder.x + margin,  // left
            sourceBorder.y + margin,  // bottom
            sourceBorder.z + margin,  // right
            sourceBorder.w + margin   // top
        );
        
        _shadowSprite = Sprite.Create(
            _shadowTexture,
            new Rect(0, 0, newW, newH),
            new Vector2(0.5f, 0.5f),
            sourceSprite.pixelsPerUnit,
            0,
            SpriteMeshType.FullRect,
            newBorder
        );
        

        _shadowImage.sprite = _shadowSprite;
        _shadowImage.type = _sourceImage.type;  // 원본과 같은 타입 (Sliced 등)
        
        // 임시 텍스처 정리
        if (sourceTexture != _sourceImage.sprite.texture)
        {
            if (Application.isPlaying)
                Destroy(sourceTexture);
            else
                DestroyImmediate(sourceTexture);
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
    
    private float[,] ApplyGaussianBlur(float[,] input, int width, int height, int radius)
    {
        if (radius <= 0)
            return input;
            
        float[,] output = new float[width, height];
        
        int kernelSize = radius * 2 + 1;
        float[] kernel = new float[kernelSize];
        float sigma = radius / 2f;
        float sum = 0;
        
        for (int i = 0; i < kernelSize; i++)
        {
            int x = i - radius;
            kernel[i] = Mathf.Exp(-(x * x) / (2 * sigma * sigma));
            sum += kernel[i];
        }

        for (int i = 0; i < kernelSize; i++)
            kernel[i] /= sum;

        float[,] temp = new float[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    int sx = Mathf.Clamp(x + k, 0, width - 1);
                    value += input[sx, y] * kernel[k + radius];
                }
                temp[x, y] = value;
            }
        }
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    int sy = Mathf.Clamp(y + k, 0, height - 1);
                    value += temp[x, sy] * kernel[k + radius];
                }
                output[x, y] = value;
            }
        }
        
        return output;
    }

    private void UpdateShadowColor()
    {
        if (_shadowImage == null)
            return;
            
        _shadowImage.color = _shadowColor;
    }

    private void UpdateShadowTransform()
    {
        if (_shadowRect == null || _sourceRect == null)
            return;
        
        // 원본의 실제 크기 가져오기
        Vector2 actualSize = _sourceRect.rect.size;
        Vector2 shadowSize = actualSize + _sizeOffset;
        
        _shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
        _shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
        _shadowRect.pivot = new Vector2(0.5f, 0.5f);
        
        _shadowRect.sizeDelta = shadowSize;
        
        Vector3 sourceCenter = _sourceRect.TransformPoint(_sourceRect.rect.center);
        _shadowRect.position = sourceCenter;
        _shadowRect.anchoredPosition += _shadowOffset;

        _shadowRect.localRotation = _sourceRect.localRotation;
        _shadowRect.localScale = _sourceRect.localScale;
    }
    public void Refresh()
    {
        GenerateBlurredSprite();
        UpdateShadowColor();
        UpdateShadowTransform();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && isActiveAndEnabled)
            {
                if (_sourceImage == null) _sourceImage = GetComponent<Image>();
                if (_sourceRect == null) _sourceRect = GetComponent<RectTransform>();
                
                if (_shadowObject == null)
                {
                    CreateShadow();
                }
                else
                {
                    bool needRegenerate = _blurRadius != _lastBlurRadius || 
                                         _blurIterations != _lastBlurIterations;
                    
                    if (needRegenerate)
                        GenerateBlurredSprite();
                    
                    UpdateShadowColor();
                    UpdateShadowTransform();
                }
                
                SaveLastValues();
            }
        };
    }
#endif
}
