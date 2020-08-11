using UnityEngine;
using System.Collections;

public class GameScore : MonoBehaviour
{
    public UILabel myScore;
    public UILabel bestScore;
    public UILabel highScore;

    public void Init()
    {
        // 테스트용 스코어 초기화
        //PlayerPrefs.SetInt("BestScore", 0);
        //PlayerPrefs.SetInt("HighScore", 0);

        if (myScore != null)
            SetMyScore();

        if (bestScore != null)
            bestScore.text = PlayerPrefs.GetInt("BestScore").ToString("N0");

        if (highScore != null)
            highScore.text = PlayerPrefs.GetInt("HighScore").ToString("N0");

        Debug.Log("BestScore : " + PlayerPrefs.GetInt("BestScore"));
        Debug.Log("HighScore : " + PlayerPrefs.GetInt("HighScore"));
    }

    public void SetMyScore()
    {
        myScore.text = GetMyScore().ToString("N0");
    }

    private int GetMyScore()
    {
        return Global.start_tile.Count;
    }

    public void SaveMyBastScoreData(int score)
    {
        if (PlayerPrefs.GetInt("BestScore") == 0)
        {
            PlayerPrefs.SetInt("BestScore", score);
            //PlayerPrefs.SetInt("BestScore", score);
            return;
        }


        int bestScore = PlayerPrefs.GetInt("BestScore");

        if (score < bestScore)
        {
            PlayerPrefs.SetInt("BestScore", score);
        }
    }

    public int GetMyBestScore()
    {
        return PlayerPrefs.GetInt("BestScore");
    }

    public void SaveMyHighScoreData(int score)
    {
        if (PlayerPrefs.GetInt("HighScore") == 0)
        {
            PlayerPrefs.SetInt("HighScore", score);
            return;
        }

        int highScore = PlayerPrefs.GetInt("HighScore");

        if (score > highScore)
        {
            PlayerPrefs.SetInt("HighScore", score);
        }
    }

    public int GetMyHighScore()
    {
        return PlayerPrefs.GetInt("HighScore");
    }
}
