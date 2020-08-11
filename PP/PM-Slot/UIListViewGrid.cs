// ListViewGrid.cs : ListViewGrid implementation file
//
// Description      : ListViewGrid
// Author           : icoder
// Maintainer       : uhrain7761, icoder
// How to use       : 
// Created          : 2016/09/02
// Last Update      : 2017/11/03
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//

using UnityEngine;
using System.Collections.Generic;


namespace DUNK.UI
{
    [AddComponentMenu("DUNK/GUI/ListView Grid")]
    public class UIListViewGrid : UIGrid
    {
        protected List<UIListViewItem> itemList = new List<UIListViewItem>();
        private GameObject dummyStart = null;
        private GameObject dummyEnd = null;

        public UIBaseListView ListView
        {
            get; protected set;
        }

        public List<UIListViewItem> ItemList
        {
            get { return itemList; }
            set { itemList = value; }
        }

        public float ItemSize
        {
            get; protected set;
        }

        public int LineCount
        {
            get; protected set;
        }

        public int CurrentIndex
        {
            get; protected set;
        }

        public int PreviousIndex
        {
            get; protected set;
        }

        protected virtual void Awake()
        {
            ListView = GetComponentInParent<UIBaseListView>();
        }

        protected override void Start()
        {
            base.Start();

            if (ListView != null)
                ListView.OnStartGrid();
        }

        public override void Reposition()
        {
            if (Application.isPlaying == false)
            {
                base.Reposition();
            }
            else
            {
                if (mStarted == false)
                {
                    repositionNow = true;
                    return;
                }

                repositionNow = false;

                int x = 0;
                int y = 0;

                if (sorted == true)
                {
                    OnSort();
                }

                foreach (UIListViewItem item in ItemList)
                {
                    Transform t = item.transform;
                    float depth = t.localPosition.z;

                    t.localPosition =
                        (arrangement == Arrangement.Horizontal) ?
                        new Vector3(cellWidth * x, -cellHeight * y, depth) :
                        new Vector3(cellWidth * y, -cellHeight * x, depth);

                    if (++x >= maxPerLine && maxPerLine > 0)
                    {
                        x = 0;
                        ++y;
                    }
                }

                UIDraggablePanel drag = NGUITools.FindInParents<UIDraggablePanel>(gameObject);
                if (drag != null) drag.UpdateScrollbars(true);
            }

            OnReposition();
        }

        protected virtual void OnSort()
        {
            if (Application.isPlaying == true)
            {
                if (ListView != null)
                    ListView.OnSort();
            }
        }

        protected virtual void OnReposition()
        {
            if (LineCount == 0)
                return;

            int startLine = 0;

            UIListViewPanel panel = NGUITools.FindInParents<UIListViewPanel>(gameObject);
            if (panel != null)
            {
                Vector3 currentPosition = panel.CurrentPosition;
                bool resetPosition = false;

                startLine = Mathf.FloorToInt(
                    (arrangement == Arrangement.Horizontal ? currentPosition.y : currentPosition.x) / ItemSize);

                if (startLine < 0)
                    startLine = 0;

                if (startLine >= transform.childCount)
                {
                    startLine = 0;
                    resetPosition = true;
                }

                if (resetPosition == true)
                {
                    CurrentIndex = 0;
                    PreviousIndex = 0;
                    panel.ResetPosition();
                }
            }

            int index = 0;
            foreach (UIListViewItem item in itemList)
            {
                bool isActive = false;
                if ((index >= startLine * maxPerLine) && (index < (startLine + LineCount) * maxPerLine))
                    isActive = true;

                item.gameObject.SetActive(isActive);

                index++;
            }

            UpdateDummyForScroll();

            PreviousIndex = 0;
            Culling(0);
        }

        protected void UpdateDummyForScroll()
        {
            if (Application.isPlaying == false)
                return;

            if (itemList.Count < 1)
            {
                if (dummyStart != null)
                    dummyStart.SetActive(false);

                if (dummyEnd != null)
                    dummyEnd.SetActive(false);

                return;
            }

            if (dummyStart == null)
            {
                dummyStart = new GameObject();
                dummyStart.name = "GridAreaStart";
                dummyStart.transform.parent = transform.parent;
                dummyStart.AddComponent<UISprite>();
            }
            else
            {
                dummyStart.SetActive(true);
            }

            if (dummyEnd == null)
            {
                dummyEnd = new GameObject();
                dummyEnd.name = "GridAreaEnd";
                dummyEnd.transform.parent = transform.parent;
                dummyEnd.AddComponent<UISprite>();
            }
            else
            {
                dummyEnd.SetActive(true);
            }

            Bounds widgetBounds = NGUIMath.CalculateRelativeWidgetBounds(transform.parent, itemList[0].transform);
            dummyStart.transform.localScale = widgetBounds.size;
            dummyStart.transform.localPosition = widgetBounds.center;

            widgetBounds = NGUIMath.CalculateRelativeWidgetBounds(transform.parent, itemList[itemList.Count - 1].transform);
            dummyEnd.transform.localScale = widgetBounds.size;
            dummyEnd.transform.localPosition = widgetBounds.center;
        }

        public void SetClip(float width, float height)
        {
            if (width == 0f)
            {
                ItemSize = cellHeight;
                LineCount = Mathf.FloorToInt(height / cellHeight) + 2;
            }
            else
            {
                ItemSize = cellWidth;
                LineCount = Mathf.FloorToInt(width / cellWidth) + 2;
            }
        }

        public void OnStartSpring(SpringPanel springPanel)
        {
            if (Application.isPlaying == true)
            {
                if (ListView != null)
                    ListView.OnStartSpring(springPanel);
            }
        }

        public void Culling(float currentPosition)
        {
            if (LineCount == 0)
                return;

            if (Application.isPlaying == true)
            {
                if (ListView != null)
                    ListView.OnCulling(currentPosition);
            }

            CurrentIndex = Mathf.FloorToInt(currentPosition / ItemSize);

            if (CurrentIndex < 0)
                CurrentIndex = 0;

            if (CurrentIndex != PreviousIndex)
                Culling();
        }

        protected void Culling()
        {
            if (LineCount == 0)
                return;

            if (PreviousIndex == CurrentIndex)
                return;

            if (Application.isPlaying == true)
            {
                if (ListView != null)
                    ListView.OnCulling();
            }

            int endIndex;

            if ((PreviousIndex < CurrentIndex) && ((PreviousIndex + LineCount) > CurrentIndex))
                endIndex = CurrentIndex;
            else
                endIndex = PreviousIndex + LineCount;

            if ((endIndex >= (CurrentIndex + LineCount)) && (PreviousIndex < (CurrentIndex + LineCount)))
                PreviousIndex = CurrentIndex + LineCount;

            for (int i = PreviousIndex * maxPerLine; i < endIndex * maxPerLine; i++)
            {
                if (i < itemList.Count)
                {
                    if (itemList[i] != null)
                        itemList[i].gameObject.SetActive(false);
                }
            }

            for (int i = CurrentIndex * maxPerLine; i < (CurrentIndex + LineCount) * maxPerLine; i++)
            {
                if (i < itemList.Count)
                {
                    if (itemList[i] != null)
                        itemList[i].gameObject.SetActive(true);
                }
            }

            PreviousIndex = CurrentIndex;
        }
    }
}
