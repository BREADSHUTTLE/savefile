using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class UITrackRectUvToTexcoord1 : BaseMeshEffect
{
    Graphic _g;
    RectTransform _rt;

    Rect _lastRect;
    int _lastVertCount = -1;
    bool _hasLast;

    protected override void Awake()
    {
        base.Awake();
        _g = GetComponent<Graphic>();
        _rt = _g.rectTransform;
        _hasLast = false;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _hasLast = false;
        if (_g == null) _g = GetComponent<Graphic>();
        _g.SetVerticesDirty();
    }

    protected override void OnDisable()
    {
        _hasLast = false;
        base.OnDisable();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (TryGetComponent<Graphic>(out var g))
            g.SetVerticesDirty();
        _hasLast = false;
    }
#endif

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh == null) return;
        if (_rt == null)
            _rt = (_g != null) ? _g.rectTransform : GetComponent<Graphic>().rectTransform;

        Rect r = _rt.rect;
        int count = vh.currentVertCount;

        bool bypassCache =
#if UNITY_EDITOR
            !Application.isPlaying;
#else
            false;
#endif

        if (!bypassCache && _hasLast && count == _lastVertCount && r.Equals(_lastRect))
            return;

        _hasLast = true;
        _lastRect = r;
        _lastVertCount = count;

        float w = Mathf.Max(r.width, 0.00001f);
        float h = Mathf.Max(r.height, 0.00001f);
        float invW = 1f / w;
        float invH = 1f / h;

        UIVertex v = default;
        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref v, i);

            float u = (v.position.x - r.xMin) * invW;
            float v01 = (v.position.y - r.yMin) * invH;

            v.uv1 = new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v01));
            vh.SetUIVertex(v, i);
        }
    }
}
