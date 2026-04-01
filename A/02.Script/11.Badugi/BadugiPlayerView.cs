using System;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class BadugiPlayerView : MonoBehaviour
    {
        [SerializeField]RectTransform userinfoWindow;
        [SerializeField]RectTransform roundInfoWindow;
        
        public Image playerImage;
        public Transform[] myCardPos;
        [HideInInspector]public Vector2[] cardPositions;
        public Transform[] winnerCardPos;
        public Transform throwChipStartPos;

        public TMPro.TMP_Text currentOwnedChip;
        public TMPro.TMP_Text playerNickName;

        [Header("##stamp##")]
        public GameObject betActionTypeImageParentObj;
        public Image betActionTypeImage;
        public Animator betActionTypeImageAnimator;
        public ParticleSystem betActionTypeImageParticle0;
        public ParticleSystem betActionTypeImageParticle1;
        
        [Header("##JokboBestCard##")]
        public CanvasGroup bestCardObj;
        public TMPro.TMP_Text bestCardText;
        public CanvasGroup bestCardObj_result;
        public TMPro.TMP_Text bestCardText_result;

        [Header("##TurnEffect&Time##")]
        public GameObject turnActiveObj;
        public Image turnActiveImage;
        public GameObject timeCountObj; 
        public Image timeCountImage;

        [Header("roundImage")]
        public BadugiRoundInfoView[] roundInfo;
        
        [Header("win&lose")]
        public GameObject winFontImageObj;
        public GameObject winChipObject;
        public TMP_Text winChipAmount;
        public GameObject allinObject;
        public Animator allinAnimator;
        
        public TMP_Text winJokboName;
        
        public GameObject loseObject;
        public TMP_Text loseChipAmount;
        
        public GameObject dealerBtnObj;
        badugi.Player playerInfo;
        
        public CPButton seePlayerInfoBtn;
        public OtherPlayerInfoModal mePlayerInfoModal;
        public OtherPlayerInfoModal otherPlayerInfoModal;
        
        public GameObject roundBetChipObj;
        public TMP_Text roundBetChiptext;
        
        public CPButton cardOpenBtn;
        public CPButton cardCloseBtn;
        
        public CPButton cardOpenBtnAtforfeitWin;

        public CanvasGroup kickvoteCanvasGroup;
        public TMP_Text kickvoteText;
        public GameObject reservedOut;
        
        public GameObject inActiveMask;
        public Image inactivemaskImage;
        public GameObject readyCompleteObj;
        private string NickNameOffColor = "#485e7d";
        private string NickNameOnColor = "#6f93c4";

        private string haveMoneyOffColor = "#516c92";
        private string haveMoneyOnColor = "#6f93c4";
        
        public Image emotionImage;
        public GameObject emotionObj;

        public GameObject drawActionParentObj;
        public GameObject[] drawActionImgs;
        private void Awake()
        {
            cardPositions = new Vector2[myCardPos.Length];
            for (int i = 0; i < myCardPos.Length; i++)
            {
                cardPositions[i] = myCardPos[i].GetComponent<RectTransform>().anchoredPosition;
            }
            drawActionParentObj.gameObject.SetActive(false);
        }

        public void Init(badugi.Player _playerInfo)
        {
            this.gameObject.SetActive(true);
            playerInfo = _playerInfo;
            InitializeUI();
        }
        public void InitializeUI()
        {
            betActionTypeImageParentObj.gameObject.SetActive(false);
            bestCardObj.gameObject.SetActive(false);
            timeCountObj.SetActive(false);
            turnActiveObj.SetActive(false);
            winChipObject.SetActive(false);
            loseObject.SetActive(false);
            currentOwnedChip.gameObject.SetActive(true);
            roundBetChipObj.SetActive(false);
            
            kickvoteCanvasGroup.gameObject.SetActive(false);
            reservedOut.SetActive(false);
            
            otherPlayerInfoModal.Init();
            mePlayerInfoModal.Init();

            ViewSetting();
        }

        public void ViewSetting()
        {
            currentOwnedChip.text = Extension.ToKoreanFormat(playerInfo.Chip, Extension.KoreanFormatMode.Planning);
            playerNickName.text = playerInfo.Nick;
        }

        public void SetActivateView(bool isMe,bool turnActive)
        {
            turnActiveObj.SetActive(turnActive);

            if (turnActive)
            {
                turnActiveImage.transform.rotation=Quaternion.identity;
            }
        }
    }

}
