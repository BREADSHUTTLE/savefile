using System.Collections;
using UnityEngine;

public class GameobjectLog : MonoBehaviour
{
    bool isactive = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isactive = gameObject.activeInHierarchy;
    }


    void OnDisable()
    {
        isactive = false;
        Debug.Log("비활성화 됨!");

    }
    // Update is called once per frame
    void Update()
    {

    }
}
