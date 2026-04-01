using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class EffectManager : MonoBehaviour {




    public float _leftTime;
    WaitForSeconds falseDelay;
    // UnityEvent 추가
    [SerializeField]
    private UnityEvent onBeforeDisable;
    void Awake()
    {
        falseDelay = new WaitForSeconds(_leftTime);
    }

    void OnEnable()
    {

        StopCoroutine("Disable");
        StartCoroutine("Disable");

    }

    IEnumerator Disable()
    {
        yield return falseDelay;
        onBeforeDisable?.Invoke();
        gameObject.SetActive(false);
    }
    
    
}
