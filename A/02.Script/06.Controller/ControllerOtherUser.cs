using System.Collections.Generic;
using System.Threading;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using Unity.VisualScripting;
using UnityEngine;

namespace CAPYBARA
{
    public class GameTotalRecord
    {
        public int winCount;
        public int loseCount;
    }
    public class ControllerOtherUser
    {
        ViewOtheruserInfo myView;
        CancellationTokenSource _cts;

        Dictionary<GameType, GameTotalRecord> gameAllRecordDic = new Dictionary<GameType, GameTotalRecord>();
        Dictionary<GameType, GameTotalRecord> gameTodayRecordDic = new Dictionary<GameType, GameTotalRecord>();

        GameType currentSelectedGameType;
        HistoryType currentSelectedhistoryType;

        bool isSendedFriend;
        bool isRequestedFriend;
        bool isAlreadyMyFriend;
        public ControllerOtherUser(ViewOtheruserInfo view, CancellationTokenSource cts)
        {
            myView = view;
            _cts = cts;
            CPPlayer.Otheruser.OpenOtherUserInfo += OpenView;
            currentSelectedGameType = GameType.ALL;
            currentSelectedhistoryType = HistoryType.Total;


            for (int i = 0; i < (int)GameType.END; i++)
            {
                GameTotalRecord record = new GameTotalRecord();
                gameAllRecordDic.Add((GameType)i, record);
                GameTotalRecord todayrecord = new GameTotalRecord();
                gameTodayRecordDic.Add((GameType)i, todayrecord);
            }
            for (int i = 0; i < myView.HistoryBtn.Length; i++)
            {
                int index = i;
                myView.HistoryBtn[index].onClick.AddListener(() => OnClickHistoryTypeBtn((HistoryType)index));
            }

            for (int i = 0; i < myView.GameTypeBtn.Length; i++)
            {
                int index = i;
                myView.GameTypeBtn[index].onClick.AddListener(() => OnClickGameTypeBtn((GameType)index));
            }
            myView.sendDirectMsg.onClick.AddListener(() => { OnClickSendMsg().Forget(); });
            myView.sendFriendReq.onClick.AddListener(() => { OnClickAddFriend().Forget(); });
            myView.closeView.onClick.AddListener(() => myView.gameObject.SetActive(false));
        }

        private void OpenView(string accountid)
        {
            SetUserInfoAndView(accountid).Forget();
        }

        private async UniTaskVoid SetUserInfoAndView(string accountid)
        {
           

        }

        void OnClickHistoryTypeBtn(HistoryType _type)
        {
            currentSelectedhistoryType = _type;
            myView.historyBtnselector.Show((int)currentSelectedhistoryType);
            myView.gameTypeselector.Show((int)currentSelectedGameType);

            string historytoText = null;
            int allhis = 0;
            float winrate = 0;
            if (currentSelectedhistoryType == HistoryType.Total)
            {
                allhis = gameAllRecordDic[currentSelectedGameType].winCount + gameAllRecordDic[currentSelectedGameType].loseCount;
                winrate = (allhis == 0) ? 0f : ((float)gameAllRecordDic[currentSelectedGameType].winCount / (float)allhis) * 100.0f;
                historytoText = string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.WinLoseWinrateRecord].StringToLocal, allhis, gameAllRecordDic[currentSelectedGameType].winCount, gameAllRecordDic[currentSelectedGameType].loseCount, winrate);
                myView.gameRecord.text = historytoText;
            }
            else
            {
                allhis = gameTodayRecordDic[currentSelectedGameType].winCount + gameTodayRecordDic[currentSelectedGameType].loseCount;
                winrate = (allhis == 0) ? 0f : ((float)gameTodayRecordDic[currentSelectedGameType].winCount / (float)allhis) * 100.0f;
                historytoText = string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.WinLoseWinrateRecord].StringToLocal, allhis, gameTodayRecordDic[currentSelectedGameType].winCount, gameTodayRecordDic[currentSelectedGameType].loseCount, winrate);
                myView.gameRecord.text = historytoText;
            }
        }
        void OnClickGameTypeBtn(GameType _type)
        {
            currentSelectedGameType = _type;
            myView.historyBtnselector.Show((int)currentSelectedhistoryType);
            myView.gameTypeselector.Show((int)currentSelectedGameType);

            string historytoText = null;
            int allhis = 0;
            float winrate = 0;

            if (currentSelectedhistoryType == HistoryType.Total)
            {
                allhis = gameAllRecordDic[currentSelectedGameType].winCount + gameAllRecordDic[currentSelectedGameType].loseCount;
                winrate = (allhis == 0) ? 0f : ((float)gameAllRecordDic[currentSelectedGameType].winCount / (float)allhis) * 100.0f;
                historytoText = string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.WinLoseWinrateRecord].StringToLocal, allhis, gameAllRecordDic[currentSelectedGameType].winCount, gameAllRecordDic[currentSelectedGameType].loseCount, winrate);
                myView.gameRecord.text = historytoText;
            }
            else
            {
                allhis = gameTodayRecordDic[currentSelectedGameType].winCount + gameTodayRecordDic[currentSelectedGameType].loseCount;
                winrate = (allhis == 0) ? 0f : ((float)gameTodayRecordDic[currentSelectedGameType].winCount / (float)allhis) * 100.0f;
                historytoText = string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.WinLoseWinrateRecord].StringToLocal, allhis, gameTodayRecordDic[currentSelectedGameType].winCount, gameTodayRecordDic[currentSelectedGameType].loseCount, winrate);
                myView.gameRecord.text = historytoText;
            }

        }


        public async UniTask OnClickAddFriend()
        {
          
        }

        public async UniTask OnClickSendMsg()
        {
           

        }
    }
}