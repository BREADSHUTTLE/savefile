using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScrollManualPosition : MonoBehaviour
{
    public RectTransform content; // Content
    public RectTransform viewport; // Viewport

    /// <summary>
    /// Content의 anchoredPosition.x 를 직접 설정
    /// </summary>
    public void SetContentX(float x)
    {
        //Vector2 pos = content.anchoredPosition;
        //pos.x = -x; // Unity UI는 왼쪽이 음수, 오른쪽이 양수인 좌표계
        //content.anchoredPosition = pos;


        SmoothMoveToX(x, 0.35f);
    }

    Coroutine moveCoroutine;

    /// <summary>
    /// 자연스럽게 이동
    /// </summary>
    public void SmoothMoveToX(float targetX, float duration = 0.3f)
    {
        return;
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        moveCoroutine = StartCoroutine(SmoothMoveCoroutine(targetX, duration));
    }

    IEnumerator SmoothMoveCoroutine(float targetX, float duration)
    {
        float elapsed = 0f;
        Vector2 startPos = content.anchoredPosition;
        Vector2 targetPos = new Vector2(-targetX, startPos.y);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            content.anchoredPosition = Vector2.Lerp(startPos, targetPos, elapsed / duration);
            yield return null;
        }
        content.anchoredPosition = targetPos;
        moveCoroutine = null;
    }
}
