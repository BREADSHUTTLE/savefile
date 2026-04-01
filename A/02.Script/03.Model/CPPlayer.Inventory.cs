using System;
using System.Collections.Generic;
using CAPYBARA.Core;
using CAPYBARA.Definition;
using CAPYBARA.lobby;
using UnityEngine;

namespace CAPYBARA
{
    public static partial class CPPlayer
    {
        public static class Inventory
        {
            public static int classNumber=0;
            public static ClassInfoRes classInfo;  // 클래스 정보
            public static Action classUpdateCallback;  // 클래스 정보 갱신 콜백

            public static Action<IAPType, bool, Action> shopClassToastPopup;
            public static Action<IAPProduct,Sprite,Action<int>> shopnormalToastPopup;
            
            public static Action eventHasBooster;

            public static InventoryRes inventoryInfo;
            public static Action inventoryUpdateCallback;

            public static Points myPoints;
            public static Action pointsUpdateCallback;

            public static lobby.Inventory equippedAvatar;
            
            // 클래스 만료 팝업
            public static bool classExpiredNotified;
            public static string lastExpiredClassName;

            public static string GetClassDisplayNameFromItemId(string itemId)
            {
                if (string.IsNullOrEmpty(itemId))
                    return "";

                return itemId switch
                {
                    nameof(ItemID.CLASS_B) => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ClassB].StringToLocal,
                    nameof(ItemID.CLASS_A) => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ClassA].StringToLocal,
                    nameof(ItemID.CLASS_S) => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ClassS].StringToLocal,
                    _ => ""
                };
            }

            public static string GetClassDisplayName(ClassInfoRes info)
            {
                if (info == null)
                    return "";

                string grade = info.ItemId switch
                {
                    nameof(ItemID.CLASS_B) => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ClassB].StringToLocal,
                    nameof(ItemID.CLASS_A) => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ClassA].StringToLocal,
                    nameof(ItemID.CLASS_S) => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ClassS].StringToLocal,
                    _ => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Class].StringToLocal
                };

                string paymentType = info.ClassPaymentType?.ToUpper() switch
                {
                    "SINGLE" => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Days30Ticket].StringToLocal,
                    "RECURRING" => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Subscription].StringToLocal,
                    _ => ""
                };

                return string.IsNullOrEmpty(paymentType) ? grade : $"{grade} {paymentType}";
            }

            public static bool CheckClassExpiredLocally()
            {
                if (classExpiredNotified)
                    return false;

                int effectEndAt = 0;
                string expiredItemId = null;

                if (classInfo != null && classInfo.EffectEndAt > 0)
                {
                    effectEndAt = classInfo.EffectEndAt;
                }
                else if (classNumber > 0 && inventoryInfo?.Inventory != null)
                {
                    foreach (var inv in inventoryInfo.Inventory)
                    {
                        if ((inv.ItemId == nameof(ItemID.CLASS_B) ||
                             inv.ItemId == nameof(ItemID.CLASS_A) ||
                             inv.ItemId == nameof(ItemID.CLASS_S)) &&
                            inv.EffectEndAt > 0)
                        {
                            effectEndAt = inv.EffectEndAt;
                            expiredItemId = inv.ItemId;
                            break;
                        }
                    }
                }

                if (effectEndAt <= 0)
                    return false;

                int now = (int)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (now < effectEndAt)
                    return false;

                classExpiredNotified = true;
                lastExpiredClassName = classInfo != null
                    ? GetClassDisplayName(classInfo)
                    : GetClassDisplayNameFromItemId(expiredItemId ?? "");
                classInfo = null;
                classNumber = 0;
                classUpdateCallback?.Invoke();
                return true;
            }

            public static bool CheckClassExpiredFromServer(int previousClassNumber)
            {
                if (classNumber > 0)
                {
                    classExpiredNotified = false;
                    return false;
                }

                if (classExpiredNotified)
                    return false;
                if (previousClassNumber <= 0)
                    return false;

                classExpiredNotified = true;
                return true;
            }

            public static void Dispose()
            {
                shopClassToastPopup = null;
                shopnormalToastPopup = null;
                eventHasBooster = null;
                inventoryUpdateCallback = null;
                pointsUpdateCallback = null;
                classExpiredNotified = false;
                lastExpiredClassName = null;
            }
        }

    }
}