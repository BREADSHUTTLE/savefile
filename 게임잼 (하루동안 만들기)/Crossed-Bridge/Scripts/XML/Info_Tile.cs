using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Text;

public class Info_Tile : MonoBehaviour
{
    public void Xme_Load(string fileName)
    {
        TextAsset textAsset = (TextAsset)Resources.Load(fileName, typeof(TextAsset));
        if (textAsset == null)
        {
            Debug.Log("invalid filename");
            return;
        }

        XmlDocument xDoc = new XmlDocument();
        xDoc.LoadXml(textAsset.text);

        XmlNodeList nodeList = xDoc.SelectNodes("CB");
        
        foreach (XmlNode node in nodeList)
        {
            if (node.Name.Equals("CB") && node.HasChildNodes)
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    TileInfo info = new TileInfo();

                    info.ID = int.Parse(child.Attributes.GetNamedItem("id").Value);
                    info.NAME = child.Attributes.GetNamedItem("name").Value;

                    info.MAINCARD = child.Attributes.GetNamedItem("icon_01").Value;
                    info.SERVECARD = child.Attributes.GetNamedItem("icon_02").Value;
                    info.SELECT_SERVECARD = child.Attributes.GetNamedItem("icon_03").Value;

                    info.COLOR = child.Attributes.GetNamedItem("color").Value;

                    info.UP = bool.Parse(child.Attributes.GetNamedItem("bridge_up").Value.ToLower());
                    info.DOWN = bool.Parse(child.Attributes.GetNamedItem("bridge_down").Value.ToLower());
                    info.LEFT = bool.Parse(child.Attributes.GetNamedItem("bridge_left").Value.ToLower());
                    info.RIGHT = bool.Parse(child.Attributes.GetNamedItem("bridge_right").Value.ToLower());

                    info.MODE_ONE = float.Parse(child.Attributes.GetNamedItem("mode_1").Value);

                    AddItem(info);
                }
            }
        }
    }

    Dictionary<int, TileInfo> m_dicData = new Dictionary<int, TileInfo>();
    // 아이템 추가.
    public void AddItem(TileInfo _cInfo)
    {
        // 아이템은 고유해야 되니까, 먼저 체크!
        if (m_dicData.ContainsKey(_cInfo.ID)) return;

        // 이제 아이템을 추가.
        m_dicData.Add(_cInfo.ID, _cInfo);
    }
    // 하나의 아이템 얻기
    public TileInfo GetItem(int _nID)
    {
        // 있는지 체크 해야겠죠?
        if (m_dicData.ContainsKey(_nID))
            return m_dicData[_nID];

        // 없으면 null 리턴!
        return null;
    }
    // 전체 리스트 얻기
    public Dictionary<int, TileInfo> GetAllItems()
    {
        return m_dicData;
    }
    // 전체 갯수 얻기
    public int GetItemsCount()
    {
        return m_dicData.Count;
    }
}

public class TileInfo
{
    private int nID;
    private string strName;
    private string strColor;
    private string mainCard;
    private string serveCard;
    private string selectCard;
    private bool bridge_up;
    private bool bridge_down;
    private bool bridge_left;
    private bool bridge_right;
    private float mode_1;

    public int ID
    {
        set { nID = value; }
        get { return nID; }
    }
    public string NAME
    {
        set { strName = value; }
        get { return strName; }
    }

    public string MAINCARD
    {
        set { mainCard = value; }
        get { return mainCard; }
    }

    public string SERVECARD
    {
        set { serveCard = value; }
        get { return serveCard; }
    }

    public string SELECT_SERVECARD
    {
        set { selectCard = value; }
        get { return selectCard; }
    }

    public string COLOR
    {
        set { strColor = value; }
        get { return strColor; }
    }

    public bool UP
    {
        set { bridge_up = value; }
        get { return bridge_up; }
    }

    public bool DOWN
    {
        set { bridge_down = value; }
        get { return bridge_down; }
    }

    public bool LEFT
    {
        set { bridge_left = value; }
        get { return bridge_left; }
    }
    
    public bool RIGHT
    {
        set { bridge_right = value; }
        get { return bridge_right; }
    }

    public float MODE_ONE
    {
        set { mode_1 = value; }
        get { return mode_1; }
    }
}