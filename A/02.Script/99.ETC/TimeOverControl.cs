using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class TimeOverControl : MonoBehaviour
{
    public float _leftTime;

    WaitForSeconds falseDelay;
    WaitForSeconds whileDelay = new WaitForSeconds(1);
    // UnityEvent �߰�
    void Awake()
    {
        falseDelay = new WaitForSeconds(_leftTime);
        //gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    void OnEnable()
    {
        StopAllCoroutines();
        StopCoroutine("TimeOut");
        StartCoroutine("TimeOut");

    }

    IEnumerator TimeOut()
    {
        yield return falseDelay;

        while (gameObject.activeSelf)
        {
            //gameManager.photonManager.MasterChangeTimeOut();
            yield return whileDelay;
        }
    }
}
