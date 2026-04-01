using System;
using System.Collections.Generic;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class HoldemPlayerView : MonoBehaviour
    {
        [SerializeField]Vector2 userInfoLocalPos;
        [SerializeField]Vector2 otherCardParentLocalPos;
        [SerializeField]Vector2 stampLocalPos;
        [SerializeField]Vector2 deealerfeeLocalPos;
        [SerializeField]Vector2 otherModalLocalPos;

        public RectTransform userinfoObj;
        public RectTransform othercardParentObj;
        
        public Image playerImage;
        public Transform[] myCardPos;
        public Transform[] otherCardPos;
        public Transform[] winnerCardPos;
        public Transform throwChipStartPos;

        public TMPro.TMP_Text currentOwnedChip;
        public TMPro.TMP_Text currentOwnedChipInactive;
        public TMPro.TMP_Text playerNickName;
        public TMPro.TMP_Text playerNickNameInactive;

        [Header("##stamp##")] public GameObject stampParentObj;
        public Image betActionTypeImage;
        public Animator betActionTypeImageAnimator;
        public ParticleSystem betActionTypeImageParticle0;
        public ParticleSystem betActionTypeImageParticle1;

        [Header("##JokboBestCard##")] 
        public CanvasGroup bestCardObjInRound_me;
        public TMPro.TMP_Text bestCardTextinRound_me;
        
        public CanvasGroup bestCardObjInResult;
        public TMPro.TMP_Text bestCardTextinRound;
        
        public CanvasGroup bestCardObjInResult_Loser;
        public TMPro.TMP_Text bestCardTextinResult;

        [Header("##TurnEffect&Time##")] 
        public GameObject turnActiveObj;
        public Image turnActiveImage;
        public GameObject timeCountObj; 
        public Image timeCountImage;

        public GameObject winFontImageObj;
        public Animator winFontImageAnimator;
        public TMP_Text winChipAmount;
        public GameObject allinObject;
        public Animator allinAnimator;

        public GameObject loseObject;
        public TMP_Text loseChipAmount;

        public GameObject dealerBtnObj;

        holdem.Player playerInfo;
        
        public CPButton seePlayerInfoBtn;
        public OtherPlayerInfoModal mePlayerInfoModal;
        public OtherPlayerInfoModal otherPlayerInfoModal;

        public GameObject roundBetChipObj;
        public TMP_Text roundBetChiptext;

        public CPButton cardOpenBtn;
        public CPButton cardCloseBtn;
        
        public CPButton cardOpenBtnAtforfeitWin;
        public CPButton cardCloseBtnAtforfeitWin;

        public CanvasGroup kickvoteCanvasGroup;
        public TMP_Text kickvoteText;
        public GameObject reservedOut;

        public GameObject inActiveMask;
        public Image inactivemaskImage;
        private string NickNameOffColor = "#485e7d";
        private string NickNameOnColor = "#6f93c4";

        private string haveMoneyOffColor = "#516c92";
        private string haveMoneyOnColor = "#6f93c4";

        public Image emotionImage;
        public GameObject emotionObj;

        public GameObject starRankObjNew;
        public Animator starRankObjNewAnimator;

        public GameObject blindParent;
        public Animator blindAnim;
        public Image smallBlind;
        public Image bigBlind;

        private void Awake()
        {
            userinfoObj.anchoredPosition = userInfoLocalPos;
            othercardParentObj.anchoredPosition = otherCardParentLocalPos;
            
            starRankObjNewAnimator.keepAnimatorStateOnDisable = true;

           // betActionTypeImage.GetComponent<RectTransform>().anchoredPosition = stampLocalPos;
        }

        public void Init()
        {
            InitializeUI();
        }

        public void InitializeUI()
        {
            stampParentObj.gameObject.SetActive(false);
            bestCardObjInRound_me.gameObject.SetActive(false);
            bestCardObjInResult.gameObject.SetActive(false);
            bestCardObjInResult_Loser.gameObject.SetActive(false);
            timeCountObj.SetActive(false);
            turnActiveObj.SetActive(false);
            allinObject.SetActive(false);
            winFontImageObj.SetActive(false);
            loseObject.SetActive(false);
            roundBetChipObj.SetActive(false);
            kickvoteCanvasGroup.gameObject.SetActive(false);
            reservedOut.SetActive(false);
            starRankObjNew.SetActive(false);
            blindParent.gameObject.SetActive(false);

            
            otherPlayerInfoModal.Init();
            mePlayerInfoModal.Init();
        }

        public void ViewSetting(holdem.Player _playerInfo)
        {   
            playerInfo = _playerInfo;
            this.gameObject.SetActive(true);
            currentOwnedChip.text = Extension.ToKoreanFormat(playerInfo.Chip, Extension.KoreanFormatMode.Planning);
            currentOwnedChipInactive.text = Extension.ToKoreanFormat(playerInfo.Chip, Extension.KoreanFormatMode.Planning);
            playerNickName.text = playerInfo.Nick;
            playerNickNameInactive.text = playerInfo.Nick;
        }

        public void SetActivateView(bool isMe, bool turnActive)
        {
            turnActiveObj.SetActive(turnActive);

            if (turnActive)
            {
                turnActiveImage.transform.rotation=Quaternion.identity;
            }
        }
    }
}