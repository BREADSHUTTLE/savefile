using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class Global
{
    public static string x_tileFile = "BridgeCard";
    public static int MAX_TILLE_4 = 63;
    public static int H_MAX_TILLE = 7;
    public static int V_MAX_TILLE = 9;

    public static Vector3 Ground_layout_4 = new Vector3(-269.0f, 174.0f, 0.0f);

    public static List<GameObject> global_tile = new List<GameObject>();
    public static List<ServeCard> select_tile = new List<ServeCard>();
    public static List<ServeCard> start_tile = new List<ServeCard>();

    public static bool start_card_line = false;
    public static bool start_card_setting_mode = false;
    public static bool start_setting = false;
    public static bool start_mode = false;
    public static bool end_mode_setting = false;

    public static bool GameEND = false;

    public static List<ServeCard> test = new List<ServeCard>();

    public static int global_currentScore = 0;

    public static Color SetColor()
    {
        Color m_color = new Color();

        var colorRandom = Random.Range(0.0f, 100.0f);

        if (colorRandom < 20.0f)
            m_color = new Color(255, 255, 0);
        else if (colorRandom >= 20.0f && colorRandom < 50.0f)
            m_color = new Color(255, 0, 0);
        else
            m_color = new Color(0, 255, 255);

        return m_color;
    }

    public static List<GameObject> GetTileObject()
    {
        return global_tile;
    }

    public static void ChangeStartBridge(ServeCard myCard)
    {
        if (!start_card_line)
            return;

        //foreach (var line in select_tile)
        //{
        //    line.LinkageCardSetting();
        //}
        start_card_setting_mode = true;
        myCard.LinkageStartCardSetting();
    }
}

public static class SGlobal
{
    public static GameStart m_gameStart = null;
    public static SettingTile m_setTile = null;

    public static void SetGameStartScript(GameStart start)
    {
        m_gameStart = start;
    }

    public static void SetTileScript(SettingTile tile)
    {
        m_setTile = tile;
    }
}
