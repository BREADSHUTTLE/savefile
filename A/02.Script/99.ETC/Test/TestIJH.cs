using System;
using UnityEngine;

namespace CAPYBARA
{
    [Serializable]
    public class PlayerActionData
    {
        public int skillId;
        public float directionX;
        public float directionY;
    }

    public static class SerializationHelper
    {
        public static byte[] ToBytes<T>(T obj)
        {
            string json = JsonUtility.ToJson(obj);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public static T FromBytes<T>(byte[] data)
        {
            string json = System.Text.Encoding.UTF8.GetString(data);
            return JsonUtility.FromJson<T>(json);
        }
    }

 

}
