using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[ExecuteInEditMode]
public class UIImageBlur : MonoBehaviour
{
    [Header("블러 설정")]
    [SerializeField] private Shader _blurShader;
    [SerializeField, Range(0f, 300f)]
    private float _blurSize = 30f;

    [Header("틴트 색상")]
    [SerializeField]
    private Color _tintColor = Color.white;

    private Image _image;
    private Material _blurMaterial;
    private Material _originalMaterial;

    private static readonly int BlurSizeId = Shader.PropertyToID("_BlurSize");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public float BlurSize
    {
        get => _blurSize;
        set
        {
            _blurSize = Mathf.Clamp(value, 0f, 300f);
            UpdateMaterial();
        }
    }

    public Color TintColor
    {
        get => _tintColor;
        set
        {
            _tintColor = value;
            UpdateMaterial();
        }
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        ApplyBlur();
    }

    private void OnDisable()
    {
        RemoveBlur();
    }

    private void OnDestroy()
    {
        if (_blurMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(_blurMaterial);
            else
                DestroyImmediate(_blurMaterial);
        }
    }

    private void Initialize()
    {
        if (_image == null)
            _image = GetComponent<Image>();

        if (_blurMaterial == null)
        {
            var shader = _blurShader != null ? _blurShader : Shader.Find("UI/ImageBlur");

            if (shader != null)
            {
                _blurMaterial = new Material(shader);
                _blurMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            else
            {
                Debug.LogError("UIImageBlur: UI/ImageBlur 셰이더를 찾을 수 없습니다!");
            }
        }
    }

    private void ApplyBlur()
    {
        if (_image == null || _blurMaterial == null)
            return;

        _originalMaterial = _image.material;
        _image.material = _blurMaterial;
        UpdateMaterial();
    }

    private void RemoveBlur()
    {
        if (_image != null && _originalMaterial != null)
            _image.material = _originalMaterial;
    }

    private void UpdateMaterial()
    {
        if (_blurMaterial == null)
            return;

        _blurMaterial.SetFloat(BlurSizeId, _blurSize);
        _blurMaterial.SetColor(ColorId, _tintColor);
    }

#if UNITY_EDITOR
    private void Reset()
    {
        _blurShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/02.Script/09.Ex/Shader/UIImageBlur.shader");
    }

    private void OnValidate()
    {
        if (_blurShader == null)
            _blurShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/02.Script/09.Ex/Shader/UIImageBlur.shader");
        
        if (_image != null && _blurMaterial != null)
            UpdateMaterial();
    }
#endif
}
