using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;


public class SettingTile : MonoBehaviour
{
    private GameObject m_goTile;
    private ServeCard m_card;

    public List<GameObject> tile_list = new List<GameObject>();

    public void Init(GameObject goTile, GameObject startTile, GameObject goalTile, UIGrid m_grid)
    {
        var prefab = Application.dataPath + "/Resources/Prefab" + "/" + "Tile";
        var start_prefab = Application.dataPath + "/Resources/Prefab" + "/" + "Start";
        var goal_prefab = Application.dataPath + "/Resources/Prefab" + "/" + "Goal";

        if (prefab != null && start_prefab != null && goal_prefab!= null)
        {
            for (int i = 0; i < Global.V_MAX_TILLE; i++)
            {
                for (int j = 0; j < Global.H_MAX_TILLE; j++)
                {
                    m_goTile = SettingTileObject(i, j);

                    m_goTile.transform.parent = m_grid.transform;
                    m_goTile.gameObject.name = i + "x" + j + " Tile";
                    m_goTile.transform.localScale = Vector3.one;
                    m_card = m_goTile.GetComponent<ServeCard>();
                    m_card.SelectSetCard(false);

                    tile_list.Add(m_goTile);
                    m_goTile.GetComponent<ServeCard>().MyIndex(i, j);
                }
            }
        }

        Global.global_tile = tile_list;
    }

    private GameObject StartTile;

    private GameObject SettingTileObject(int i, int j)
    {
        GameObject go;
        if (i == 0 && j == Global.H_MAX_TILLE - 1)
        {
            go = (GameObject)GameObject.Instantiate(Resources.Load("Prefab/Goal"));
            go.transform.FindChild("Tile").transform.localPosition = new Vector3(14.0f, 13.0f, 0.0f);
            go.GetComponent<ServeCard>().SetFinishCard(true);
        }
        else if (i == Global.V_MAX_TILLE - 1 && j == 0)
        {
            go = (GameObject)GameObject.Instantiate(Resources.Load("Prefab/Start"));
            go.transform.FindChild("Tile").transform.localPosition = new Vector3(-14.5f, -13.3f, 0.0f);
            go.GetComponent<ServeCard>().SetStartCard(true);
            StartTile = go.transform.FindChild("Tile").gameObject;
        }
        else
        {
            go = (GameObject)GameObject.Instantiate(Resources.Load("Prefab/Tile"));
        }
        return go;
    }


    public GameObject GetStartTile()
    {
        return StartTile;
    }
}
