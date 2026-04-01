using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[AddComponentMenu("UI/Effects/UI Gradient")]
[RequireComponent(typeof(Graphic))]
public class UIGradient : BaseMeshEffect
{
    public enum GradientDirection
    {
        TopToBottom,      // 위 > 아래
        BottomToTop,      // 아래 > 위
        LeftToRight,      // 왼쪽 > 오른쪽
        RightToLeft,      // 오른쪽 > 왼쪽
        TopLeftToBottomRight,   // 좌상단 > 우하단
        TopRightToBottomLeft,   // 우상단 > 좌하단
        BottomLeftToTopRight,   // 좌하단 > 우상단
        BottomRightToTopLeft    // 우하단 > 좌상단
    }

    public enum ColorMode
    {
        TwoColors,    // 시작 - 끝 2색
        MultiColors   // 다중 색상 (Gradient)
    }

    [Header("그라데이션 설정")]
    [SerializeField] private GradientDirection _direction = GradientDirection.TopToBottom;
    [SerializeField] private ColorMode _colorMode = ColorMode.TwoColors;
    
    [Header("2색 모드")]
    [SerializeField] private Color _colorStart = Color.white;
    [SerializeField] private Color _colorEnd = Color.black;
    
    [Header("다중 색상 모드")]
    [SerializeField] private Gradient _gradient;
    
    [Header("각도 조절 (대각선 전용)")]
    [SerializeField] [Range(0.1f, 3f)] private float _angleX = 1f;   // X축 가중치
    [SerializeField] [Range(0.1f, 3f)] private float _angleY = 1f;   // Y축 가중치
    
    [Header("강도 조절")]
    [SerializeField] [Range(0f, 1f)] private float _intensity = 1f;  // 그라데이션 강도 (0: 효과없음, 1: 최대)
    [SerializeField] [Range(0f, 2f)] private float _contrast = 1f;   // 대비 (0.5: 연하게, 1: 기본, 2: 진하게)
    
    [Header("블렌드 설정")]
    [SerializeField] private bool _multiplyMode = true;  // true: 곱하기, false: 덮어쓰기

    #region Properties
    public GradientDirection Direction
    {
        get => _direction;
        set { _direction = value; graphic.SetVerticesDirty(); }
    }

    public ColorMode Mode
    {
        get => _colorMode;
        set { _colorMode = value; graphic.SetVerticesDirty(); }
    }

    public Color ColorStart
    {
        get => _colorStart;
        set { _colorStart = value; graphic.SetVerticesDirty(); }
    }

    public Color ColorEnd
    {
        get => _colorEnd;
        set { _colorEnd = value; graphic.SetVerticesDirty(); }
    }

    public Gradient GradientColors
    {
        get
        {
            if (_gradient == null)
                InitializeDefaultGradient();
            return _gradient;
        }
        set { _gradient = value; graphic.SetVerticesDirty(); }
    }

    public float Intensity
    {
        get => _intensity;
        set { _intensity = Mathf.Clamp01(value); graphic.SetVerticesDirty(); }
    }

    public float Contrast
    {
        get => _contrast;
        set { _contrast = Mathf.Clamp(value, 0f, 2f); graphic.SetVerticesDirty(); }
    }

    public bool MultiplyMode
    {
        get => _multiplyMode;
        set { _multiplyMode = value; graphic.SetVerticesDirty(); }
    }

    public float AngleX
    {
        get => _angleX;
        set { _angleX = Mathf.Clamp(value, 0.1f, 3f); graphic.SetVerticesDirty(); }
    }

    public float AngleY
    {
        get => _angleY;
        set { _angleY = Mathf.Clamp(value, 0.1f, 3f); graphic.SetVerticesDirty(); }
    }
    #endregion

    private new void Reset()
    {
        InitializeDefaultGradient();
    }

    private void InitializeDefaultGradient()
    {
        if (_gradient == null)
            _gradient = new Gradient();
            
        GradientColorKey[] colorKeys = new GradientColorKey[2];
        colorKeys[0] = new GradientColorKey(Color.white, 0f);
        colorKeys[1] = new GradientColorKey(Color.gray, 1f);
        
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);
        
        _gradient.SetKeys(colorKeys, alphaKeys);
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0)
            return;

        if (_intensity <= 0f)
            return;

        if (_colorMode == ColorMode.MultiColors && GradientColors.colorKeys.Length > 2)
            ModifyMeshWithSubdivision(vh);
        else
            ModifyMeshSimple(vh);
    }

    private void ModifyMeshSimple(VertexHelper vh)
    {
        UIVertex vertex = new UIVertex();
        
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            minX = Mathf.Min(minX, vertex.position.x);
            maxX = Mathf.Max(maxX, vertex.position.x);
            minY = Mathf.Min(minY, vertex.position.y);
            maxY = Mathf.Max(maxY, vertex.position.y);
        }

        float width = maxX - minX;
        float height = maxY - minY;

        if (width <= 0 || height <= 0)
            return;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            ApplyGradientToVertex(ref vertex, minX, minY, width, height);
            vh.SetUIVertex(vertex, i);
        }
    }

    private void ModifyMeshWithSubdivision(VertexHelper vh)
    {
        List<UIVertex> stream = new List<UIVertex>();
        vh.GetUIVertexStream(stream);

        if (stream.Count == 0)
            return;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        
        foreach (var v in stream)
        {
            minX = Mathf.Min(minX, v.position.x);
            maxX = Mathf.Max(maxX, v.position.x);
            minY = Mathf.Min(minY, v.position.y);
            maxY = Mathf.Max(maxY, v.position.y);
        }

        float width = maxX - minX;
        float height = maxY - minY;

        if (width <= 0 || height <= 0)
            return;

        int colorKeyCount = GradientColors.colorKeys.Length;
        int subdivisionLevel = Mathf.Clamp(colorKeyCount - 1, 1, 4); // 1~4회

        List<UIVertex> currentStream = stream;
        
        for (int level = 0; level < subdivisionLevel; level++)
        {
            List<UIVertex> newStream = new List<UIVertex>();
            
            for (int i = 0; i < currentStream.Count; i += 3)
            {
                UIVertex v0 = currentStream[i];
                UIVertex v1 = currentStream[i + 1];
                UIVertex v2 = currentStream[i + 2];
                
                UIVertex m01 = LerpVertex(v0, v1, 0.5f);
                UIVertex m12 = LerpVertex(v1, v2, 0.5f);
                UIVertex m20 = LerpVertex(v2, v0, 0.5f);
                

                newStream.Add(v0);
                newStream.Add(m01);
                newStream.Add(m20);

                newStream.Add(m01);
                newStream.Add(v1);
                newStream.Add(m12);

                newStream.Add(m20);
                newStream.Add(m12);
                newStream.Add(v2);
                
                newStream.Add(m01);
                newStream.Add(m12);
                newStream.Add(m20);
            }
            
            currentStream = newStream;
        }

        vh.Clear();
        
        for (int i = 0; i < currentStream.Count; i++)
        {
            UIVertex v = currentStream[i];
            ApplyGradientToVertex(ref v, minX, minY, width, height);
            vh.AddVert(v);
        }

        for (int i = 0; i < currentStream.Count; i += 3)
            vh.AddTriangle(i, i + 1, i + 2);
    }

    private UIVertex LerpVertex(UIVertex a, UIVertex b, float t)
    {
        UIVertex result = new UIVertex();
        result.position = Vector3.Lerp(a.position, b.position, t);
        result.normal = Vector3.Lerp(a.normal, b.normal, t);
        result.tangent = Vector4.Lerp(a.tangent, b.tangent, t);
        result.color = Color32.Lerp(a.color, b.color, t);
        result.uv0 = Vector4.Lerp(a.uv0, b.uv0, t);
        result.uv1 = Vector4.Lerp(a.uv1, b.uv1, t);
        result.uv2 = Vector4.Lerp(a.uv2, b.uv2, t);
        result.uv3 = Vector4.Lerp(a.uv3, b.uv3, t);
        return result;
    }

    private void ApplyGradientToVertex(ref UIVertex vertex, float minX, float minY, float width, float height)
    {
        float nx = width > 0 ? (vertex.position.x - minX) / width : 0f;
        float ny = height > 0 ? (vertex.position.y - minY) / height : 0f;
        
        float t = CalculateGradientFactor(nx, ny);
        t = ApplyContrast(t);
        
        Color gradientColor = GetGradientColor(t);
        
        if (_multiplyMode)
        {
            if (_intensity < 1f)
                gradientColor = Color.Lerp(Color.white, gradientColor, _intensity);
            vertex.color = MultiplyColor(vertex.color, gradientColor);
        }
        else
        {
            Color32 original = vertex.color;
            vertex.color = gradientColor;
            vertex.color.a = (byte)(original.a * gradientColor.a * _intensity);
        }
    }

    private Color GetGradientColor(float t)
    {
        if (_colorMode == ColorMode.MultiColors)
            return GradientColors.Evaluate(t);

        return Color.Lerp(_colorStart, _colorEnd, t);
    }

    private float ApplyContrast(float t)
    {
        float centered = t - 0.5f;
        float adjusted = centered * _contrast;
        return Mathf.Clamp01(adjusted + 0.5f);
    }

    private float CalculateGradientFactor(float nx, float ny)
    {
        float maxDiag, diag;
        
        switch (_direction)
        {
            case GradientDirection.TopToBottom:
                return 1f - ny;
                
            case GradientDirection.BottomToTop:
                return ny;
                
            case GradientDirection.LeftToRight:
                return nx;
                
            case GradientDirection.RightToLeft:
                return 1f - nx;
                
            case GradientDirection.TopLeftToBottomRight:
                maxDiag = _angleX + _angleY;
                diag = (nx * _angleX) + ((1f - ny) * _angleY);
                return Mathf.Clamp01(diag / maxDiag);
                
            case GradientDirection.TopRightToBottomLeft:
                maxDiag = _angleX + _angleY;
                diag = ((1f - nx) * _angleX) + ((1f - ny) * _angleY);
                return Mathf.Clamp01(diag / maxDiag);
                
            case GradientDirection.BottomLeftToTopRight:
                maxDiag = _angleX + _angleY;
                diag = (nx * _angleX) + (ny * _angleY);
                return Mathf.Clamp01(diag / maxDiag);
                
            case GradientDirection.BottomRightToTopLeft:
                maxDiag = _angleX + _angleY;
                diag = ((1f - nx) * _angleX) + (ny * _angleY);
                return Mathf.Clamp01(diag / maxDiag);
                
            default:
                return 0f;
        }
    }

    private Color32 MultiplyColor(Color32 original, Color gradient)
    {
        return new Color32(
            (byte)(original.r * gradient.r),
            (byte)(original.g * gradient.g),
            (byte)(original.b * gradient.b),
            (byte)(original.a * gradient.a)
        );
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (graphic != null)
            graphic.SetVerticesDirty();
    }
#endif
}
