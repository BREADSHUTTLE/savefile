using UnityEngine;
using System.Collections;

public class GameStart : MonoBehaviour
{
    public SettingTile m_settingTile;
    public UIGrid m_tileGrid;
    public GameObject objTile;

    public GameObject StartTile;
    public GameObject GoalTile;

    public MainCard m_mainCard;
    public Info_Tile infoTile;

    public MenuManager menuManager;

    void Start()
    {
        infoTile.Xme_Load(Global.x_tileFile);
        SGlobal.SetGameStartScript(gameObject.GetComponent<GameStart>());
        SGlobal.SetTileScript(m_settingTile);
        // main card random setting
        m_mainCard.MainCardListSetting();
        m_mainCard.PlayGameExitHide();

        // select card clear
        Global.select_tile.Clear();
        Global.start_tile.Clear();
        Global.start_card_line = false;
        Global.start_card_setting_mode = false;
        Global.start_setting = false;
        Global.start_mode = false;
        Global.end_mode_setting = false;

        Global.GameEND = false;
        Global.global_currentScore = 0;

        Global.test.Clear();
        menuManager.Hide();

        m_tileGrid.transform.parent.transform.localPosition = Global.Ground_layout_4;
        m_mainCard.SetMainModeState(false);
        m_mainCard.SetMainCardList();

        m_mainCard.m_Score.Init();

        m_settingTile.Init(objTile, StartTile, GoalTile, m_tileGrid);

        InitStartTile();
    }

    public MainCard GetMainCardScript()
    {
        return m_mainCard;
    }

    public void MenuPopupOpen()
    {
        menuManager.Build();
    }

    public GameObject GetStartTileObject()
    {
        return m_settingTile.GetStartTile();
    }

    public void InitStartTile()
    {
        GameObject tile = GetStartTileObject();
        Transform normalTile = tile.transform.FindChild("Tile_normal");
        Transform upTile = tile.transform.FindChild("Tile_Up");
        Transform rightTile = tile.transform.FindChild("Tile_Right");

        normalTile.gameObject.SetActive(true);
        upTile.gameObject.SetActive(false);
        rightTile.gameObject.SetActive(false);
    }
}