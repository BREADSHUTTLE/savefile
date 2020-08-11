// UICanvasListView.cs - UICanvasListView implementation file
//
// Description      : UICanvasListView
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2018/02/27
// Last Update      : 2019/03/05
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace Dorothy.UI
{
    [AddComponentMenu("ST.MARIA/UI/UGUI/Canvas List View"), RequireComponent(typeof(ScrollRect))]
    public abstract class UICanvasListView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public enum ScrollType
        {
            Infinite,
            //Disable,
        }

        [SerializeField] protected ScrollType scrollType = ScrollType.Infinite;
        [SerializeField] private bool endDragCenter = false;
        [SerializeField] private GameObject item = null;
        [SerializeField] private int spareItemCount = 3;
        [SerializeField] private int redeemCount = 2;
        [SerializeField] private RectTransform posFocusItem;
        [SerializeField] private HorizontalOrVerticalLayoutGroup group;
        /*[SerializeField]*/ protected Scrollbar horizontalScrollBar;
        /*[SerializeField]*/ protected Scrollbar verticalScrollBar;
        [SerializeField] private float focusCenterTime = 0.3f;

        public Action ActionFront = null;
        public Action ActionBack = null;
        public Action<int> ActionCenter = null;
        public Action ActionCenterTweenComplete = null;
        public bool isMoveRelative = false;

        protected bool isDrag = false;
        protected int centerChildIndex;
        protected List<RectTransform> objectTempList = new List<RectTransform>();

        private DragDirection dragDirection = DragDirection.None;
        private int objDistance;
        private float[] childDistance;
        private float minDistance;
        private int showCount = 0;
        private int listCount = 0;
        private float spacing = 0;
        [SerializeField] private Vector3 prevPos;
        private List<int> indexList = new List<int>();
        private RectTransform contentRect = null;
        private ScrollRect scrollRect;
        private Vector2 pressPos = new Vector2(0f, 0f);
        private Vector2 dragPos = new Vector2(0f, 0f);

        private bool isMove = false;

        public enum DragDirection
        {
            Horizontal,
            Vertical,
            None,
        }

        public int ViewCount
        {
            get { return showCount - spareItemCount; }
        }

        public int ListCount
        {
            get { return listCount; }
        }

        public float ItemWidth
        {
            get { return item.GetComponent<RectTransform>().rect.width; }
        }

        public float ItemHeight
        {
            get { return item.GetComponent<RectTransform>().rect.height; }
        }

        public float Spacing
        {
            get { return spacing; }
        }

        public DragDirection Direction
        {
            get { return dragDirection; }
        }

        public int CenterIndex
        {
            get { return centerChildIndex; }
            set { centerChildIndex = value; }
        }

        public void SetItemActive(bool active)
        {
            item.SetActive(active);
        }

        protected virtual void ResetPosition()
        {
            for (int i = 0; i < showCount; i++)
                SetPosition(objectTempList[i], indexList[i]);
        }

        protected abstract void GetItemDatum(RectTransform itemRect, int index, bool isShow = true);
        protected abstract void ClearItem(RectTransform _item, int index);

        public virtual void OnMoveRelative(Vector3 localPosition, Vector2 velocity)
        {
        }

        protected void Reset()
        {
            for (int i = 0; i < objectTempList.Count; i++)
                Destroy(objectTempList[i].gameObject);

            objectTempList.Clear();
        }

        protected void Init(int count)
        {
            listCount = count;

            scrollRect = GetComponent<ScrollRect>();
            RectTransform myRect = GetComponent<RectTransform>();
            RectTransform itemRect = item.GetComponent<RectTransform>();
            contentRect = scrollRect.content;
            dragDirection = scrollRect.horizontal == true ? DragDirection.Horizontal : DragDirection.Vertical;

            if (listCount == 0 || scrollRect == null || itemRect == null || myRect == null)
                return;

            if (scrollRect.horizontalScrollbar != null)
                horizontalScrollBar = scrollRect.horizontalScrollbar;

            if (scrollRect.verticalScrollbar != null)
                verticalScrollBar = scrollRect.verticalScrollbar;

            if (dragDirection == DragDirection.Horizontal)
            {
                if (contentRect.GetComponent<HorizontalLayoutGroup>() != null)
                    spacing = contentRect.GetComponent<HorizontalLayoutGroup>().spacing;

                if (scrollType == ScrollType.Infinite /*|| scrollType == ScrollType.Disable*/)
                    showCount = Mathf.RoundToInt(myRect.rect.width / itemRect.rect.width);
                else
                    showCount = listCount;

                scrollRect.movementType = ScrollRect.MovementType.Elastic;

                if (scrollType == ScrollType.Infinite /*|| scrollType == ScrollType.Disable*/)
                {
                    if (listCount <= showCount)
                        scrollRect.movementType = ScrollRect.MovementType.Clamped;
                }

                if (listCount <= showCount + spareItemCount)
                    showCount = listCount;
                else
                    showCount += spareItemCount;

                float contentWidth = ((itemRect.rect.width + spacing) * listCount) - spacing;
                contentRect.sizeDelta = new Vector2(contentWidth, contentRect.rect.height);
                contentRect.localPosition = new Vector3(0, contentRect.localPosition.y, contentRect.localPosition.z);

                scrollRect.horizontalNormalizedPosition = 0f;
                prevPos = new Vector3(contentRect.localPosition.x - ((itemRect.rect.width * 0.5f) * redeemCount/* * spareItemCount*/), contentRect.localPosition.y, contentRect.localPosition.z);
            }
            else if(dragDirection == DragDirection.Vertical)
            {
                if (contentRect.GetComponent<VerticalLayoutGroup>() != null)
                    spacing = contentRect.GetComponent<VerticalLayoutGroup>().spacing;

                if (scrollType == ScrollType.Infinite /*|| scrollType == ScrollType.Disable*/)
                    showCount = Mathf.RoundToInt(myRect.rect.height / itemRect.rect.height);
                else
                    showCount = listCount;

                scrollRect.movementType = ScrollRect.MovementType.Elastic;

                if (scrollType == ScrollType.Infinite /*|| scrollType == ScrollType.Disable*/)
                {
                    if (listCount <= showCount)
                        scrollRect.movementType = ScrollRect.MovementType.Clamped;
                }

                if (listCount <= showCount + spareItemCount)
                    showCount = listCount;
                else
                    showCount += spareItemCount;

                float contentHeight = ((itemRect.rect.height + spacing) * listCount) - spacing;
                contentRect.sizeDelta = new Vector2(contentRect.rect.width, contentHeight);
                contentRect.localPosition = new Vector3(contentRect.localPosition.x, 0, contentRect.localPosition.z);

                scrollRect.verticalNormalizedPosition = 1f;

                prevPos = new Vector3(contentRect.localPosition.x, contentRect.localPosition.y + ((itemRect.rect.height * 0.5f) * redeemCount/* * spareItemCount*/), contentRect.localPosition.z);
            }


            indexList.Clear();

            //if (scrollType == ScrollType.Disable)
            //{
            //    for (int i = 0; i < listCount; ++i)
            //        indexList.Add(i);

            //    if (objectTempList.Count == 0)
            //    {
            //        for (int j = 0; j < listCount; ++j)
            //        {
            //            GameObject obj = Instantiate(item) as GameObject;
            //            RectTransform rect = obj.GetComponent<RectTransform>();

            //            rect.transform.SetParent(contentRect);
            //            rect.localPosition = new Vector3(0, 0, 0f);
            //            rect.localScale = new Vector3(1f, 1f, 1f);
            //            rect.gameObject.SetActive(true);
            //            objectTempList.Add(rect);
            //        }
            //    }
            //}
            //else
            {
                for (int i = 0; i < showCount; i++)
                    indexList.Add(i);

                if (objectTempList.Count == 0)
                {
                    for (int i = 0; i < showCount; i++)
                    {
                        GameObject obj = Instantiate(item) as GameObject;
                        RectTransform rect = obj.GetComponent<RectTransform>();

                        rect.transform.SetParent(contentRect);
                        rect.localPosition = new Vector3(0, 0, 0f);
                        rect.localScale = new Vector3(1f, 1f, 1f);
                        rect.gameObject.SetActive(true);
                        // 스위칭을 위한 리스트 등록
                        objectTempList.Add(rect);
                    }
                }
            }

            childDistance = new float[objectTempList.Count];
            item.SetActive(false);
            ResetPosition();
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
                ActionCenter += SetListScrollPos;
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
                ActionCenter -= SetListScrollPos;
        }

        protected virtual void Update()
        {
            if (listCount == 0)
                return;

            if (isMoveRelative)
            {
                //if (isDrag == true)
                {
                    if (contentRect != null)
                        //OnMoveRelative(scrollRect.normalizedPosition);
                        OnMoveRelative(contentRect.localPosition, scrollRect.velocity);
                        //OnMoveRelative(contentRect.localPosition, contentRect.sizeDelta);
                }
            }

            if (scrollType == ScrollType.Infinite)
            {
                OnMoveItem();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDrag = true;
            pressPos = eventData.pressPosition;
            //Debug.Log("press : " + pressPos);
        }

        public void OnDrag(PointerEventData eventData)
        {
            //OnMoveItem();
            isMove = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            dragPos = eventData.position;
            //Debug.Log("drag : " + dragPos);

            SetChildCenterIndex();

            isDrag = false;

            if (endDragCenter)
            {
                if (ActionCenter != null)
                    ActionCenter(centerChildIndex);
            }
        }

        private void SetChildCenterIndex()
        {
            if (posFocusItem == null)
                return;

            if (dragDirection == DragDirection.Horizontal)
            {
                for (int i = 0; i < objectTempList.Count; i++)
                    childDistance[i] = Mathf.Abs((posFocusItem.transform.position.x + 
                                                (dragPos.x > pressPos.x ? -(posFocusItem.rect.width * 0.5f) : (posFocusItem.rect.width * 0.5f)))
                                                - objectTempList[i].transform.position.x);

                minDistance = Mathf.Min(childDistance);
                for (int j = 0; j < objectTempList.Count; j++)
                {
                    if (minDistance == childDistance[j])
                    {
                        if (scrollType == ScrollType.Infinite)
                            centerChildIndex = indexList[j];
                        else
                            centerChildIndex = j;
                    }
                }
            }
            else if (dragDirection == DragDirection.Vertical)
            {
                for (int i = 0; i < objectTempList.Count; i++)
                    childDistance[i] = Mathf.Abs((posFocusItem.transform.position.y +
                                                (dragPos.y > pressPos.y ? -(posFocusItem.rect.height * 0.5f) : (posFocusItem.rect.height * 0.5f)))
                                                - objectTempList[i].transform.position.y);

                minDistance = Mathf.Min(childDistance);
                for (int j = 0; j < objectTempList.Count; j++)
                {
                    if (minDistance == childDistance[j])
                    {
                        if (minDistance == childDistance[j])
                        {
                            if (scrollType == ScrollType.Infinite)
                                centerChildIndex = indexList[j];
                            else
                                centerChildIndex = j;
                        }
                    }
                }
            }
        }

        private void SetListScrollPos(int index)
        {
            if (dragDirection == DragDirection.Horizontal)
            {
                float itemWidth = ItemWidth + group.spacing;
                float rectWidth = gameObject.GetComponent<RectTransform>().rect.width;
                float listWidth = (itemWidth * ListCount) - rectWidth;
                float pos = index * (itemWidth / listWidth) - ((rectWidth - itemWidth) / 2f / listWidth);
                
                pos = Mathf.Max(0f, pos);
                pos = Mathf.Min(1f, pos);
                scrollRect.DOHorizontalNormalizedPos(pos, focusCenterTime).SetEase(Ease.OutCubic).OnComplete(()=> 
                {
                    if (ActionCenterTweenComplete != null)
                    {
                        ActionCenterTweenComplete();
                        ActionCenterTweenComplete = null;
                    }
                });
            }
            else if (dragDirection == DragDirection.Vertical)
            {
                float itemHeight = ItemHeight + group.spacing;
                float rectHeight = gameObject.GetComponent<RectTransform>().rect.height;
                float listHeight = (itemHeight * ListCount) - rectHeight;
                float pos = index * (itemHeight / listHeight) - ((rectHeight - itemHeight) / 2f / listHeight);
                
                pos = Mathf.Max(0f, pos);
                pos = Mathf.Min(1f, pos);
                scrollRect.DOVerticalNormalizedPos(1f - pos, focusCenterTime).SetEase(Ease.OutCubic).OnComplete(() => 
                {
                    if (ActionCenterTweenComplete != null)
                    {
                        ActionCenterTweenComplete();
                        ActionCenterTweenComplete = null;
                    }
                });
            }
        }

        private void SetPosition(RectTransform _item, int index)
        {
            GetItemDatum(_item, index);

            if (dragDirection == DragDirection.Horizontal)
            {
                //float contentWidth = (_item.rect.width + spacing) * showCount;
                float itemPosition = (_item.rect.width + spacing) * index;
                float itemRadius = _item.rect.width * 0.5f;

                float pos = /*contentWidth + */(itemPosition + itemRadius);

                _item.localPosition = new Vector3(pos, _item.localPosition.y, _item.localPosition.z);
                _item.name = index.ToString();
            }
            else if (dragDirection == DragDirection.Vertical)
            {
                float itemPosition = (_item.rect.height + spacing) * index;
                float itemRadius = _item.rect.height * 0.5f;

                float pos = itemPosition + itemRadius;

                _item.localPosition = new Vector3(_item.localPosition.x, -pos, _item.localPosition.z);
                _item.name = index.ToString();
            }
        }

        private void OnMoveItem()
        {
            RectTransform itemRect = item.GetComponent<RectTransform>();

            if (dragDirection == DragDirection.Horizontal)
            {
                if (contentRect.localPosition.x > prevPos.x + (itemRect.rect.width * 0.5f))
                {
                    SetFrontItem();
                }
                else if (contentRect.localPosition.x < prevPos.x - (itemRect.rect.width * 0.5f))
                { 
                    SetBackItem();
                }
            }
            else if (dragDirection == DragDirection.Vertical)
            {
                if (contentRect.localPosition.y < prevPos.y - (itemRect.rect.height * 0.5f))
                    SetFrontItem();
                else if (contentRect.localPosition.y > prevPos.y + (itemRect.rect.height * 0.5f))
                    SetBackItem();
            }
        }

        //private void OnDisableItem()
        //{
        //    RectTransform itemRect = item.GetComponent<RectTransform>();
            
        //    float leftViewPort = scrollRect.viewport.rect.xMin;
        //    float rightViewPort = scrollRect.viewport.rect.xMax;
            
        //    if (dragDirection == DragDirection.Horizontal)
        //    {
        //        for (int i = 0; i < objectTempList.Count; ++i)
        //        {
        //            if ((objectTempList[i].position.x + (objectTempList[i].rect.width)) >= leftViewPort &&
        //                (objectTempList[i].position.x + (objectTempList[i].rect.width)) <= rightViewPort)
        //            {
        //                //Debug.Log("item : " + i + "/" + "true" + "/" + (objectTempList[i].position.x + (objectTempList[i].rect.width)));
        //                //TRUE
        //                GetItemDatum(objectTempList[i], indexList[i], true);
        //            }
        //            else
        //            {
        //                //Debug.Log("item : " + i + "/" + "false" + "/" + (objectTempList[i].position.x + (objectTempList[i].rect.width)));
        //                //FALSE
        //                GetItemDatum(objectTempList[i], indexList[i], false);
        //            }
        //        }
        //    }
        //}

        private void SetFrontItem()
        {
            if (indexList[0] <= 0)
                return;

            ClearItem(objectTempList[objectTempList.Count - 1], indexList[indexList.Count - 1]);

            objectTempList.Insert(0, objectTempList[objectTempList.Count - 1]);
            objectTempList.RemoveAt(objectTempList.Count - 1);

            indexList.Insert(0, indexList[0] - 1);
            indexList.RemoveAt(indexList.Count - 1);

            if (dragDirection == DragDirection.Horizontal)
            {
                prevPos.Set((prevPos.x + spacing) + item.GetComponent<RectTransform>().rect.width, prevPos.y, prevPos.z);
            }
            else if (dragDirection == DragDirection.Vertical)
            {
                prevPos.Set(prevPos.x, (prevPos.y - spacing) - item.GetComponent<RectTransform>().rect.height, prevPos.z);
            }
            
            SetPosition(objectTempList[0], indexList[0]);
        }

        private void SetBackItem()
        {
            if (listCount <= indexList[indexList.Count - 1] + 1)
                return;

            ClearItem(objectTempList[0], indexList[0]);

            objectTempList.Add(objectTempList[0]);
            objectTempList.RemoveAt(0);

            indexList.Add(indexList[indexList.Count - 1] + 1);
            indexList.RemoveAt(0);

            if (dragDirection == DragDirection.Horizontal)
            {
                //prevPos.Set(prevPos.x - item.GetComponent<RectTransform>().rect.width, prevPos.y, prevPos.z);
                prevPos.Set((prevPos.x - spacing) - (item.GetComponent<RectTransform>().rect.width), prevPos.y, prevPos.z);
            }
            else if (dragDirection == DragDirection.Vertical)
            {
                prevPos.Set(prevPos.x, (prevPos.y + spacing) + item.GetComponent<RectTransform>().rect.height, prevPos.z);
            }

            SetPosition(objectTempList[objectTempList.Count - 1], indexList[indexList.Count - 1]);
        }
    }
}
