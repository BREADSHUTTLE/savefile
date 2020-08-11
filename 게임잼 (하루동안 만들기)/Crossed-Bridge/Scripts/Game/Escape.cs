using UnityEngine;
using System.Collections;

public class Escape : MonoBehaviour {

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private bool menuOpen = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Application.loadedLevelName == "TitleScene")
            {
                Application.Quit();
            }
            else if (Application.loadedLevelName == "GameScene")
            {
                if (!menuOpen)
                {
                    SGlobal.m_gameStart.menuManager.Build();
                    menuOpen = true;
                }
                else
                {
                    SGlobal.m_gameStart.menuManager.Hide();
                    menuOpen = false;
                }

            }
        }
    }
}
