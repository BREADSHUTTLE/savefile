using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ToggleDOTweenController : MonoBehaviour
{
    public Toggle uiToggle;
    public DOTweenAnimation tween;

    void Start()
    {
        if (tween == null || uiToggle == null) return;

        // 토글의 초기 IsOn 상태에 맞게 트윈 위치를 맞춰줌
        if (uiToggle.isOn)
        {
            // ON 상태라면 트윈을 끝 프레임으로 맞추기
            tween.DORestart();
            tween.DOComplete();   // 바로 끝까지 점프
        }
        else
        {
            // OFF 상태라면 처음 프레임으로
            tween.DORewind();
        }
    }

    public void OnToggleValueChanged(bool isOn)
    {
        if (tween == null) return;

        if (isOn)
        {
            // OFF → ON : 앞으로 재생
            tween.DORestart();
        }
        else
        {
            // ON → OFF : 역재생
            tween.DOPlayBackwards();
        }
    }
}
