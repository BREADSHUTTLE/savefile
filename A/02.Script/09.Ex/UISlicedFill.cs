using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[AddComponentMenu("UI/Effects/UI Sliced Fill")]
[RequireComponent(typeof(Graphic))]
public class UISlicedFill : BaseMeshEffect
{
    public enum FillDirection
    {
        LeftToRight,
        RightToLeft,
        BottomToTop,
        TopToBottom
    }

    [Header("Fill 설정")]
    [SerializeField] private FillDirection _fillDirection = FillDirection.LeftToRight;
    [SerializeField] [Range(0f, 1f)] private float _fillAmount = 1f;

    #region Properties
    public FillDirection Direction
    {
        get => _fillDirection;
        set
        {
            _fillDirection = value;
            graphic.SetVerticesDirty();
        }
    }

    public float FillAmount
    {
        get => _fillAmount;
        set
        {
            _fillAmount = Mathf.Clamp01(value);
            graphic.SetVerticesDirty();
        }
    }
    #endregion

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0)
            return;

        if (_fillAmount >= 1f)
            return;

        if (_fillAmount <= 0f)
        {
            vh.Clear();
            return;
        }

        List<UIVertex> stream = new List<UIVertex>();
        vh.GetUIVertexStream(stream);

        if (stream.Count == 0)
            return;

        // 바운딩 박스 계산
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        for (int i = 0; i < stream.Count; i++)
        {
            Vector3 pos = stream[i].position;
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
        }

        float width = maxX - minX;
        float height = maxY - minY;

        if (width <= 0 || height <= 0)
            return;

        // 클리핑 경계 계산
        float clipMin, clipMax;
        bool isHorizontal = (_fillDirection == FillDirection.LeftToRight || _fillDirection == FillDirection.RightToLeft);

        if (isHorizontal)
        {
            if (_fillDirection == FillDirection.LeftToRight)
            {
                clipMin = minX;
                clipMax = minX + width * _fillAmount;
            }
            else
            {
                clipMin = maxX - width * _fillAmount;
                clipMax = maxX;
            }
        }
        else
        {
            if (_fillDirection == FillDirection.BottomToTop)
            {
                clipMin = minY;
                clipMax = minY + height * _fillAmount;
            }
            else // TopToBottom
            {
                clipMin = maxY - height * _fillAmount;
                clipMax = maxY;
            }
        }

        // 새로운 버텍스 리스트
        List<UIVertex> newStream = new List<UIVertex>();

        // 각 삼각형 처리
        for (int i = 0; i < stream.Count; i += 3)
        {
            UIVertex v0 = stream[i];
            UIVertex v1 = stream[i + 1];
            UIVertex v2 = stream[i + 2];

            // 삼각형 클리핑
            List<UIVertex> clippedTriangle = ClipTriangle(v0, v1, v2, clipMin, clipMax, isHorizontal);
            newStream.AddRange(clippedTriangle);
        }

        // VertexHelper 재구성
        vh.Clear();

        for (int i = 0; i < newStream.Count; i += 3)
        {
            if (i + 2 < newStream.Count)
            {
                vh.AddVert(newStream[i]);
                vh.AddVert(newStream[i + 1]);
                vh.AddVert(newStream[i + 2]);

                int baseIndex = i;
                vh.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
            }
        }
    }

    private List<UIVertex> ClipTriangle(UIVertex v0, UIVertex v1, UIVertex v2, float clipMin, float clipMax, bool isHorizontal)
    {
        List<UIVertex> result = new List<UIVertex>();
        List<UIVertex> polygon = new List<UIVertex> { v0, v1, v2 };

        // 클리핑 평면 1: clipMin (최소 경계)
        polygon = ClipPolygonAgainstEdge(polygon, clipMin, isHorizontal, true);
        
        // 클리핑 평면 2: clipMax (최대 경계)
        polygon = ClipPolygonAgainstEdge(polygon, clipMax, isHorizontal, false);

        // 다각형을 삼각형으로 분할 (fan triangulation)
        if (polygon.Count >= 3)
        {
            for (int i = 1; i < polygon.Count - 1; i++)
            {
                result.Add(polygon[0]);
                result.Add(polygon[i]);
                result.Add(polygon[i + 1]);
            }
        }

        return result;
    }

    private List<UIVertex> ClipPolygonAgainstEdge(List<UIVertex> polygon, float edge, bool isHorizontal, bool keepGreater)
    {
        if (polygon.Count == 0)
            return polygon;

        List<UIVertex> result = new List<UIVertex>();

        for (int i = 0; i < polygon.Count; i++)
        {
            UIVertex current = polygon[i];
            UIVertex next = polygon[(i + 1) % polygon.Count];

            float currentVal = isHorizontal ? current.position.x : current.position.y;
            float nextVal = isHorizontal ? next.position.x : next.position.y;

            bool currentInside = keepGreater ? (currentVal >= edge) : (currentVal <= edge);
            bool nextInside = keepGreater ? (nextVal >= edge) : (nextVal <= edge);

            if (currentInside)
            {
                result.Add(current);

                if (!nextInside)
                {
                    // current → next 교차점 추가
                    result.Add(ComputeIntersection(current, next, edge, isHorizontal));
                }
            }
            else if (nextInside)
            {
                // next가 inside이면 교차점 추가
                result.Add(ComputeIntersection(current, next, edge, isHorizontal));
            }
        }

        return result;
    }

    private UIVertex ComputeIntersection(UIVertex v0, UIVertex v1, float edge, bool isHorizontal)
    {
        float val0 = isHorizontal ? v0.position.x : v0.position.y;
        float val1 = isHorizontal ? v1.position.x : v1.position.y;

        // 보간 비율 계산
        float t = (edge - val0) / (val1 - val0);
        t = Mathf.Clamp01(t);

        UIVertex result = new UIVertex();
        
        // 위치 보간
        result.position = Vector3.Lerp(v0.position, v1.position, t);
        
        // UV 보간
        result.uv0 = Vector4.Lerp(v0.uv0, v1.uv0, t);
        result.uv1 = Vector4.Lerp(v0.uv1, v1.uv1, t);
        result.uv2 = Vector4.Lerp(v0.uv2, v1.uv2, t);
        result.uv3 = Vector4.Lerp(v0.uv3, v1.uv3, t);
        
        // 색상 보간
        result.color = Color32.Lerp(v0.color, v1.color, t);
        
        // 노말 보간
        result.normal = Vector3.Lerp(v0.normal, v1.normal, t).normalized;
        
        // 탄젠트 보간
        result.tangent = Vector4.Lerp(v0.tangent, v1.tangent, t);

        return result;
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
