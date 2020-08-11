// UIMyIslandTutorial.cs    : UIMyIslandTutorial implementation file
//
// Description              : UIMyIslandTutorial
// Author                   : 
// Maintainer               : uhrain7761
// How to use               : 
// Created                  : 2018/10/18
// Last Update              : 2018/10/22
// Known bugs               : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//

using System;
using UnityEngine;
using UnityEngine.UI;
using ST.MARIA;
using ST.MARIA.Popup;

public class UIMyIslandTutorial : MonoBehaviour
{
    [SerializeField] private Camera camera;
    [SerializeField] private RectTransform structureTuto;
    [SerializeField] private RectTransform videoTuto;
    [SerializeField] private Image editTuto;
    [SerializeField] private Image invenTuto;
    [SerializeField] private RectTransform mapTuto;

    private bool firstCasino = true;

    private void OnEnable()
    {
        if (structureTuto != null)
            CommonTools.SetActive(structureTuto, false);

        if (videoTuto != null)
            CommonTools.SetActive(videoTuto, false);

        if (editTuto != null)
            CommonTools.SetActive(editTuto, false);

        if (invenTuto != null)
            CommonTools.SetActive(invenTuto, false);

        if (mapTuto != null)
            CommonTools.SetActive(mapTuto, false);
    }

    public void BuildStructure(string ID, Image structure)
    {
        if (ST.MARIA.PlayerPrefs.GetInt("StructureTutorial") > 0)
            return;

        string[] temp = ID.Split(':');

        // 무조건 첫 번째 카지노
        if (temp.Length <= 1)
        {
            CommonTools.SetActive(structureTuto, false);
            firstCasino = false;
            return;
        }
        
        if (Convert.ToInt32(temp[1]) == 1)
        {
            structureTuto.anchoredPosition = structure.rectTransform.anchoredPosition + new Vector2(200f, 200f);
            //CommonTools.SetActive(structureTuto, true);
            firstCasino = true;
        }
    }

    public void DisableStructure(string ID)
    {
        string[] temp = ID.Split(':');

        // 무조건 첫 번째 카지노
        if (temp.Length <= 1)
            return;

        if (Convert.ToInt32(temp[1]) == 1)
        {
            CommonTools.SetActive(structureTuto, false);
            firstCasino = false;
        }
    }

    public void BuildVideoAD(Image video)
    {
        if (ST.MARIA.PlayerPrefs.GetInt("VideoAdTutorial") > 0)
            return;

        videoTuto.anchoredPosition = video.rectTransform.anchoredPosition + new Vector2(200f, 200f);
        CommonTools.SetActive(videoTuto, true);
    }

    public void ShowEditMode(bool show)
    {
        if (structureTuto != null)
        {
            if (ST.MARIA.PlayerPrefs.GetInt("StructureTutorial") < 1)
            {
                if (firstCasino == true)
                    CommonTools.SetActive(structureTuto, show);
                else
                    CommonTools.SetActive(structureTuto, false);
            }
            else
            {
                CommonTools.SetActive(structureTuto, false);
            }
        }

        if (videoTuto != null)
        {
            if (ST.MARIA.PlayerPrefs.GetInt("VideoAdTutorial") < 1)
                CommonTools.SetActive(videoTuto, show);
            else
                CommonTools.SetActive(videoTuto, false);
        }

        if (editTuto != null)
        {
            if (ST.MARIA.PlayerPrefs.GetInt("EditButtonTutorial") < 1)
                CommonTools.SetActive(editTuto, show);
            else
                CommonTools.SetActive(editTuto, false);
        }

        if (invenTuto != null)
        {
            if (ST.MARIA.PlayerPrefs.GetInt("InventoryTutorial") < 1)
                CommonTools.SetActive(invenTuto, !show);
            else
                CommonTools.SetActive(invenTuto, false);
        }

        ShowMap(show);
    }

    private void ShowMap(bool show)
    {
        if (mapTuto != null)
        {
            if (ST.MARIA.PlayerPrefs.GetInt("MapTutorial") < 1)
            {
                // 닫기버튼 최초 클릭
                if (ST.MARIA.PlayerPrefs.GetInt("FirstEditClose") > 0)
                    CommonTools.SetActive(mapTuto, show);
                else
                    CommonTools.SetActive(mapTuto, false);
            }
            else
            {
                CommonTools.SetActive(mapTuto, false);
            }
        }
    }

    private void LateUpdate()
    {
        //if (structureTuto != null && structureImage != null)
        //{
        //    var point = camera.WorldToScreenPoint(structureImage.transform.position);
        //    structureTuto.rectTransform.anchoredPosition = new Vector2(point.x > (mapRect.rect.width / 2) ? point.x - (mapRect.rect.width / 2) : point.x,
        //                                                                point.y > (mapRect.rect.height / 2) ? point.y - (mapRect.rect.height / 2) : point.y);
        //}

        //if (videoTuto != null && videoImage != null)
        //{
        //    var point = camera.WorldToScreenPoint(videoImage.transform.position);
        //    videoTuto.rectTransform.anchoredPosition = new Vector2(point.x > (mapRect.rect.width / 2) ? point.x - (mapRect.rect.width / 2) : point.x,
        //                                                                point.y > (mapRect.rect.height / 2) ? point.y - (mapRect.rect.height / 2) : point.y);
        //}

        //if (mapTuto != null && mapImage != null)
        //{
        //    var point = camera.WorldToScreenPoint(mapImage.transform.position);
        //    mapTuto.rectTransform.anchoredPosition = new Vector2(point.x > (mapRect.rect.width / 2) ? point.x - (mapRect.rect.width / 2) : point.x,
        //                                                                point.y > (mapRect.rect.height / 2) ? point.y - (mapRect.rect.height / 2) : point.y);
        //}
    }

    public void OnClickStructureTutorial()
    {
        Debug.Log("MyStrip Structure Tutorial");

        CommonTools.SetActive(structureTuto, false);
        ST.MARIA.PlayerPrefs.SetInt("StructureTutorial", 1);
        PopupTutorial.Create(PopupTutorial.Type.CasinoStructure);
    }

    public void OnClickVideoAdTutorial()
    {
        Debug.Log("MyStrip Video Ad Tutorial");

        CommonTools.SetActive(videoTuto, false);
        ST.MARIA.PlayerPrefs.SetInt("VideoAdTutorial", 1);
        PopupTutorial.Create(PopupTutorial.Type.VideoAd);
    }

    public void OnClickEditTutorial()
    {
        Debug.Log("MyStrip Edit Button Tutorial");

        CommonTools.SetActive(editTuto, false);
        ST.MARIA.PlayerPrefs.SetInt("EditButtonTutorial", 1);
        PopupTutorial.Create(PopupTutorial.Type.MyStripEdit);
    }

    public void OnClickInventoryTutorial()
    {
        Debug.Log("MyStrip Inventory Tutorial");

        CommonTools.SetActive(invenTuto, false);
        ST.MARIA.PlayerPrefs.SetInt("InventoryTutorial", 1);
        PopupTutorial.Create(PopupTutorial.Type.Inventory);
    }

    public void OnClickMapTutorial()
    {
        Debug.Log("MyStrip Map Tutorial");

        CommonTools.SetActive(mapTuto, false);
        ST.MARIA.PlayerPrefs.SetInt("MapTutorial", 1);
        PopupTutorial.Create(PopupTutorial.Type.Map);
    }
}
