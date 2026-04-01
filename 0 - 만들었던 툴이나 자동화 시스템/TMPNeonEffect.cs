using UnityEngine;
using TMPro;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
[AddComponentMenu("UI/TMP Neon Effect")]
public class TMPNeonEffect : MonoBehaviour
{
    [Header("글로우 설정")]
    [SerializeField] private Color _glowColor = new Color(0f, 1f, 0.8f, 0.5f);
    [SerializeField] [Range(-1f, 1f)] private float _glowOffsetX = 0f;
    [SerializeField] [Range(-1f, 1f)] private float _glowOffsetY = 0f;
    [SerializeField] [Range(-1f, 1f)] private float _glowDilate = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float _glowSoftness = 0.8f;

    private TMP_Text _tmpText;
    private Material _materialInstance;
    [SerializeField, HideInInspector] private Material _originalMaterial;
    
    // Material 캐싱용 static 딕셔너리
    private static Dictionary<int, Material> _cachedMaterials = new Dictionary<int, Material>();

    private static readonly int _UnderlayColor = Shader.PropertyToID("_UnderlayColor");
    private static readonly int _UnderlayOffsetX = Shader.PropertyToID("_UnderlayOffsetX");
    private static readonly int _UnderlayOffsetY = Shader.PropertyToID("_UnderlayOffsetY");
    private static readonly int _UnderlayDilate = Shader.PropertyToID("_UnderlayDilate");
    private static readonly int _UnderlaySoftness = Shader.PropertyToID("_UnderlaySoftness");
    private static readonly int _OutlineWidth = Shader.PropertyToID("_OutlineWidth");

    private void Awake()
    {
        _tmpText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        ApplyEffect();
    }

    private void OnDestroy()
    {
        RestoreOriginal();
        
        // 인스턴스 Material이 캐시되지 않은 것이면 파괴
        if (_materialInstance != null && !_cachedMaterials.ContainsValue(_materialInstance))
        {
            if (Application.isPlaying)
                Destroy(_materialInstance);
            else
                DestroyImmediate(_materialInstance);
        }
        _materialInstance = null;
        _originalMaterial = null;
    }
    
    private void RestoreOriginal()
    {
        if (_tmpText != null && _originalMaterial != null)
        {
            _tmpText.fontSharedMaterial = _originalMaterial;
        }
    }
    
    private int GetMaterialHash()
    {
        // 동일한 설정의 Material은 같은 해시를 가짐
        int hash = 17;
        if (_originalMaterial != null)
            hash = hash * 31 + _originalMaterial.GetInstanceID();
        hash = hash * 31 + _glowColor.GetHashCode();
        hash = hash * 31 + _glowOffsetX.GetHashCode();
        hash = hash * 31 + _glowOffsetY.GetHashCode();
        hash = hash * 31 + _glowDilate.GetHashCode();
        hash = hash * 31 + _glowSoftness.GetHashCode();
        return hash;
    }

    private void ApplyEffect()
    {
        if (_tmpText == null)
            _tmpText = GetComponent<TMP_Text>();
            
        if (_tmpText == null)
            return;

        if (_originalMaterial == null)
            _originalMaterial = _tmpText.fontSharedMaterial;
        
        if (_originalMaterial == null)
            return;
        
        // 캐시된 Material 확인
        int hash = GetMaterialHash();
        if (_cachedMaterials.TryGetValue(hash, out Material cachedMat) && cachedMat != null)
        {
            _materialInstance = cachedMat;
            _tmpText.fontSharedMaterial = _materialInstance;
            return;
        }
        
        // 새 Material 생성 (캐시에 없을 때만)
        _materialInstance = new Material(_originalMaterial);
        _materialInstance.name = _originalMaterial.name + " (Neon)";

        _materialInstance.DisableKeyword("OUTLINE_ON");
        _materialInstance.DisableKeyword("GLOW_ON");
        _materialInstance.DisableKeyword("UNDERLAY_INNER");
        _materialInstance.SetFloat(_OutlineWidth, 0f);

        _materialInstance.EnableKeyword("UNDERLAY_ON");
        _materialInstance.SetColor(_UnderlayColor, _glowColor);
        _materialInstance.SetFloat(_UnderlayOffsetX, _glowOffsetX);
        _materialInstance.SetFloat(_UnderlayOffsetY, _glowOffsetY);
        _materialInstance.SetFloat(_UnderlayDilate, _glowDilate);
        _materialInstance.SetFloat(_UnderlaySoftness, _glowSoftness);
        
        // 캐시에 저장
        _cachedMaterials[hash] = _materialInstance;

        _tmpText.fontSharedMaterial = _materialInstance;
    }

    private void CleanupMaterial()
    {
        RestoreOriginal();
        _materialInstance = null;
        _originalMaterial = null;
    }

    public void SetGlowColor(Color color)
    {
        _glowColor = color;
        // 색상이 바뀌면 새 Material이 필요하므로 다시 적용
        ApplyEffect();
    }

    public void Refresh()
    {
        ApplyEffect();
    }
    
    // 앱 종료 시 캐시 정리
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearCache()
    {
        foreach (var mat in _cachedMaterials.Values)
        {
            if (mat != null)
                DestroyImmediate(mat);
        }
        _cachedMaterials.Clear();
    }

    [ContextMenu("Material 원본으로 복원")]
    public void RestoreOriginalMaterial()
    {
        if (_tmpText == null)
            _tmpText = GetComponent<TMP_Text>();
            
        if (_tmpText != null)
        {
            var fontAsset = _tmpText.font;
            if (fontAsset != null && fontAsset.material != null)
            {
                _tmpText.fontSharedMaterial = fontAsset.material;
            }
        }
        
        _materialInstance = null;
        _originalMaterial = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!isActiveAndEnabled)
            return;
            
        UnityEditor.EditorApplication.delayCall -= DelayedApplyEffect;
        UnityEditor.EditorApplication.delayCall += DelayedApplyEffect;
    }
    
    private void DelayedApplyEffect()
    {
        if (this != null && isActiveAndEnabled)
            ApplyEffect();
    }
#endif
}
