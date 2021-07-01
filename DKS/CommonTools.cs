/// CommonTools.cs
/// Create : 2020.10.22
/// Author : KimJuHee
/// 

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DFramework.Common
{
    public static class CommonTools
    {
        public static GameObject CreateGameObject(GameObject gameObject, GameObject parent)
        {
            GameObject go = GameObject.Instantiate(gameObject) as GameObject;
            if (go != null && parent != null)
            {
                Transform t = go.transform;
                t.SetParent(parent.transform);
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
                go.layer = parent.layer;
            }

            return go;
        }

        public static GameObject CreateGameObject(GameObject gameObject, Vector3 position, Vector3 scale, GameObject parent)
        {
            GameObject go = GameObject.Instantiate(gameObject) as GameObject;
            if (go != null && parent != null)
            {
                Transform t = go.transform;
                t.SetParent(parent.transform);
                t.localPosition = Vector3.zero;
                t.position = position;
                t.localRotation = Quaternion.identity;
                t.localScale = scale;
                go.layer = parent.layer;
            }

            return go;
        }

        public static T CreateGameObject<T>(GameObject gameObject, Vector3 position, Vector3 scale, GameObject parent)
        {
            GameObject go = GameObject.Instantiate(gameObject) as GameObject;
            if (go != null && parent != null)
            {
                Transform t = go.transform;
                t.SetParent(parent.transform);
                t.localPosition = Vector3.zero;
                t.position = position;
                t.localRotation = Quaternion.identity;
                t.localScale = scale;
                go.layer = parent.layer;
            }

            return go.GetComponent<T>();
        }

        public static T GetEnumFromString<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value);
        }

        public static int[] GetIntArrayFromString(string value)
        {
            string[] strArray = value.Split(',');

            int[] intArray = new int[strArray.Length];
            for(int i = 0; i < strArray.Length; i++)
            {
                intArray[i] = Int32.Parse(strArray[i]);
            }

            return intArray;
        }

        public static List<int> GetIntListFromString(string value)
        {
            string[] strArray = value.Split(',');

            List<int> list = new List<int>();
            for (int i = 0; i < strArray.Length; i++)
            {
                list.Add(Int32.Parse(strArray[i]));
            }

            return list;
        }
    }
}
