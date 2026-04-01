using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AnteCoinControl : MonoBehaviour
{
    [Tooltip("애니메이션에 사용할 Canvas의 Image 컴포넌트")]
    public Image effectImage;

    [Tooltip("애니메이션 시작 위치 (월드 좌표)")]
    private Vector3 startPosition;

    [Tooltip("애니메이션 끝 위치 (화면 중앙, 월드 좌표)")]
    public Vector3 endPosition;

    [Tooltip("애니메이션 지속 시간 (초)")]
    public float duration = 1.0f;

    // 애니메이션 시작: OnEnable에서 코루틴 시작
    private void OnEnable()
    {
        startPosition = transform.position;
        // Image의 알파값을 1로 초기화
        Color c = effectImage.color;
        c.a = 1f;
        effectImage.color = c;
        
        StopAllCoroutines();
        StartCoroutine(AnimateEffect());
    }

    private IEnumerator AnimateEffect()
    {
        float elapsed = 0f;
        Color initialColor = effectImage.color;
        while (elapsed < duration)
        {
            float t = elapsed / duration; // 진행률 (0 ~ 1)

            // 위치 보간: 시작 위치에서 끝 위치까지 선형 이동
            transform.position = Vector3.Lerp(startPosition, endPosition, t);

            // 알파값: t가 0.7 미만이면 1, 그 이후에 0으로 부드럽게 감소
            float newAlpha = 1f;
            if (t >= 0.7f)
            {
                // t가 0.7에서 1사이에서 비율을 계산: (t - 0.7) / 0.3 (0 ~ 1)
                float fadeT = (t - 0.7f) / 0.3f;
                newAlpha = Mathf.Lerp(1f, 0f, fadeT);
            }

            effectImage.color = new Color(initialColor.r, initialColor.g, initialColor.b, newAlpha);

            elapsed += Time.deltaTime;
            yield return null;
        }
        // 애니메이션 종료 후: 끝 위치로 이동, 알파값 0으로 설정
        transform.position = endPosition;
        effectImage.color = new Color(initialColor.r, initialColor.g, initialColor.b, 0f);
        gameObject.SetActive(false);
    }
}
