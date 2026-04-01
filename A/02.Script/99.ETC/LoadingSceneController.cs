using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LoadingSceneController : MonoBehaviour
{
    public Text loadingText;

    private float minimumLoadingTime = 2.2f;

    void Start()
    {
        Application.targetFrameRate = 60;
        StartCoroutine(LoadAsyncScene("Game")); // 로드할 씬 이름
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    IEnumerator LoadAsyncScene(string sceneName)
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);
        asyncOperation.allowSceneActivation = false;

        float timer = 0f;

        while (!asyncOperation.isDone)
        {
            // 로딩 진행률 계산
            float progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
            loadingText.text = (progress * 100f).ToString("F0") + "%";

            timer += Time.deltaTime;

            // 씬 로딩 완료 & 최소 시간 경과 시 전환
            if (asyncOperation.progress >= 0.9f && timer >= minimumLoadingTime)
            {
                loadingText.text = "100%";
                asyncOperation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
