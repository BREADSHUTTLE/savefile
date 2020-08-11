using UnityEngine;
using System.Collections;
using JaykayFramework;
using System;
using System.Reflection;
using System.Linq;
using UnityEngine.UI;
using System.Collections.Generic;


public class AddCanvasElementToBuildUI : MonoBehaviour {

    Camera subuicamera;

    CanvasScaler canvasScaler;

    void Awake()
    {
        SetCanvasResolution();
        subuicamera = GameObject.Find("d3_SubUICamera").GetComponent<Camera>();

        Canvas canvas = gameObject.GetComponent<Canvas>();
        AddCameraToCanvas(canvas);
        BuildUI.Instance.AddCanvasElementTodicCanvas(gameObject.name, canvas);
    }

    void AddCameraToCanvas(Canvas canvas)
    {
        switch (canvas.name)
        {
            case "d10_SubUIPopupCanvas":
            case "d11_SubUIPopupCanvas":
            case "d100_SubUIPopupCanvas":
                canvas.worldCamera = subuicamera;
                break;
            default:
                canvas.worldCamera = Camera.main;
                break;
        }
    }

    public void SetCanvasResolution ()
    {
        if (gameObject.name == "InGameCanvas")
            return;

        canvasScaler = gameObject.GetComponent<CanvasScaler>();

        Vector2 stand = new Vector2(720, 1280); // 16:9 비율
        float standScreen = (float)stand.y / stand.x;

        float screen = (float)Screen.height / Screen.width;
        float defScreen = (float)canvasScaler.referenceResolution.y / canvasScaler.referenceResolution.x;

        if (defScreen == screen)
            return;

        if (screen <= standScreen)
            return;

        // get child list
        RectTransform[] list = gameObject.GetComponentsInChildren<RectTransform>(true);
        List<RectTransform> childList = new List<RectTransform>();

        foreach (var child in list)
        {
            if (child.name.Contains("Stage") && !child.name.Contains("sp") && !child.name.Contains("StageInGamePause"))
                childList.Add(child);
        }

        // create root object
        GameObject rootObject = new GameObject("RootCanvas", typeof(RectTransform));
        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.SetParent(transform);
        rootRect.transform.localScale = new Vector3(1, 1, 1);
        rootRect.anchoredPosition = new Vector2(0, 0);

        rootRect.sizeDelta = canvasScaler.referenceResolution;

        foreach (var child in childList)
        {
            child.SetParent(rootRect);
            child.offsetMin = new Vector2(0, 0);
            child.offsetMax = new Vector2(0, 0);
        }

        canvasScaler.matchWidthOrHeight = (screen > defScreen) ? 0 : 1;
    }
}
