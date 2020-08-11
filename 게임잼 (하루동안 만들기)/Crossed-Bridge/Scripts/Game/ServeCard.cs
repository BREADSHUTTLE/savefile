using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

enum Position
{
    left = 0,
    right = 1,
    up = 2,
    down = 3,
}

public class ServeCard : MonoBehaviour
{
    public UITexture sprMyCard;
    private bool SetMyCard = false;
    private int cardIndex = 0;
    private bool bridge = false;

    public AudioClip selectSound;
    public AudioClip startSound;
    public AudioClip successSound;
    public AudioClip gameoverSound;

    public void SetCard()
    {
        if (!SGlobal.m_gameStart.m_mainCard.GetMainModeState())
            return;

        if (SetMyCard)
        {
            // 여기에 만약 메인 카드 취소 같은 부분 들어가면 삽입할 부분
            return;
        }
        temp_list.Clear();

        SetMyCard = true;
        SGlobal.m_gameStart.m_mainCard.SetMainModeState(false);

        // 임시 테스트용 색깔 변경
        //sprMyCard.color = SGlobal.m_gameStart.m_mainCard.GetMainCardColor();

        // select card setting
        Global.select_tile.Add(gameObject.GetComponent<ServeCard>());

        // 놓으면 사운드 재생
        audio.clip = selectSound;
        audio.PlayOneShot(selectSound);

        // 꽉차면 끝
        FullCard(gameObject.GetComponent<ServeCard>());

        cardIndex = SGlobal.m_gameStart.m_mainCard.GetServeIndex();
        sprMyCard.mainTexture = SGlobal.m_gameStart.m_mainCard.GetMainCardTexture();
        LinkageCardSetting();

        SGlobal.m_gameStart.m_mainCard.SetMainCardList();
    }

    public void FullCard(ServeCard card)
    {
        if (Global.select_tile.Count == (Global.MAX_TILLE_4 - 2))
        {
            Global.GameEND = false;
            Global.end_mode_setting = true;
            GamePositionExit(card, false, false);
            return;
        }
    }

    private int vIndex = 0;
    private int hIndex = 0;

    public void MyIndex(int v_index, int h_index)
    {
        vIndex = v_index;
        hIndex = h_index;
    }

    public int MyVIndex()
    {
        return vIndex;
    }

    public int MyHIndex()
    {
        return hIndex;
    }

    public void SelectSetCard(bool state)
    {
        SetMyCard = state;
    }

    public bool GetSelectSetCard()
    {
        return SetMyCard;
    }

    private int GetMyTileIndex()
    {
        int index = SGlobal.m_setTile.tile_list.IndexOf(gameObject);
        return index;
    }

    #region serve card start/end setting

    private bool start_card = false;
    private bool finish_card = false;

    public void SetStartCard(bool state)
    {
        start_card = state;
    }

    public bool GetStartCard()
    {
        return start_card;
    }

    public void SetFinishCard(bool state)
    {
        finish_card = state;
    }

    public bool GetFinishCard()
    {
        return finish_card;
    }

    #endregion

    #region serve card periphery find
    public int GetLinkageCardIndex()
    {
        return cardIndex;
    }
    // 스타트 지점 연결 시 한번에 연결
    public void LinkageStartCardSetting()
    {
        LinkageCardSetting();
        //Global.start_card_setting_mode = false;
        Global.start_setting = true;
    }

    // 체크 후 이미지 색상 변경
    public void LinkageCardSetting()
    {
        //// my tile index
        //TileInfo InfoTile = new TileInfo();
        //InfoTile = SGlobal.m_gameStart.infoTile.GetItem(cardIndex);
        
        //// my object
        //GameObject go = Global.global_tile[FindObjectIndex()];

        if (!SetMyCard)
            return;

        MyCardBridgeSetting();
    }

    // 상하좌우 체크
    private void MyCardBridgeSetting()
    {
        TileInfo InfoTile = new TileInfo();
        InfoTile = SGlobal.m_gameStart.infoTile.GetItem(cardIndex);

        if (InfoTile.LEFT)
            MyCardBridge(FindObjectIndexLeft(vIndex, hIndex), Position.left);

        if (InfoTile.RIGHT)
            MyCardBridge(FindObjectIndexRight(vIndex, hIndex), Position.right);

        if (InfoTile.UP)
            MyCardBridge(FindObjectIndexUP(vIndex, hIndex), Position.up);

        if (InfoTile.DOWN)
            MyCardBridge(FindObjectIndexDown(vIndex, hIndex), Position.down);
    }

    private bool CardLineList(ServeCard card)
    {
        foreach (var line in Global.start_tile)
        {
            if (line == card)
                return false;
        }
        return true;
    }


    // 주변 오브젝트 체크 후 이미지 변경
    private void MyCardBridge(int bridgeIndex, Position pos)
    {
        int index = bridgeIndex;

        if (index == -1)
            return;

        GameObject go = Global.global_tile[index];
        ServeCard card = go.GetComponent<ServeCard>();

        TileInfo InfoTile = new TileInfo();
        InfoTile = SGlobal.m_gameStart.infoTile.GetItem(card.GetLinkageCardIndex());

        // 상하좌우 중 하나의 오브젝트가 출발지일 경우
        if (card.GetStartCard())
        {
            // 출발지랑 붙어있으면 무조건 true
            //GetBridgeCardImage();

            // 출발지랑 붙은 게 처음일 경우
            if (!Global.start_card_line)
            {
                // 놓으면 사운드 재생
                audio.clip = startSound;
                audio.PlayOneShot(startSound);

                // 출발지 방향 설정
                Debug.Log(pos);
                StartImageChange(pos);

                GetBridgeCardImage();

                Global.start_card_line = true;
                Global.start_card_setting_mode = true;
                if (StartCardLineList(gameObject.GetComponent<ServeCard>()))
                    Global.start_tile.Add(gameObject.GetComponent<ServeCard>());
                Global.ChangeStartBridge(gameObject.GetComponent<ServeCard>());

                SGlobal.m_gameStart.m_mainCard.GetPreMyScore();
                // 만약 처음으로 시작 지점 연결 했는데, 종료부분 이어져 있으면 무조건 게임 종료
                //Debug.Log(StartCardLineList(Global.test));
                GamePositionExit(card, ExitStartCardLineList(Global.test), true);
                Global.start_card_setting_mode = false;

                // 점수
                if (!ExitStartCardLineList(Global.test))
                {
                    Debug.Log("currentScore : " + currentScore);
                    SaveScore(currentScore);
                }
                
                return;
            }
        }
        if (card.GetFinishCard())
        {
            if(Global.test.Count < 2)
                Global.test.Add (gameObject.GetComponent<ServeCard>());
        }

        // 게임 종료
        GamePositionExit(card, ExitStartCardLineList(Global.test), true);


        if (!card.GetSelectSetCard())
            return;


        if (card.GetBridgeState())
        {
            bool state = false;
            temp_list.Clear();

            int temp = 0;
            if (!StartCardLineList(card))
            {
                StartMyCardBridgeSetting();
                

                for (int i = 0; i < temp_list.Count; i++)
                {
                    //Debug.Log("temp_list : " + temp_list[i].gameObject.name);
                    if (!StartCardLineList(temp_list[i]))
                    {
                        //Debug.Log("Bridge temp_list : " + temp_list[i].gameObject.name);
                        state = true;
                        temp = i;
                        if (StartCardLineList(gameObject.GetComponent<ServeCard>()))
                            Global.start_tile.Add(gameObject.GetComponent<ServeCard>());
                    }
                }
            }

            if (state)
            {
                SGlobal.m_gameStart.m_mainCard.GetPreMyScore();
                GetBridgeCardImage();

                // 막힌 부분 테스트
                //GameStuckBridge(temp_list, temp_list[temp]);

                return;
            }
            SGlobal.m_gameStart.m_mainCard.GetPreMyScore();
            return;
        }

        PosBridge(pos, InfoTile, card);
    }

    private void StartImageChange(Position pos)
    {
        GameObject tile = SGlobal.m_gameStart.GetStartTileObject();
        Transform normalTile = tile.transform.FindChild("Tile_normal");
        Transform upTile = tile.transform.FindChild("Tile_Up");
        Transform rightTile = tile.transform.FindChild("Tile_Right");

        switch (pos)
        {
            case Position.down:
                normalTile.gameObject.SetActive(false);
                upTile.gameObject.SetActive(true);
                rightTile.gameObject.SetActive(false);
                break;
            case Position.left:
                normalTile.gameObject.SetActive(false);
                upTile.gameObject.SetActive(false);
                rightTile.gameObject.SetActive(true);
                break;
            default:
                break;
        }
    }

    private void SaveScore(int score)
    {
        SGlobal.m_gameStart.m_mainCard.SetSaveBestScore(Global.global_currentScore);
        SGlobal.m_gameStart.m_mainCard.SetSaveHighScore(Global.global_currentScore);
        SGlobal.m_gameStart.m_mainCard.PlayGlobalScore(Global.global_currentScore);
    }

    private int currentScore = 0;

    private void GamePositionExit(ServeCard card, bool stateEnd, bool saveOK)
    {
        // 시작 점에서 차근차근 쌓아올라감
        //Debug.Log("Global.start_tile.Count : " + Global.start_tile.Count);
        if (currentScore < Global.start_tile.Count)
        {
            currentScore = Global.start_tile.Count;
            if (Global.global_currentScore < currentScore)
                Global.global_currentScore = currentScore;
        }

        if (Global.GameEND)
            return;

        // 게임 종료
        if (card.GetFinishCard() || !stateEnd)
        {
            if (!Global.end_mode_setting)
            {
                Global.end_mode_setting = true;
                Debug.Log("종료 지점에 연결");
                if (Global.start_card_line)
                    GamePositionExit(card, stateEnd, saveOK);
            }
            else
            {
                if (!stateEnd)
                {
                    Global.GameEND = true;
                    Debug.Log("게임을 종료합니다.");
                    
                    // 점수
                    if (!Global.start_card_setting_mode)
                    {
                        if (saveOK)
                        {
                            Debug.Log("currentScore : " + currentScore);
                            SaveScore(currentScore);
                        }
                    }

                    if(saveOK)
                    {
                        audio.clip = successSound;
                        audio.PlayOneShot(successSound);
                    }
                    else
                    {
                        audio.clip = gameoverSound;
                        audio.PlayOneShot(gameoverSound);
                    }

                    SGlobal.m_gameStart.m_mainCard.PlayGameExit(saveOK, currentScore);
                    return;
                }
            }
        }
    }

    // 위치 체크
    private void PosBridge(Position pos, TileInfo InfoTile, ServeCard card)
    {
        if (InfoTile == null)
            return;

        switch (pos)
        {
            case Position.left:
                if (InfoTile.RIGHT)
                    PosStart(card, pos);
                break;
            case Position.right:
                if (InfoTile.LEFT)
                    PosStart(card, pos);
                break;
            case Position.up :
                if (InfoTile.DOWN)
                    PosStart(card, pos);
                break;
            case Position.down:
                if (InfoTile.UP)
                    PosStart(card, pos);
                break;
            default:
                Debug.Log("start / end bridge");
                break;
        }
    }

    private void PosStart(ServeCard card, Position pos)
    {
        if (!Global.start_card_line)
            return;
        int temp = 0;

        if (Global.start_setting)
        {
            bool state = false;
            temp_list.Clear();
            
            if (StartCardLineList(card))
            {
                StartMyCardBridgeSetting();

                for (int i = 0; i < temp_list.Count; i++)
                {
                    //Debug.Log("temp_list : " + temp_list[i].gameObject.name);
                    if (!StartCardLineList(temp_list[i]))
                    {
                        state = true;
                        temp = i;
                        //Debug.Log("Bridge temp_list : " + temp_list[i].gameObject.name);
                        if (StartCardLineList(gameObject.GetComponent<ServeCard>()))
                            Global.start_tile.Add(gameObject.GetComponent<ServeCard>());
                    }
                }
            }

            if (!state)
            {
                //Debug.Log("지나감");
                SGlobal.m_gameStart.m_mainCard.GetPreMyScore();
                return;
            }
        }

        // 막힌 부분 테스트
        //GameStuckBridge(temp_list, temp_list[temp]);

        GetBridgeCardImage();
        card.GetBridgeCardImage();

        // 시작 지점 체크
        StartChack(card);

    }

    int nTemp = 0;
    private void GameStuckBridge(List<ServeCard> cardItem, ServeCard bridgeItem)
    {
        if (nTemp > 0)
        {
            nTemp = 0;
            return;
        }

        int StuckCount = 0;
        if (nTemp == 0)
        {
            for (int i = 0; i < cardItem.Count; i++)
            {
                if(StartCardLineList(cardItem[i]) && cardItem[i] != bridgeItem)
                    StuckCount++;
            }
        }

        if (StuckCount > 0)
            Debug.Log(" 더이상 갈 곳 없음 (TEST) ");

    }

    private void StartChack(ServeCard card)
    {
        PosLinkList(card);
    }

    private void PosLinkList(ServeCard card)
    {
        //if (StartCardLineList(card))
        //{
        //    Debug.Log("시작점 연결 리스트에 존재하지 않는 옆동네 오브젝트 : " + card.gameObject.name);
        //}

        card.LinkageCardSetting();
        if (StartCardLineList(gameObject.GetComponent<ServeCard>()))
            Global.start_tile.Add(gameObject.GetComponent<ServeCard>());

        if (StartCardLineList(card))
            Global.start_tile.Add(card);

        SGlobal.m_gameStart.m_mainCard.GetPreMyScore();
    }

    private void ListDate()
    {
        foreach(var list in Global.start_tile)
        {
            Debug.Log(list.gameObject.name);
        }
    }

    private void PosStateMode(ServeCard card)
    {
        if (!StartCardLineList(card))
        {
            GetBridgeCardImage();
        }
    }

    private void StartMyCardBridgeSetting()
    {
        TileInfo InfoTile = new TileInfo();
        InfoTile = SGlobal.m_gameStart.infoTile.GetItem(cardIndex);

        if (InfoTile.LEFT)
            StartMyCardBridge(FindObjectIndexLeft(vIndex, hIndex), Position.left);

        if (InfoTile.RIGHT)
            StartMyCardBridge(FindObjectIndexRight(vIndex, hIndex), Position.right);

        if (InfoTile.UP)
            StartMyCardBridge(FindObjectIndexUP(vIndex, hIndex), Position.up);

        if (InfoTile.DOWN)
            StartMyCardBridge(FindObjectIndexDown(vIndex, hIndex), Position.down);
    }

    private void StartMyCardBridge(int bridgeIndex, Position pos)
    {
        int index = bridgeIndex;

        if (index == -1)
            return;

        GameObject go = Global.global_tile[index];
        ServeCard card = go.GetComponent<ServeCard>();

        TileInfo InfoTile = new TileInfo();
        InfoTile = SGlobal.m_gameStart.infoTile.GetItem(card.GetLinkageCardIndex());

        StartPosBridge(pos, InfoTile, card);
    }

    int m_state = 0;
    private List<ServeCard> temp_list = new List<ServeCard>();

    private void StartPosBridge(Position pos, TileInfo InfoTile, ServeCard s_card)
    {
        if (InfoTile == null)
            return;

        switch (pos)
        {
            case Position.left:
                if (InfoTile.RIGHT)
                    temp_list.Add(s_card);
                break;
            case Position.right:
                if (InfoTile.LEFT)
                    temp_list.Add(s_card);
                break;
            case Position.up:
                if (InfoTile.DOWN)
                    temp_list.Add(s_card);
                break;
            case Position.down:
                if (InfoTile.UP)
                    temp_list.Add(s_card);
                break;
            default:
                Debug.Log("start / end bridge");
                break;
        }
    }

    private void GetBridgeCardImage()
    {
        // 이미 색이 바뀐 연결된 다리 일 경우
        if (bridge)
            return;

        bridge = true;

        sprMyCard.mainTexture =
            Resources.Load(SGlobal.m_gameStart.infoTile.GetItem(cardIndex).SELECT_SERVECARD) as Texture;
    }

    private void GetBridgeCardImageChack()
    {
        bridge = false;

        sprMyCard.mainTexture =
            Resources.Load(SGlobal.m_gameStart.infoTile.GetItem(cardIndex).SERVECARD) as Texture;
    }

    private bool StartCardLineList(ServeCard card)
    {
        foreach (var line in Global.start_tile)
        {
            if (line == card)
                return false;
            //Debug.Log("line.name : " + line.name);
        }
        return true;
    }

    private bool ExitStartCardLineList(List<ServeCard> cardlist)
    {
        foreach (var line in cardlist)
        {
            if (!StartCardLineList(line))
                return false;
        }
        return true;
    }

    public bool GetBridgeState()
    {
        return bridge;
    }

    

    #endregion

    #region index fild
    // 자기 자신 인덱스 찾기
    private int FindObjectIndex()
    {
        int index = 0;
        if (vIndex == 0)
        {
            index = hIndex;
            return index;
        }

        index = (vIndex * 7) + hIndex;
        return index;
    }

    // 상
    private int FindObjectIndexUP(int v_index, int h_index)
    {
        int index = 0;

        if (v_index == 0)
            return -1;   // 아무것도 없음 -1

        index = ((v_index - 1) * 7) + h_index;
        return index;
    }

    // 하
    private int FindObjectIndexDown(int v_index, int h_index)
    {
        int index = 0;

        if (v_index == (Global.V_MAX_TILLE - 1))
            return -1;  // 아무것도 없음 -1

        index = ((v_index + 1) * 7) + h_index;
        return index;
    }

    // 좌
    private int FindObjectIndexLeft(int v_index, int h_index)
    {
        int index = 0;

        if (h_index == 0)
            return -1;  // 아무것도 없음 -1

        index = (v_index * 7) + (h_index - 1);
        return index;
    }

    // 우
    private int FindObjectIndexRight(int v_index, int h_index)
    {
        int index = 0;

        if (h_index == (Global.H_MAX_TILLE - 1))
            return -1;  // 아무것도 없음 -1

        index = (v_index * 7) + (h_index + 1);
        return index;
    }
    #endregion
}
