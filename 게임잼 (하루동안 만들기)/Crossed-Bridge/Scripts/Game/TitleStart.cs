using UnityEngine;
using System.Collections;

public class TitleStart : MonoBehaviour
{
    public GameObject groupLogo;
    public GameObject groupTitle;

    public TweenAlpha logoAlpha;

    public UILabel lblLoad;

    public UILabel lblStart;

    void Start()
    {
        if (groupLogo != null)
        {
            groupLogo.gameObject.SetActive(true);
            logoAlpha.ResetToBeginning();
            logoAlpha.enabled = false;
        }

        if (groupTitle != null)
            groupTitle.gameObject.SetActive(false);

        lblLoad.gameObject.SetActive(false);

        StartCoroutine(SetLogoAlpha());
    }

    private IEnumerator SetLogoAlpha()
    {
        yield return new WaitForSeconds(1);

        logoAlpha.enabled = true;
    }

    public void TweenLogoHide()
    {
        logoAlpha.enabled = false;
        groupLogo.gameObject.SetActive(false);
        groupTitle.gameObject.SetActive(true);
    }

    public void LoadGameScene()
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
