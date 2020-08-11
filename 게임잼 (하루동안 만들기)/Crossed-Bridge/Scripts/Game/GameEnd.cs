using UnityEngine;
using System.Collections;

public class GameEnd : MonoBehaviour
{
    public GameObject Group;
    public UILabel lblSuccess;

    public UILabel lblMyScore;
    public UILabel lblMyBestScore;
    public UILabel lblMyHighScore;

    public UILabel lblLoad;

    public void Build(bool Success, int myScore, int BestScore, int HighScore)
    {
        lblLoad.gameObject.SetActive(false);
        if (Success)
        {
            Group.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            lblSuccess.gameObject.SetActive(true);
        }
        else
        {
            Group.transform.localPosition = new Vector3(0.0f, 77.0f, 0.0f);
            lblSuccess.gameObject.SetActive(false);
        }

        if (lblMyScore != null)
            lblMyScore.text = myScore.ToString("N0");

        if (lblMyBestScore != null)
            lblMyBestScore.text = BestScore.ToString("N0");

        if (lblMyHighScore != null)
            lblMyHighScore.text = HighScore.ToString("N0");

        if(!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    public void GlobalMyScore(int myScore, int BestScore, int HighScore)
    {
        if (lblMyScore != null)
            lblMyScore.text = myScore.ToString("N0");

        if (lblMyBestScore != null)
            lblMyBestScore.text = BestScore.ToString("N0");

        if (lblMyHighScore != null)
            lblMyHighScore.text = HighScore.ToString("N0");
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void NewLoadScene()
    {
        StartCoroutine(Load());
    }

    private IEnumerator Load()
    {
        lblLoad.gameObject.SetActive(true);
        AsyncOperation async = Application.LoadLevelAsync("GameScene");

        while (!async.isDone)
        {
            float progress = async.progress * 100.0f;
            int pRounded = Mathf.RoundToInt(progress);
            lblLoad.text = "Loading---   " + pRounded.ToString() + "%";

            yield return true;
        }
    }
}
