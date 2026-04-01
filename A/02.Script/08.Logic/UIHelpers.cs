using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

public static class UIHelpers
{
    // 각 Transform으로부터 Root까지의 siblingIndex 리스트를 구합니다.
    static List<int> GetHierarchyPath(Transform t)
    {
        var path = new List<int>();
        while (t != null)
        {
            path.Add(t.GetSiblingIndex());
            t = t.parent;
        }
        path.Reverse();
        return path;
    }

    // 두 Transform의 하이어라키 순서를 비교합니다.
    // return >0 if a is after b in hierarchy (즉, a가 b보다 위에 그려짐)
    static int CompareHierarchy(Transform a, Transform b)
    {
        var pa = GetHierarchyPath(a);
        var pb = GetHierarchyPath(b);
        int len = Mathf.Min(pa.Count, pb.Count);
        for (int i = 0; i < len; i++)
        {
            if (pa[i] != pb[i])
                return pa[i].CompareTo(pb[i]);
        }
        // 공통 경로가 다 같다면, 더 깊은 쪽이 나중(위)에 그려집니다.
        return pa.Count.CompareTo(pb.Count);
    }

    /// <summary>
    /// 여러 버튼 중, 하이어라키 전체 순서에서 가장 나중(=가장 위)에 그려지는 버튼을 반환합니다.
    /// </summary>
 public static Button GetTopmostActiveGlobal(Button[] buttons)
    {
        if (buttons == null || buttons.Length == 0) return null;
        Button top = null;

        foreach (var b in buttons)
        {
            if (b == null) 
                continue;

            // 자신과 부모 모두 활성화된 상태만 고려
            if (!b.gameObject.activeInHierarchy) 
                continue;

            // 첫 활성 버튼 발견 시 초기화
            if (top == null || CompareHierarchy(b.transform, top.transform) > 0)
            {
                top = b;
            }
        }

        return top;
    }
}
