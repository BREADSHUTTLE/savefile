#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using CAPYBARA;
using UnityEditor;
using UnityEngine;

namespace CAPYBARA.Editor
{
    [CustomPropertyDrawer(typeof(SearchableEnumAttribute))]
    public class SearchableEnumDrawer : PropertyDrawer
    {
        private string searchText = "";
        private bool isOpen = false;
        private Vector2 scroll;

        private const float ROW_HEIGHT = 18f;
        private const float MAX_LIST_HEIGHT = 180f;
        private const float SEARCH_HEIGHT = 20f;
        private const float PADDING = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect btnRect = new Rect(position.x, position.y, position.width, ROW_HEIGHT);
            Rect labelRect = EditorGUI.PrefixLabel(btnRect, label);

            string currentName = property.enumNames[property.enumValueIndex];
            if (GUI.Button(labelRect, currentName, EditorStyles.popup))
                isOpen = !isOpen;

            if (isOpen)
            {
                var filtered = GetFiltered(property);

                float listHeight = Mathf.Min(filtered.Count * ROW_HEIGHT, MAX_LIST_HEIGHT);
                float totalDropHeight = SEARCH_HEIGHT + PADDING + listHeight;

                Rect searchRect = new Rect(labelRect.x, btnRect.yMax + PADDING, labelRect.width, SEARCH_HEIGHT);
                GUI.SetNextControlName("SearchField");
                searchText = EditorGUI.TextField(searchRect, searchText, EditorStyles.toolbarSearchField);

                Rect scrollViewRect = new Rect(labelRect.x, searchRect.yMax + PADDING, labelRect.width, listHeight);
                Rect contentRect = new Rect(0, 0, labelRect.width - 16f, filtered.Count * ROW_HEIGHT);

                scroll = GUI.BeginScrollView(scrollViewRect, scroll, contentRect);
                for (int i = 0; i < filtered.Count; i++)
                {
                    Rect rowRect = new Rect(0, i * ROW_HEIGHT, contentRect.width, ROW_HEIGHT);
                    bool isCurrent = filtered[i].idx == property.enumValueIndex;
                    if (isCurrent)
                        EditorGUI.DrawRect(rowRect, new Color(0.2f, 0.5f, 1f, 0.3f));

                    if (GUI.Button(rowRect, filtered[i].name, EditorStyles.label))
                    {
                        property.enumValueIndex = filtered[i].idx;
                        isOpen = false;
                        searchText = "";
                        GUI.FocusControl(null);
                    }
                }
                GUI.EndScrollView();
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float baseHeight = base.GetPropertyHeight(property, label);
            if (!isOpen)
                return baseHeight;

            var filtered = GetFiltered(property);
            float listHeight = Mathf.Min(filtered.Count * ROW_HEIGHT, MAX_LIST_HEIGHT);
            return baseHeight + PADDING + SEARCH_HEIGHT + PADDING + listHeight + PADDING;
        }

        private List<(string name, int idx)> GetFiltered(SerializedProperty property)
        {
            return property.enumNames
                .Select((name, idx) => (name, idx))
                .Where(e => string.IsNullOrEmpty(searchText) ||
                            e.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }
    }
}
#endif
