using CAPYBARA.Bundles;
using UnityEngine;
using UnityEngine.UI;


namespace CAPYBARA
{
    public class PageIndicator : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private PageScrollSnap snap;
        [SerializeField] private Transform dotContainer;
        [SerializeField] private Button dotPrefab;
        [SerializeField] private Sprite dotOn;
        [SerializeField] private Sprite dotOff;

        private Button[] dots;
        
        [Header("Prev Button")]
        [SerializeField] private CPButton prevButton;
        [SerializeField] private Image prevImage;
        [SerializeField] private Sprite prevOnSprite;
        [SerializeField] private Sprite prevOffSprite;

        [Header("Next Button")]
        [SerializeField] private CPButton nextButton;
        [SerializeField] private Image nextImage;
        [SerializeField] private Sprite nextOnSprite;
        [SerializeField] private Sprite nextOffSprite;
        void Start()
        {
            // snap이 자동 pageCount 쓰면 여기서 맞춰도 됨
            // snap.SetPageCountAuto();

            BuildDots(snap.PageCount);

        
            prevButton.onClick.AddListener(PrevPage);
            nextButton.onClick.AddListener(NextPage);

            // 초기 UI 반영
            RefreshUI(snap.CurrentPage);

            // 페이지 변경 이벤트 연결
            snap.OnPageChanged += RefreshUI;
        }

        private void PrevPage()
        {
            snap.SnapToPage(snap.CurrentPage - 1);
        }

        private void NextPage()
        {
            snap.SnapToPage(snap.CurrentPage + 1);
        }
        
        void OnDestroy()
        {
            if (snap != null) snap.OnPageChanged -= RefreshUI;
        }

        private void BuildDots(int count)
        {
            for (int i = dotContainer.childCount - 1; i >= 0; i--)
                Destroy(dotContainer.GetChild(i).gameObject);

            dots = new Button[count];

            for (int i = 0; i < count; i++)
            {
                int idx = i;
                var dotBtn = Instantiate(dotPrefab, dotContainer);
                dots[i] = dotBtn;

                dotBtn.onClick.AddListener(() => snap.SnapToPage(idx));

                var img = dotBtn.GetComponent<Image>();
                if (img != null) img.sprite = dotOff;
            }
        }

        private void RefreshUI(int pageIndex)
        {
            // --- Dot ---
            if (dots != null)
            {
                for (int i = 0; i < dots.Length; i++)
                {
                    var img = dots[i].GetComponent<Image>();
                    if (img == null) continue;

                    img.sprite = (i == pageIndex) ? dotOn : dotOff;
                }
            }

            // --- Prev Button ---
            bool canPrev = pageIndex > 0;
            prevButton.enabled = canPrev;
            if (prevImage != null)
                prevImage.sprite = canPrev ? prevOnSprite : prevOffSprite;

            // --- Next Button ---
            bool canNext = pageIndex < snap.PageCount - 1;
            nextButton.enabled = canNext;
            if (nextImage != null)
                nextImage.sprite = canNext ? nextOnSprite : nextOffSprite;
        }
    }

}

