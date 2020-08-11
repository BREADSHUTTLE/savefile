// ImageGradationColor.cs - ImageGradationColor implementation file
//
// Description      : PopupShop
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2018/03/12
// Last Update      : 2018/03/12
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ST.MARIA.UI
{
    [AddComponentMenu("ST.MARIA/UI/UGUI/UI Gradation Color")]
    public class UIGradationColor : BaseMeshEffect
    {
        [SerializeField] private Color top = Color.white;
        [SerializeField] private Color bottom = Color.white;
        [SerializeField] private Color left = Color.white;
        [SerializeField] private Color right = Color.white;

        public Color colorTop
        {
            get { return top; }
            set { if (top != value) top = value; }
        }

        public Color colorBottom
        {
            get { return bottom; }
            set { if (bottom != value) bottom = value; }
        }

        public Color colorLeft
        {
            get { return left; }
            set { if (left != value) left = value; }
        }

        public Color colorRight
        {
            get { return right; }
            set { if (right != value) right = value; }
        }

        public void SetAlpha(float alpha)
        {
            Color topColor = top;
            topColor.a = alpha;
            top = topColor;

            Color bottomColor = bottom;
            bottomColor.a = alpha;
            bottom = bottomColor;

            Color leftColor = left;
            leftColor.a = alpha;
            left = leftColor;

            Color rightColor = right;
            rightColor.a = alpha;
            right = rightColor;
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if(IsActive() == false)
                return;

            List<UIVertex> vertexList = new List<UIVertex>();

            vh.GetUIVertexStream(vertexList);

            ModifyVertices(vertexList);

            vh.Clear();
            vh.AddUIVertexTriangleStream(vertexList);
        }

        private void ModifyVertices(List<UIVertex> list)
        {
            if(IsActive() == false || list == null || list.Count == 0)
                return;

            float minX = 0f, maxX = 0f;
            float minY = 0f, maxY = 0f;
            float width = 0f;
            float height = 0f;

            UIVertex newVertex;

            for(int i = 0; i < list.Count; i++)
            {
                if(i == 0)
                {
                    minX = Mathf.Min(minX, list[i].position.x);
                    minY = Mathf.Min(minY, list[i].position.y);
                    maxX = Mathf.Max(maxX, list[i].position.x);
                    maxY = Mathf.Max(maxY, list[i].position.y);

                    width = maxX - minX;
                    height = maxY - minY;
                }

                newVertex = list[i];

                Color colorOriginal = newVertex.color;
                Color colorVertical = Color.Lerp(bottom, top, (height > 0 ? (newVertex.position.y - minY) / height : 0));
                Color colorHorizontal = Color.Lerp(left, right, (width > 0 ? (newVertex.position.x - minX) / width : 0));

                newVertex.color = colorOriginal * colorVertical * colorHorizontal;

                list[i] = newVertex;
            }
        }
    }
}

