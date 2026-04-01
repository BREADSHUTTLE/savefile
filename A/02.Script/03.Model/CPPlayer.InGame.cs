using CAPYBARA.Definition;
using System;
using UnityEngine;

namespace CAPYBARA
{
    public static partial class CPPlayer
    {
        public static class InGame
        {
            public static Action<GameType, GameMode, CAPYBARA.lobby.RoomInfo> EnterInGame;
            public static Action<GameType> LeaveGame;
            public static Action<GameType> MoveTable;
            public static bool isMovingTable = false;
            public static GameMode currentGameMode;
            public static GameType currentGameType;
            public static CAPYBARA.lobby.RoomInfo currentRoomInfo;
            public static Action<GameType, EmotionInfo> emotionExpressEvent;
            public static bool haveKickVote = false;

            #region  toastPopup 

            #endregion

            public static Action<bool> AFKPopupActive;
            
            public static bool isUserAFK = false;
            public static bool AFKPopupActiveFlag = false;
            public static bool isInGame = false;

            public static Action<IAPProduct> afterPurchasePopup;

            public static ItemID currentEquippedEmoticon;
            

            public static void Dispose()
            {
                EnterInGame = null;
                LeaveGame = null;
                MoveTable = null;
              
                AFKPopupActive = null;
                afterPurchasePopup = null;
                emotionExpressEvent = null;
            }
        }
    }
}
