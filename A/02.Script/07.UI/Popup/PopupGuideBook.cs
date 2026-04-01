using CAPYBARA.Bundles;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public enum GuideBookType
    {
        SevenPoker = 0,
        Badugi = 1,
        Holdem = 2,
        JackPot = 3,
        DealerFee = 4,
        ClassInfo = 5
    }

    public class PopupGuideBook : BasePopup
    {
        [SerializeField] private CPButton sevenPokerClose;
        [SerializeField] private CPButton badugiClose;
        [SerializeField] private CPButton holdemClose;
        [SerializeField] private CPButton jackpotClose;
        [SerializeField] private CPButton dealerFeeClose;
        [SerializeField] private CPButton classInfoClose;
        
        [SerializeField] GameObject sevenPokerBook;
        [SerializeField] GameObject badugiBook;
        [SerializeField] GameObject holdemBook;
        [SerializeField] GameObject jackPotBook;
        [SerializeField] GameObject dealerFeeBook;
        [SerializeField] GameObject classinfoBook;

        
            
        protected override void OnInit()
        {
            base.OnInit();
            sevenPokerClose.onClick.AddListener(Close);
            badugiClose.onClick.AddListener(Close);
            holdemClose.onClick.AddListener(Close);
            jackpotClose.onClick.AddListener(Close);
            dealerFeeClose.onClick.AddListener(Close);
            classInfoClose.onClick.AddListener(Close);
        }

        public void OpenBook(GuideBookType bookType)
        {
            // 모든 책을 비활성화
            sevenPokerBook.SetActive(false);
            badugiBook.SetActive(false);
            holdemBook.SetActive(false);
            jackPotBook.SetActive(false);
            dealerFeeBook.SetActive(false);
            classinfoBook.SetActive(false);

            // 선택된 타입에 해당하는 책만 활성화
            switch (bookType)
            {
                case GuideBookType.SevenPoker:
                    sevenPokerBook.SetActive(true);
                    break;
                case GuideBookType.Badugi:
                    badugiBook.SetActive(true);
                    break;
                case GuideBookType.Holdem:
                    holdemBook.SetActive(true);
                    break;
                case GuideBookType.JackPot:
                    jackPotBook.SetActive(true);
                    break;
                case GuideBookType.DealerFee:
                    dealerFeeBook.SetActive(true);
                    break;
                case GuideBookType.ClassInfo:
                    classinfoBook.SetActive(true);
                    break;
                default:
                    Debug.LogWarning($"Invalid book type: {bookType}");
                    // 기본적으로 첫 번째 책을 활성화
                    sevenPokerBook.SetActive(true);
                    break;
            }

            PopupManager.Instance.Open < PopupGuideBook > ();
        }



        protected override void OnOpen()
        {
            base.OnOpen();
        }

        protected override void OnClose()
        {
            base.OnClose();
        }
    }

}
