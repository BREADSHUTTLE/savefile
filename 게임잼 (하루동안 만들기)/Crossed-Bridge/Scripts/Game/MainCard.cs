using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MainCard : MonoBehaviour
{
    public UITexture sprMainCard;
    public UI2DSprite sprSelectLine;
    private bool select_main_mode = false;

    public GameScore m_Score;

    public GameEnd m_gameEnd;

    public void SetMainCardList()
    {
        sprSelectLine.gameObject.SetActive(false);

        // test color
        //sprMainCard.color = Global.SetColor();

        // 테스트 렌덤 뽑기
        RandomCardListSetting();
    }

    public void SelectCardMode()
    {
        if(select_main_mode)
        {
            // 하이라이트 효과 없애는 부분 여기에 세팅
            sprSelectLine.gameObject.SetActive(false);
            select_main_mode = false;
            return;
        }

        // 하이라이트 효과 주는 부분 여기에 체크
        sprSelectLine.gameObject.SetActive(true);
        select_main_mode = true;
    }

    public bool GetMainModeState()
    {
        return select_main_mode;
    }

    public void SetMainModeState(bool state)
    {
        select_main_mode = state;
    }

    private List<float> m_rand = new List<float>();

    private List<float> card_probability = new List<float>();
    private Dictionary<int, TileInfo> InfoTile = new Dictionary<int, TileInfo>();

    public void MainCardListSetting()
    {
        InfoTile = SGlobal.m_gameStart.infoTile.GetAllItems();
        
        // 1인 모드, id 100 이하 값들
        foreach (var info in InfoTile.Values)
        {
            if (info.ID < 100)
                card_probability.Add(info.MODE_ONE);
        }
    }

    private void RandomCardListSetting()
    {
        int index = RandomIndexWithProbability(card_probability);
        SetServeIndex(index);
        
        TileInfo InfoTile = new TileInfo();
        InfoTile = SGlobal.m_gameStart.infoTile.GetItem(index);
        sprMainCard.mainTexture = Resources.Load(InfoTile.MAINCARD) as Texture;
    }

    private int serveIndex = 0;

    private void SetServeIndex(int index)
    {
        serveIndex = index;
    }

    public int GetServeIndex()
    {
        return serveIndex;
    }

    public Texture GetMainCardTexture()
    {
        return Resources.Load(SGlobal.m_gameStart.infoTile.GetItem(serveIndex).SERVECARD) as Texture;
    }

    private int RandomIndexWithProbability(List<float> p)
    {
        // sum 계산 
        float sum = 0;
        foreach (float item in p)
        {
            sum += item;
        }

        float rand = Random.Range(0, sum);

        float current = 0;
        //Debug.Log(p.Count);
        for (int i = 0; i < p.Count; i++)
        {
            current += p[i];
            if (rand < current)
            {
                return i + 1;
            }
        }
        return 0;
    }

    public void GetPreMyScore()
    {
        m_Score.SetMyScore();
    }

    public void SetSaveBestScore(int score)
    {
        m_Score.SaveMyBastScoreData(score);
    }

    public void SetSaveHighScore(int score)
    {
        m_Score.SaveMyHighScoreData(score);
    }


    public void PlayGameExit(bool success, int myCurrentScore)
    {
        m_gameEnd.Build(success, myCurrentScore, m_Score.GetMyBestScore(), m_Score.GetMyHighScore());
    }

    public void PlayGlobalScore(int currentScore)
    {
        m_gameEnd.GlobalMyScore(currentScore, m_Score.GetMyBestScore(), m_Score.GetMyHighScore());
    }

    public void PlayGameExitHide()
    {
        m_gameEnd.Hide();
    }
}
