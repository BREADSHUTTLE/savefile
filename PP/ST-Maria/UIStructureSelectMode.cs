// UIStructureSelectMode.cs     : UIStructureSelectMode implementation file
//
// Description                  : UIStructureSelectMode
// Author                       : uhrain7761
// Maintainer                   : uhrain7761
// How to use                   : 
// Created                      : 2018/10/01
// Last Update                  : 2019/01/09
// Known bugs                   : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MyStrip;

namespace ST.MARIA.UI.MYSTRIP
{
    public class UIStructureSelectMode : MonoBehaviour
    {
        public enum State
        {
            None,
            DropInventory,
            UnDropInventory,
            Select,
        }

        [SerializeField] private GameObject arrow;
        [SerializeField] private Button removeButton;
        [SerializeField] private Button applyButton;
        [SerializeField] private Tooltip tooltip;

        [SerializeField] private Vector2 arrowTopDefPos = new Vector2(0, 0);
        [SerializeField] private Vector2 arrowBottompDefPos = new Vector2(0, 0);
        [SerializeField] private Vector2 arrowTopPos = new Vector2(0, 0);
        [SerializeField] private Vector2 arrowBottomPos = new Vector2(0, 0);

        [SerializeField] private RectTransform arrowLeftTop;
        [SerializeField] private RectTransform arrowLeftBottom;
        [SerializeField] private RectTransform arrowRightTop;
        [SerializeField] private RectTransform arrowRightBottom;

        [SerializeField] private UIStructureInfo casinoInfo;
        [SerializeField] private UIStructureInfo decoInfo;

        private ObjCreatureData structureData;
        private State mode = State.None;

        public void Build(State state, ObjCreatureData data)
        {
            structureData = data;
            mode = state;

            ShowInfo();
        }

        public void ShowInfo()
        {
            if (mode == State.None)
                return;

            if (mode == State.DropInventory || mode == State.UnDropInventory)
            {
                CommonTools.SetActive(removeButton, true);
                CommonTools.SetActive(applyButton, true);
                ShowSelectStructureInfo(false);
                if (mode == State.DropInventory)
                {
                    applyButton.interactable = true;
                    //applyButton.image.color = applyButton.colors.normalColor;
                }
                else if (mode == State.UnDropInventory)
                {
                    applyButton.interactable = false;
                    //applyButton.image.color = applyButton.colors.disabledColor;
                }

                if (tooltip != null)
                {
                    tooltip.Initialize();
                    tooltip.TooltipUpdate(structureData);
                    tooltip.Show(true);
                }
            }
            else if (mode == State.Select)
            {
                CommonTools.SetActive(removeButton, false);
                CommonTools.SetActive(applyButton, false);
                ShowSelectStructureInfo(true, structureData);

                if (tooltip != null)
                    tooltip.Show(false);
            }
        }

        public void ShowArrow(bool show, ObjCreatureData data)
        {
            if (data != null)
            {
                if (arrowLeftTop != null)
                {
                    arrowLeftTop.anchoredPosition = arrowTopDefPos;
                    arrowLeftTop.anchoredPosition += new Vector2(arrowTopPos.x * (data.SizeX - 1), arrowTopPos.y * (data.SizeY - 1));
                }

                if (arrowLeftBottom != null)
                {
                    arrowLeftBottom.anchoredPosition = arrowBottompDefPos;
                    arrowLeftBottom.anchoredPosition += new Vector2(-(arrowBottomPos.x * (data.SizeX - 1)), arrowBottomPos.y * (data.SizeY - 1));
                }

                if (arrowRightTop != null)
                {
                    arrowRightTop.anchoredPosition = arrowTopDefPos;
                    arrowRightTop.anchoredPosition += new Vector2(arrowTopPos.x * (data.SizeX - 1), arrowTopPos.y * (data.SizeY - 1));
                }
               
                if (arrowRightBottom != null)
                {
                    arrowRightBottom.anchoredPosition = arrowBottompDefPos;
                    arrowRightBottom.anchoredPosition += new Vector2(arrowBottomPos.x * (data.SizeX - 1), arrowBottomPos.y * (data.SizeY - 1));
                }
            }

            CommonTools.SetActive(arrow, show);
        }

        public void OnClickApply()
        {
            SetupAttributeMap.Instance.SelectImageParent(SetupAttributeMap.Instance.draggingIMG, false);

            if (mode == State.DropInventory)
            {
                SetupAttributeMap.Instance.DropCell(true, true);
            }
            else if (mode == State.Select)
            {
                //SetupAttributeMap.Instance.ShowArrow(null, false, null);
                //SetupAttributeMap.Instance.DropSelectMode();
                //EditUI.Instance.ShowSelectModeClose(false);
                //EditUI.Instance.SelectMode(false);
                //SetupAttributeMap.Instance.AssignObjInfoPopupOnOff(true);
                //if (EditUI.Instance.isEditSelectMode)
                //{
                //    EditUI.Instance.isEditSelectMode = false;
                //    EditUI.Instance.EditClick(true, true);
                //}
                //SessionLobby.I.RequestMystripSave(() =>
                //{
                //    SetupAttributeMap.I.ReponseDataReconnect();
                //    SetupAttributeMap.Instance.AssignObjInfoPopupOnOff(true);
                //});
                
                EditUI.Instance.SelectMode(false);
                SetupAttributeMap.Instance.AssignObjInfoPopupOnOff(true);
                SetupAttributeMap.Instance.AssignObjSort();
                ShowSelectStructureInfo(false);
            }
        }
        
        public void OnClickRemove()
        {
            EditUI.I.soundPlayer[2].Play();
            SetupAttributeMap.Instance.RemoveSelectAssignObj();
            SetupAttributeMap.Instance.DelectStructure();
            SessionLobby.I.RequestMystripSave(() =>
            {
                SetupAttributeMap.I.ReponseDataReconnect();    
            });
        }

        private void ShowSelectStructureInfo(bool show, ObjCreatureData data = null)
        {
            CommonTools.SetActive(casinoInfo, false);
            CommonTools.SetActive(decoInfo, false);

            if (data == null)
                return;

            if (data.ObjType == OBJ_TYPE.STRUCTURE)
            {
                casinoInfo.Build(data);
                CommonTools.SetActive(casinoInfo, true);
            }
            else if (data.ObjType == OBJ_TYPE.DECORATOR)
            {
                decoInfo.Build(data);
                CommonTools.SetActive(decoInfo, true);
            }
        }
    }
}