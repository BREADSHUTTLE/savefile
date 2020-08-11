// ObjStructure.cs       : ObjStructure implementation file
//
// Description           : ObjStructure
// Author                : Gerndal
// Maintainer            : Gerndal, uhrain7761
// How to use            : 
// Created               : 2018/05/23
// Last Update           : 2018/10/24
// Known bugs            : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using ST.MARIA;
using ST.MARIA.Network;
using System.Text.RegularExpressions;
using System;
using ST.MARIA.UI.MYSTRIP;
using ST.MARIA.DataPool;

namespace MyStrip
{
    public enum OBJ_TYPE
    {
        NONE_AREA   ,
        STRUCTURE   ,
        DECORATOR   ,
        VideoDeco,
    }

    [System.Serializable]
    public class ObjCreatureData
    {
        public int MapIndex = 0;
        public OBJ_TYPE ObjType = OBJ_TYPE.STRUCTURE;
        public int Count = 1;
        public string Name = "OBJ Name";
        public string ID = "xxx:1:01";
        public int PosX = 0;// 2 Array Idx
        public int PosY = 0;// 2 Array Idx
        public float SkinSize = 1;
        public int SizeX = 0;
        public int SizeY = 0;
        public long defaultGold = 0;
        public long Gold = 0;
        public long defaultGem = 0;
        public long Gem = 0;
        public long defaultXP = 0;
        public long XP = 0;
        public long LastTime = 0;
        public long NextTime = 0;
        public long RemainTime = -1;
        public long DefineTime = 0;
        public int Level = 1;
        public string ThumbImgURL = "url";
        public string ObjImgURL = "url";
        public string Description = "Description";

        public  ObjStructure OS         = null;
        public  Image        ObjImg     = null;
        public ObjCreatureData Clone ()
        {
            return (ObjCreatureData) this.MemberwiseClone ();
        }

        public ObjCreatureData Set(OBJ_TYPE type , int count , int level, StructureData SD)
        {
            //MapIndex   = 
            ObjType    = type;
            Count      = count;
            Level      = level;

            Name       = SessionLobby.I.TableLanguageEn[SD.structureName].value;
            ID         = SD.structureId;
            //PosX       = 
            //PosY       = 
            SkinSize   = SD.skinSize/100.0f;
            SizeX      = SD.sizeX;
            SizeY      = SD.sizeY;
            if (type == OBJ_TYPE.STRUCTURE)
            {
                var idx = (Level == 1) ? SD.abilityId1 :
                          (Level == 2) ? SD.abilityId2 :
                                         SD.abilityId3;
                AbilityData abil = SessionLobby.I.TableAbility[idx];
                defaultGold = abil.coinAmount;
                Gold = abil.coinAmount;
                defaultGem = abil.gemAmount;
                Gem = abil.gemAmount;
                defaultXP = abil.expAmount;
                XP = abil.expAmount;
                DefineTime = abil.rewardInterval;
            }
            else
            {
                AbilityData abil1 = SessionLobby.I.TableAbility[SD.abilityId1];
                defaultGold = (int)(abil1.coinPercent * 10000.0f);
                Gold = (int)(abil1.coinPercent * 10000.0f);
                defaultGem = (int)(abil1.gemPercent * 10000.0f);
                Gem = (int)(abil1.gemPercent * 10000.0f);
                defaultXP = (int)(abil1.expPercent * 10000.0f);
                XP = (int)(abil1.expPercent * 10000.0f);
            }

            //Gold       = 
            //Gem        
            //XP         
            //RemainTime 
            //Level    
            ObjImgURL  = Regex.Replace(SD.structureId, ":01", "", RegexOptions.None);
            ObjImgURL  = Regex.Replace(ObjImgURL, ":", "", RegexOptions.None);
            ObjImgURL += (ObjType == OBJ_TYPE.STRUCTURE) ? Level.ToString("D2") : 1.ToString("D2");
            ThumbImgURL= ObjImgURL + ".png";

            Description= SD.structureDescription;
            return this;
        }

        public ObjCreatureData Set(StructuresInUse use , StructureData SD)
        {
            var type = use.structureId.ToLower().Contains("casino") ? OBJ_TYPE.STRUCTURE : OBJ_TYPE.DECORATOR;
            Set(type , Count , Level , SD);
            if (use.structureId.ToLower().Contains("_basic_"))
            {
                ThumbImgURL= use.structureId + ".png";
                ObjImgURL  = use.structureId;
            }

            //MapIndex   = 
            PosX = use.posX;
            PosY = use.posY;
            if (type == OBJ_TYPE.STRUCTURE)
            {
                Gold = use.coinAmount;
                Gem  = use.gemAmount;
                XP   = use.expAmount;
            }

            LastTime   = use.lastRewardDt;
            NextTime   = use.nextRewardDt;
            RemainTime = use.secondsToNext;
            //Level      = use.
            return this;
        }
    }

    [System.Serializable]
    public class ObjCreatureDataList
    {
        public string                   TabName;
        public List<ObjCreatureData>    Datas;

        public ObjCreatureDataList Create(string tabName , string resourcesName)
        {
            var answer = resourcesName.ResourceFromJsonString<AnswerMystripInventory>();
            return Create(tabName , answer);
        }

        public ObjCreatureDataList Create(string tabName , AnswerMystripInventory answer)
        {
            TabName = tabName;
            Datas = new List<ObjCreatureData>();
            if (answer == null) return this;

            var type = (answer.value.casinoBuildingInventory != null) ? OBJ_TYPE.STRUCTURE : OBJ_TYPE.DECORATOR;
            var inv = (type == OBJ_TYPE.STRUCTURE) ? answer.value.casinoBuildingInventory : answer.value.decoInventory;
            inv.ForEach(x => Store(type , x) );
            return this;
        }

        private void Store(OBJ_TYPE type , EditUIInventoryData x)
        {
            if (SessionLobby.I.TableStructure.ContainsKey(x.structureId) == false)
            {
                ("EditUI : " + x.structureId + "   Not Found !!!!!").Print(Color.red);
                return;
            }

            var OCD = new ObjCreatureData().Set(type , x.remain , x.achievedCasinoGrade, SessionLobby.I.TableStructure[x.structureId]);
            Datas.Add(OCD);
        }
    }


    [System.Serializable]
    public class ObjStructure : ScrollCellRoot, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        public ObjCreatureData OCD;
        [HideInInspector]   public string desciption = "";
        private Image thumbDragImg = null;

        private bool fCreate = false;
        private bool fThumbImgDragging = false;
        private Vector2 v2BasicPos;
        private Vector2 v2PrevPos;

        private HScrollAutoSizeCalc hScrollAutoSizeCalc;

        private bool fScrollOnly = false;

        [SerializeField] private Image structureIcon;
        [SerializeField] private Text structureName;
        [SerializeField] private Text remainCount;
        [SerializeField] private Text rewardCoin;
        [SerializeField] private Text rewardGem;
        [SerializeField] private Text rewardXP;
        [SerializeField] private Text rewardTime;
        [SerializeField] private List<GameObject> grade = new List<GameObject>();
        [SerializeField] private RectTransform gradeGroup;
        [SerializeField] private Image newIcon;

        public override void Initialize()
        {
            base.Initialize();

            if (structureName != null)
                structureName.text = OCD.Name;

            hScrollAutoSizeCalc = transform.GetComponentInParent<HScrollAutoSizeCalc>();

            OCD.OS = this;
            OCD.ObjImg = structureIcon;

            SetStateValue();
            InitPointer();

            switch (OCD.ObjType)
            {
                case OBJ_TYPE.STRUCTURE:
                    CommonTools.SetActive(remainCount, false);
                    break;
                case OBJ_TYPE.DECORATOR:
                    CommonTools.SetActive(rewardTime.transform.parent, false);
                    break;
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying == true)
            {
                EditUI.Instance.ActionDelectNewIcon += DeleteNewIcon;
                ShopInfo.Instance.ActionBuyStructure += SetNewIcon;
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying == true)
            {
                EditUI.Instance.ActionDelectNewIcon -= DeleteNewIcon;
                ShopInfo.Instance.ActionBuyStructure -= SetNewIcon;
            }
        }

        private void DeleteNewIcon(string type)
        {
            if (type == "casino")
            {
                if (OCD.ObjType != OBJ_TYPE.STRUCTURE)
                    return;
            }
            else if (type == "structure")
            {
                if (OCD.ObjType != OBJ_TYPE.DECORATOR)
                    return;
            }

            ST.MARIA.DataPool.InventoryInfo.Instance.RemoveKey(OCD.ID);

            CommonTools.SetActive(newIcon, false);
        }

        public void SetStateValue()
        {
            if (grade.Count > 0)
                grade.ForEach(g => CommonTools.SetActive(g, false));

            if (OCD.ObjType == OBJ_TYPE.STRUCTURE)
            {
                var dt = new DateTime(OCD.DefineTime * 10000000);
                var time = "";
                time += (dt.Hour > 0) ? (dt.Hour + "h ") : "";
                time += dt.Minute + "min";

                if (rewardTime != null)
                    rewardTime.text = time;

                if (rewardCoin != null)
                    rewardCoin.text = OCD.defaultGold.ToString();

                if (rewardGem != null)
                    rewardGem.text = OCD.defaultGem.ToString();

                if (rewardXP != null)
                    rewardXP.text = OCD.defaultXP.ToString();

                if (grade.Count > 0)
                {
                    if (grade.Count >= OCD.Level)
                    {
                        for (int i = 0; i < OCD.Level; i++)
                            grade[i].SetActive(true);
                    }
                }
            }
            else
            {
                if (rewardCoin != null)
                    rewardCoin.text = OCD.defaultGold.PercentString();

                if (rewardGem != null)
                    rewardGem.text = OCD.defaultGem.PercentString();

                if (rewardXP != null)
                    rewardXP.text = OCD.defaultXP.PercentString();
            }

            if (remainCount != null)
                remainCount.text = string.Format("x {0}", OCD.Count);

            if (gradeGroup != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(gradeGroup);

            SetNewIcon();
        }

        public void SetNewIcon()
        {
            if (newIcon != null)
            {
                if (InventoryInfo.Instance.FindRewardKey("casino") > 0 || InventoryInfo.Instance.FindRewardKey("structure") > 0)
                {
                    if (InventoryInfo.Instance.FindKey(OCD.ID))
                        CommonTools.SetActive(newIcon, true);
                    else
                        CommonTools.SetActive(newIcon, false);
                }
                else
                {
                    CommonTools.SetActive(newIcon, false);
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (hScrollAutoSizeCalc == null)
                hScrollAutoSizeCalc = GetScrollAutoSizeCalc();

            hScrollAutoSizeCalc.scrollRect.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (thumbDragImg != null)
            {
                thumbDragImg.transform.localPosition = eventData.delta + v2PrevPos;
                v2PrevPos = thumbDragImg.transform.localPosition;
            }

            if (fCreate)
                return;

            if (OCD.Count <= 0)
            {
                if (hScrollAutoSizeCalc == null)
                    hScrollAutoSizeCalc = GetScrollAutoSizeCalc();

                hScrollAutoSizeCalc.scrollRect.OnDrag(eventData);
                return;
            }

            if (fScrollOnly == false)
            {
                var v3Diff = Input.mousePosition - v3StartPoint;
                if ((fCreate == false) && (v3Diff.y > EditUI.I.fingerShake))
                    CreateThumbImg();
                else if (Mathf.Abs(v3Diff.x) > EditUI.I.fingerShake)
                    fScrollOnly = true;
            }

            if (fCreate == false)
            {
                if (fThumbImgDragging == false)
                    fThumbImgDragging = true;

                if (hScrollAutoSizeCalc == null)
                    hScrollAutoSizeCalc = GetScrollAutoSizeCalc();

                hScrollAutoSizeCalc.scrollRect.OnDrag(eventData);
                return;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (hScrollAutoSizeCalc == null)
                hScrollAutoSizeCalc = GetScrollAutoSizeCalc();

            hScrollAutoSizeCalc.scrollRect.OnEndDrag(eventData);
            //EndDrag();
        }

        private HScrollAutoSizeCalc GetScrollAutoSizeCalc()
        {
            return transform.GetComponentInParent<HScrollAutoSizeCalc>();
        }

        public void EndDrag()
        {
            if (EndDragTouch())
            {
                InitPointer();
                if (EditUI.Instance.isEditMode)
                {
                    //EditUI.Instance.ShowTipText(EditUI.Instance.isEditMode ? EditUI.EditState.EditOn : EditUI.EditState.EditOff);
                    if (EditUI.Instance.isDragMode)
                    {
                        if (SetupAttributeMap.Instance.MapAreaCheck())
                        {
                            SetupAttributeMap.Instance.AttachStructurePostion();
                            SetupAttributeMap.Instance.AddSelectAssignObj();
                            SetupAttributeMap.Instance.SetDragImageRaycast(true);
                            EditUI.Instance.BuildSelectMode(UIStructureSelectMode.State.DropInventory, OCD);
                        }
                        else
                        {
                            SetupAttributeMap.Instance.AddSelectAssignObj();
                            SetupAttributeMap.Instance.SetDragImageRaycast(true);
                            EditUI.Instance.BuildSelectMode(UIStructureSelectMode.State.UnDropInventory, OCD);
                        }

                        EditUI.Instance.ShowSelectMode(true);
                        EditUI.Instance.isDragMode = false;
                    }
                }
                else if (EditUI.Instance.isSelectMode)
                {
                    if (EditUI.Instance.isDragMode)
                    {
                        EditUI.Instance.ShowSelectModeClose(true);

                        if (SetupAttributeMap.Instance.MapAreaCheck())
                        {
                            SetupAttributeMap.Instance.AttachStructurePostion();
                            SetupAttributeMap.Instance.AddSelectAssignObj();
                            SetupAttributeMap.Instance.SetDragImageRaycast(true);
                            EditUI.Instance.BuildSelectMode(UIStructureSelectMode.State.Select, OCD);
                        }
                        else
                        {
                            SetupAttributeMap.Instance.AddSelectAssignObj();
                            SetupAttributeMap.Instance.SetDragImageRaycast(true);
                            EditUI.Instance.BuildSelectMode(UIStructureSelectMode.State.UnDropInventory, OCD);
                        }

                        EditUI.Instance.ShowSelectMode(true);
                        EditUI.Instance.isDragMode = false;
                    }
                }
            }
        }

        private bool EndDragTouch()
        {
            if (Input.touchCount > 1)
            {
                return Input.GetMouseButton(0) == false ||
                    Input.GetTouch(0).phase == TouchPhase.Ended ||
                    Input.GetTouch(1).phase == TouchPhase.Ended ||
                    Input.GetTouch(0).phase == TouchPhase.Canceled ||
                    Input.GetTouch(1).phase == TouchPhase.Canceled;
            }
            else if (Input.touchCount == 1)
            {
                return Input.GetMouseButton(0) == false ||
                    Input.GetTouch(0).phase == TouchPhase.Ended ||
                    Input.GetTouch(0).phase == TouchPhase.Canceled;
            }
            else
            {
                return Input.GetMouseButton(0) == false;
            }
        }

        private Vector3     v3StartPoint = Vector3.zero;
        public void OnPointerDown(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(RT, eventData.position, EditUI.I.EditUI_Canvs.worldCamera, out v2BasicPos);

            v3StartPoint = Input.mousePosition;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            EndDrag();
        }

        private void InitPointer()
        {
            fScrollOnly = false;
            fCreate = false;
            fThumbImgDragging = false;
        }

        private void CreateThumbImg()
        {
            fThumbImgDragging = true;
            fCreate   = true;
            v2PrevPos = v2BasicPos;

            thumbDragImg = Instantiate(OCD.ObjImg , Vector3.zero , Quaternion.identity , OCD.ObjImg.transform.parent);
            thumbDragImg.SetNativeSize();
            thumbDragImg.transform.localPosition = v2PrevPos;

            thumbDragImg.color = new Color(1,1,1,0);
            thumbDragImg.DOColor(new Color(1,1,1,1) , EditUI.I.DragCreateDuration);
            thumbDragImg.transform.localScale = Vector3.zero;
            thumbDragImg.transform.DOScale(Vector3.one * OCD.SkinSize * 1.2f , EditUI.I.DragCreateDuration).SetEase(Ease.OutBack);

            RT.SetAsLastSibling();
            //EditUI.Instance.ShowTipText(EditUI.EditState.DragMode);
            EditUI.I.soundPlayer[0].Play();
            StartDrag();
        }

        private void StartDrag()
        {
            if (fCreate == false)
                return;

            if (thumbDragImg == null)
                return;
            
            if (Input.GetMouseButton(0) == false)
                return;
            
            if (SetupAttributeMap.I.draggingOCD != null)
                return;

            //fThumbImgDragging = true;
            if (thumbDragImg != null)
            {
                thumbDragImg.DOColor(new Color(1, 1, 1, 0), EditUI.I.DragCreateDuration);
                thumbDragImg.transform.DOScale(Vector3.zero, EditUI.I.DragCreateDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    DestroyImmediate(thumbDragImg.gameObject);
                    thumbDragImg = null;
                });
            }

            --OCD.Count;
            SetStateValue();
            SetupAttributeMap.I.New(OCD, true);
            SetupAttributeMap.Instance.AssignObjInfoPopupOnOff(false);
            EditUI.Instance.BuildSelectMode(UIStructureSelectMode.State.None, OCD);
            EditUI.Instance.isDragMode = true;
        }
    }
}
