using UnityEngine;
using System.Collections;

public class MenuManager : MonoBehaviour
{
    public GameObject helpPopup;
    public GameObject btnObject;

    public UILabel lblLoad;

    public void Build()
    {
        lblLoad.gameObject.SetActive(false);
        helpPopup.SetActive(false);
        btnObject.SetActive(true);

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void HelpPopupOpen()
    {
        helpPopup.SetActive(true);
        btnObject.SetActive(false);
    }

    public void HelpPopupHide()
    {
        helpPopup.SetActive(false);
        btnObject.SetActive(true);
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

    public void GameClose()
    {
        Debug.Log("게임 종료");
        Application.Quit();
    }
}
