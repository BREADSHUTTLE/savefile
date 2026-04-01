using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Chip : MonoBehaviour
{
    // Start is called before the first frame update
    public Image chipImage;
    public RectTransform rectTransform;

    private void Awake()
    {
        //gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }
    private void OnEnable()
    {
        chipImage.transform.up = Vector2.right * Random.Range(-1.0f, 1.0f) + Vector2.up * Random.Range(-1.0f, 1.0f);
    }
public void ReturnChip(Vector3 dir)
{
    // dir은 출발지에서 목적지까지의 벡터여야 합니다.
    // 60 프레임 동안 이동하면 정확히 도착하게 됩니다.
    Vector3 step = dir / 60f; 
    Debug.Log("ReturnChip step: " + step);
    StartCoroutine(ReturnChipCoroutine(step));
}
    IEnumerator ReturnChipCoroutine(Vector3 dir)
    {
        for (int i = 0; i < 60; i++)
        {
            rectTransform.position += dir;
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
