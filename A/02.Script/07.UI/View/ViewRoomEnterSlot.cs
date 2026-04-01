using System.Collections.Generic;
using BlackTree.Bundles;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.Definition;
using CAPYBARA.lobby;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class ViewRoomEnterSlot : MonoBehaviour
    {
        // public TMP_Text betMoney;
        public Image BGImage;
        public TMP_Text betMoneyTxt;
        public TMP_Text possibleMoney;
        public CPButton enterGame;
        public Image inactiveObj;
        public Image imgGoldIcon;
        public TMP_Text txtANTE;

        [SerializeField] private Color holdemBetMoneyTopColor;
        [SerializeField] private Color holdemBetMoneyBottomColor;
        [SerializeField] private Color holdemMinMoneyTopColor;
        [SerializeField] private Color holdemMinMoneyBottomColor;
        
        [SerializeField] private Color badugiBetMoneyTopColor;
        [SerializeField] private Color badugiBetMoneyBottomColor;
        [SerializeField] private Color badugiMinMoneyTopColor;
        [SerializeField] private Color badugiMinMoneyBottomColor;
        
        [SerializeField] private Color spokerBetMoneyTopColor;
        [SerializeField] private Color spokerBetMoneyBottomColor;
        [SerializeField] private Color spokerMinMoneyTopColor;
        [SerializeField] private Color spokerMinMoneyBottomColor;
        
        [HideInInspector] public GameType gameType;
        [HideInInspector] public GameMode gameMode;
        [HideInInspector]public lobby.RoomInfo inGameRoomdata;
        
        private List<lobby.RoomInfo>  badugiRoomdatas;

        public bool canEnterGame;
        
        public int RoomId
        {
            get
            {
                return inGameRoomdata?.RoomId ?? -1;
            }
        }
        public void Init(lobby.RoomInfo data,GameType _gameType,GameMode gamemode)
        {
            gameType = _gameType;
            inGameRoomdata = data;

            imgGoldIcon.gameObject.SetActive(true);
            possibleMoney.gameObject.SetActive(true);
            possibleMoney.text = Extension.ToKoreanFormat(data.MinBuyIn) + " " + StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.OrMore].StringToLocal;
            gameMode = gamemode;

            txtANTE.gameObject.SetActive(false);
            
            SetColorAndTheme();
        }
      
        public void Init(List<lobby.RoomInfo>  datas,GameType _gameType,GameMode gamemode)
        {
            gameType = _gameType;
            badugiRoomdatas = datas;
            imgGoldIcon.gameObject.SetActive(false);
            possibleMoney.gameObject.SetActive(false);
            gameMode = gamemode;

            txtANTE.gameObject.SetActive(true);
            
            SetColorAndTheme();
        }

        void SetColorAndTheme()
        {
            BGImage.sprite = LobbyResourcesBundle.Loaded.roomSlotBGSprites[(int)gameType];
            inactiveObj.sprite=LobbyResourcesBundle.Loaded.roomSlotBGSprites[(int)gameType];
            inactiveObj.gameObject.SetActive(false);

            if (gameType == GameType.HOLDEM)
            {
                betMoneyTxt.text = Extension.ToKoreanFormat(inGameRoomdata.Ante);

                VertexGradient betMoneyGrad = new VertexGradient(holdemBetMoneyTopColor, holdemBetMoneyTopColor, holdemBetMoneyBottomColor, holdemBetMoneyBottomColor);
                VertexGradient minMoneyGrad = new VertexGradient(holdemMinMoneyTopColor, holdemMinMoneyTopColor, holdemMinMoneyBottomColor, holdemMinMoneyBottomColor);

                betMoneyTxt.enableVertexGradient = true;
                betMoneyTxt.colorGradient = betMoneyGrad;

                possibleMoney.enableVertexGradient = true;
                possibleMoney.colorGradient = minMoneyGrad;
            }
            else if (gameType == GameType.LOW_BADUGI)
            {
                if (gameMode == GameMode.Default)
                {
                    betMoneyTxt.text = Extension.ToKoreanFormat(inGameRoomdata.Ante);
                }
                else
                {
                    betMoneyTxt.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.OneOnOne].StringToLocal;
                    txtANTE.text = "ANTE";
                }

                VertexGradient betMoneyGrad = new VertexGradient(badugiBetMoneyTopColor, badugiBetMoneyTopColor, badugiBetMoneyBottomColor, badugiBetMoneyBottomColor);
                VertexGradient minMoneyGrad = new VertexGradient(badugiMinMoneyTopColor, badugiMinMoneyTopColor, badugiMinMoneyBottomColor, badugiMinMoneyBottomColor);

                betMoneyTxt.enableVertexGradient = true;
                betMoneyTxt.colorGradient = betMoneyGrad;

                possibleMoney.enableVertexGradient = true;
                possibleMoney.colorGradient = minMoneyGrad;
                
                txtANTE.enableVertexGradient = true;
                txtANTE.colorGradient = minMoneyGrad;
            }
            else if (gameType == GameType.SEVEN_POKER)
            {
                betMoneyTxt.text = Extension.ToKoreanFormat(inGameRoomdata.Ante);

                VertexGradient betMoneyGrad = new VertexGradient(spokerBetMoneyTopColor, spokerBetMoneyTopColor, spokerBetMoneyBottomColor, spokerBetMoneyBottomColor);
                VertexGradient minMoneyGrad = new VertexGradient(spokerMinMoneyTopColor, spokerMinMoneyTopColor, spokerMinMoneyBottomColor, spokerMinMoneyBottomColor);

                betMoneyTxt.enableVertexGradient = true;
                betMoneyTxt.colorGradient = betMoneyGrad;

                possibleMoney.enableVertexGradient = true;
                possibleMoney.colorGradient = minMoneyGrad;
            }

            canEnterGame = false;
        }

        public void ActivateSlot()
        {
            bool active = false;

            if (gameType == GameType.HOLDEM)
            {
                if (inGameRoomdata.MaxBuyIn <= 0)
                {
                    active = CPPlayer.UserInfo.userDatabase.User.Gold >= inGameRoomdata.MinBuyIn;
                }
                else
                {
                    active=CPPlayer.UserInfo.userDatabase.User.Gold>=inGameRoomdata.MinBuyIn &&
                           CPPlayer.UserInfo.userDatabase.User.Gold<=inGameRoomdata.MaxBuyIn;    
                }
            }
            else if (gameType == GameType.SEVEN_POKER)
            {
                if (inGameRoomdata.MaxBuyIn <= 0)
                {
                    active = CPPlayer.UserInfo.userDatabase.User.Gold >= inGameRoomdata.MinBuyIn;
                }
                else
                {
                    active=CPPlayer.UserInfo.userDatabase.User.Gold>=inGameRoomdata.MinBuyIn &&
                           CPPlayer.UserInfo.userDatabase.User.Gold<=inGameRoomdata.MaxBuyIn;    
                }
            }
            else    if (gameType == GameType.LOW_BADUGI)
            {
                if (gameMode == GameMode.TwoVS)
                {
                    active = true;
                }
                else
                {
                    if (inGameRoomdata.MaxBuyIn <= 0)
                    {
                        active = CPPlayer.UserInfo.userDatabase.User.Gold >= inGameRoomdata.MinBuyIn;
                    }
                    else
                    {
                        active=CPPlayer.UserInfo.userDatabase.User.Gold>=inGameRoomdata.MinBuyIn &&
                               CPPlayer.UserInfo.userDatabase.User.Gold<=inGameRoomdata.MaxBuyIn;    
                    }
                }
            }
            inactiveObj.gameObject.SetActive(!active);
            canEnterGame = active;
        }
    }
}
