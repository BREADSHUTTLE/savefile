// StoreListView.cs - StoreListView implementation file
//
// Description      : StoreListView
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2019/06/17
// Last Update      : 2019/06/17
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dorothy.DataPool;

namespace Dorothy.UI
{
    public class StoreListView : UICanvasListView
    {
        private StoreData storeData = null;
        private string how = "";
        private UserTracking.Location location = UserTracking.Location.none;

        public void Build(StoreData data, string trackingHow, UserTracking.Location trackingLocation)
        {
            if (data == null)
                return;

            storeData = data;
            how = trackingHow;
            location = trackingLocation;

            base.Init(storeData.goodsList.Count);
        }

        protected override void GetItemDatum(RectTransform itemRect, int index, bool isShow)
        {
            var item = itemRect.GetComponent<StoreListViewItem>();
            item.Build(storeData.goodsList[index], how, location);
        }

        protected override void ClearItem(RectTransform _item, int index)
        {

        }
    }
}
