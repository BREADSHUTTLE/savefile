using System.Linq;
using UnityEngine;

public class RectTransformSorter : MonoBehaviour
{
    private RectTransform parentRectTransform;

    void Start()
    {
        parentRectTransform = GetComponent<RectTransform>();
    }

    void FixedUpdate()
    {
        SortChildrenByXValue();
    }

    void SortChildrenByXValue()
    {
        RectTransform[] children = new RectTransform[parentRectTransform.childCount];
        for (int i = 0; i < parentRectTransform.childCount; i++)
        {
            children[i] = parentRectTransform.GetChild(i) as RectTransform;
        }

        // 그룹 A: scale.x > 0.5 → x값 기준 오름차순
        var groupA = children
            .Where(c => c.localScale.x > 0.5f)
            .OrderBy(c => c.anchoredPosition.x)
            .ToList();

        // 그룹 B: scale.x <= 0.5 → x 절댓값 기준 **내림차순**
        var groupB = children
            .Where(c => c.localScale.x <= 0.5f)
            .OrderByDescending(c => Mathf.Abs(c.anchoredPosition.x))
            .ToList();

        // 병합
        var sorted = groupA.Concat(groupB).ToList();

        // 적용
        for (int i = 0; i < sorted.Count; i++)
        {
            sorted[i].SetSiblingIndex(i);
        }
    }




}
