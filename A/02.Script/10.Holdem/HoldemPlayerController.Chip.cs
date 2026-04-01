using Cysharp.Threading.Tasks;
using CAPYBARA.Core;
using CAPYBARA.holdem;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    /// <summary>
    /// 베팅/칩 데이터 처리 및 표시
    /// </summary>
    public partial class HoldemPlayerController
    {
        public void BetChipData(long bet, long OwnedChip)
        {
            holdemPlayerInfo.roundBetChipList.Add(bet);
            SetCurrentOwnedChip(OwnedChip);

            view.roundBetChipObj.SetActive(true);
            view.roundBetChiptext.text = Extension.ToKoreanFormat(GetRoundBetChip());
        }

        private long GetRoundBetChip()
        {
            long totalBet = 0;
            for (int i = 0; i < holdemPlayerInfo.roundBetChipList.Count; i++)
            {
                totalBet += holdemPlayerInfo.roundBetChipList[i];
            }

            return totalBet;
        }

        public void ClearCurrentRoundBetHistory()
        {
            holdemPlayerInfo.roundBetChipList.Clear();
            view.roundBetChipObj.SetActive(false);
        }

        public void SetCurrentOwnedChip(long OwnedChip)
        {
            if (holdemPlayerInfo == null)
                return;
            if (holdemPlayerInfo.playerInfo == null)
                return;
            holdemPlayerInfo.playerInfo.Chip = OwnedChip;

            if (isMe)
            {
                CPPlayer.UserInfo.userDatabase.User.Gold = OwnedChip;
            }

            view.currentOwnedChip.text = Extension.ToKoreanFormat(holdemPlayerInfo.playerInfo.Chip, Extension.KoreanFormatMode.Planning);
            view.currentOwnedChipInactive.text = Extension.ToKoreanFormat(holdemPlayerInfo.playerInfo.Chip, Extension.KoreanFormatMode.Planning);
            
        }

        public void SetActionData(Partial.ActionType actionType, Partial.BetSizeType holdemactionType, long bet, long amount)
        {
            ActionToDisplay(holdemactionType);
            BetChipData(bet, amount);
        }

        public void BetImageActive(bool isactive = false)
        {
            if (holdemPlayerInfo.playerInfo == null)
                return;
            view.stampParentObj.gameObject.SetActive(isactive);
        }

        public void SetTotalBet(long totalBet)
        {
            currentTotalBet = totalBet;
        }

        public void SetBlind(bool smallb)
        {
            Image img = view.bigBlind;
            if (smallb)
                img = view.smallBlind;
            if (!img.gameObject.activeSelf)
                img.gameObject.SetActive(true);

            view.blindParent.gameObject.SetActive(true);
            view.blindAnim.Play("Blind");
            DeactivateBlindAnimAfterPlay().Forget();
        }

        private async UniTaskVoid DeactivateBlindAnimAfterPlay()
        {
            float clipLength = 0f;
            foreach (var clip in view.blindAnim.runtimeAnimatorController.animationClips)
            {
                if (clip.name == "Blind")
                {
                    clipLength = clip.length;
                    break;
                }
            }
            await UniTask.Delay(System.TimeSpan.FromSeconds(clipLength));
            view.blindParent.gameObject.SetActive(false);
        }


    }
}