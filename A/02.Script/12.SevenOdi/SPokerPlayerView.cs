using System;
using System.Collections;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class SPokerPlayerView : MonoBehaviour
    {
        
        [SerializeField]RectTransform userinfoWindowObj;
        
        public Image playerImage; 
        public Transform[] myCardPos;
        [HideInInspector]public Vector2[] cardPositions;
        public Transform throwChipStartPos;
        
        public TMPro.TMP_Text currentOwnedChip;
        public TMPro.TMP_Text playerNickName;
       
        [Header("##JokboBestCard##")]
        public GameObject betActionTypeImageParentObj;
        public Image betActionTypeImage;
        public Animator betActionTypeImageAnimator;
        public ParticleSystem betActionTypeImageParticle0;
        public ParticleSystem betActionTypeImageParticle1;
        
        [Header("##JokboBestCard##")]
        public CanvasGroup bestCardObj;
        public TMPro.TMP_Text bestCardText;

        [Header("##TurnEffect&Time##")]
        public GameObject turnActiveObj;
        public Image turnActiveImage;
        public GameObject timeCountObj; 
        public Image timeCountImage;

        [Header("win&lose")]
        public GameObject winFontImageObj;
        public GameObject winChipObject;
        public TMP_Text dealerFee;
        public TMP_Text winChipAmount;
        public GameObject allinObject;
        public Animator allinAnimator;
        
        public TMP_Text winJokboName;
        
        public GameObject loseObject;
        public TMP_Text loseChipAmount;
        
        public GameObject dealerBtnObj;
        sevenPoker.Player playerInfo;
        
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

        private WaitForSeconds waitInterval;
        private void Awake()
        {
            cardPositions = new Vector2[myCardPos.Length];
            for (int i = 0; i < myCardPos.Length; i++)
            {
                cardPositions[i] = myCardPos[i].GetComponent<RectTransform>().anchoredPosition;
            }
            waitInterval= new WaitForSeconds(interval);
        }

        public void Init(sevenPoker.Player _playerInfo)
        {
            this.gameObject.SetActive(true);
            playerInfo = _playerInfo;
            InitializeUI();
            dotLoadingObj.gameObject.SetActive(false);
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
        
        [SerializeField] public CanvasGroup dotAnimationCG;
        [SerializeField] public GameObject dotLoadingObj;
        [SerializeField] private GameObject dot1;
        [SerializeField] private GameObject dot2;
        [SerializeField] private GameObject dot3;

        [SerializeField] private float interval = 1f;

        private Coroutine loopCo;
        
        public void StartLoop()
        {
            if (this.gameObject.activeInHierarchy == false)
                return;
            
            StopLoop(); // 중복 방지
            dotLoadingObj.gameObject.SetActive(true);
            dotAnimationCG.alpha = 1f;
            loopCo = StartCoroutine(DotLoop());
        }

        public void StopLoop()
        {
            if (loopCo != null)
            {
                StopCoroutine(loopCo);
                loopCo = null;
               // dotLoadingObj.gameObject.SetActive(false);
               dotAnimationCG.DOFade(0f, 0.3f).OnComplete(() =>
               {
                   dotLoadingObj.gameObject.SetActive(false);    
               });
            }

            
            
        }
        
        private IEnumerator DotLoop()
        {
            
            // 초기 상태
            dot1.SetActive(true);
            dot2.SetActive(false);
            dot3.SetActive(false);

            while (true)
            {
                // 1초 후 2 ON
                yield return waitInterval;
                dot2.SetActive(true);

                // 1초 후 3 ON
                yield return waitInterval;
                dot3.SetActive(true);

                // 1초 후 2,3 OFF
                yield return waitInterval;
                dot2.SetActive(false);
                dot3.SetActive(false);
            }
        }
    }
}
