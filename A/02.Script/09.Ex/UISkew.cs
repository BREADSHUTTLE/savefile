using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/UI Skew")]
[RequireComponent(typeof(Graphic))]
public class UISkew : BaseMeshEffect
{
    [Header("기울기 (픽셀)")]
    [SerializeField] [Range(-500f, 500f)] private float _skewX = 0f;
    [SerializeField] [Range(-500f, 500f)] private float _skewY = 0f;

    public float SkewX
    {
        get => _skewX;
        set
        {
            _skewX = value;
            graphic.SetVerticesDirty();
        }
    }

    public float SkewY
    {
        get => _skewY;
        set
        {
            _skewY = value;
            graphic.SetVerticesDirty();
        }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0)
            return;

        if (_skewX == 0f && _skewY == 0f)
            return;

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

        float centerX = (minX + maxX) * 0.5f;
        float centerY = (minY + maxY) * 0.5f;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            float nx = height > 0 ? (vertex.position.y - minY) / height : 0.5f;
            float ny = width > 0 ? (vertex.position.x - minX) / width : 0.5f;
            
            float offsetX = _skewX * (nx - 0.5f);
            float offsetY = _skewY * (ny - 0.5f);
            
            vertex.position.x += offsetX;
            vertex.position.y += offsetY;
            
            vh.SetUIVertex(vertex, i);
        }
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
