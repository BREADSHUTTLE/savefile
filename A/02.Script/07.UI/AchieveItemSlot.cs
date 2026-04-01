using System.Collections.Generic;
using System.Linq;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.Definition;
using CAPYBARA.lobby;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class AchieveItemSlot : MonoBehaviour
    {
        [HideInInspector]public RewardMissionInfo myRewardmissionInfo;
        [HideInInspector] public ConfigPoints myConfigPointsInfo;

        [SerializeField] private Image imgGetReward;
        [SerializeField] private Image imgComplete;
        
        public Image mainImages;
        
        public TMP_Text titleNames;

        public GameObject openArrowObject;
        public CPButton openDetailBtn;
        public CPButton closeDetailBtn;
        public Image imgCloseArrow;
        
        public Animator openObjAnimator;

        public Transform achieveslotParent;

        [HideInInspector]public List<AchieveSlot> achieveSlotList=new List<AchieveSlot>();
        ScrollRect viewscrollRect;
        private bool isOpen = false;
        private bool isAnimating = false;
        
        private void OnEnable()
        {
            CancelInvoke(nameof(OnAnimationComplete));
            isAnimating = false;

            if (openObjAnimator != null)
                openObjAnimator.keepAnimatorStateOnDisable = true;
            if (closeDetailBtn != null)
                closeDetailBtn.gameObject.SetActive(isOpen);
            if (imgCloseArrow != null)
                imgCloseArrow.gameObject.SetActive(isOpen);
        }

        public void ClearSlots()
        {
            foreach (var slot in achieveSlotList)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }
            achieveSlotList.Clear();
            
            // 리스너 정리
            openDetailBtn.onClick.RemoveAllListeners();
            closeDetailBtn.onClick.RemoveAllListeners();
            
            // 상태 초기화
            isOpen = false;
            isAnimating = false;
        }
        
        public void Init(RewardMissionInfo rewardmissionInfo, ScrollRect _viewscrollRect, string categoryKey)
        {
            myRewardmissionInfo = rewardmissionInfo;
            viewscrollRect = _viewscrollRect;

            string title = GetCategoryTitle(categoryKey);
            titleNames.text = title;

            openDetailBtn.onClick.AddListener(ToggleDetail);
            closeDetailBtn.onClick.AddListener(ToggleDetail);
            isOpen = false;
            isAnimating = false;
            closeDetailBtn.gameObject.SetActive(false);
            imgCloseArrow.gameObject.SetActive(false);
        }

        private string GetCategoryTitle(string categoryKey)
        {
            var categoryInfo = StaticData.Wrapper.rewardMissionInfo.FirstOrDefault(x => x.rewardId.ToString() == categoryKey);
            
            return categoryInfo?.message_Kr ?? categoryKey;
        }

        public void Init(ConfigPoints configPointsInfo, ScrollRect _viewscrollRect)
        {
            myConfigPointsInfo = configPointsInfo;

            string title = GetCategoryTitle("PLAY_POINT");
            titleNames.text = title;

            openDetailBtn.onClick.AddListener(ToggleDetail);
            closeDetailBtn.onClick.AddListener(ToggleDetail);
            isOpen = false;
            isAnimating = false;
            closeDetailBtn.gameObject.SetActive(false);
            imgCloseArrow.gameObject.SetActive(false);
            //mainImages.sprite = LobbyResourcesBundle.Loaded.achieveMainImage[0];

            viewscrollRect = _viewscrollRect;
        }
        
        private void ToggleDetail()
        {
            if (isAnimating)
                return;
            
            isOpen = !isOpen;
            isAnimating = true;
            
            openObjAnimator.SetBool("On", isOpen);
            closeDetailBtn.gameObject.SetActive(isOpen);
            imgCloseArrow.gameObject.SetActive(isOpen);
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewscrollRect.content);
            
            // 애니메이션 완료 후 플래그 해제 (0.3초 후)
            Invoke(nameof(OnAnimationComplete), 0.3f);
        }
        
        private void OnAnimationComplete()
        {
            isAnimating = false;
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewscrollRect.content);
        }

        public void SortSlotsByMaxCount()
        {
            achieveSlotList = achieveSlotList.OrderBy(slot => slot.isAlreadyClaimed).ThenBy(slot => slot.sortValue).ToList();
            for (int i = 0; i < achieveSlotList.Count; i++)
            {
                achieveSlotList[i].transform.SetSiblingIndex(i);
                achieveSlotList[i].SetReward(i);
            }
        }

        public void RefreshStatusImages()
        {
            if (achieveSlotList == null || achieveSlotList.Count == 0)
            {
                if (imgGetReward != null)
                    imgGetReward.gameObject.SetActive(false);
                if (imgComplete != null)
                    imgComplete.gameObject.SetActive(false);
                return;
            }
            
            bool hasAnyClaimable = achieveSlotList.Any(slot => slot.canClaimAny);
            bool allClaimed = achieveSlotList.All(slot => slot.isAlreadyClaimed);
            
            if (imgGetReward != null)
                imgGetReward.gameObject.SetActive(hasAnyClaimable);
            if (imgComplete != null)
                imgComplete.gameObject.SetActive(allClaimed);

            if (allClaimed && isOpen)
            {
                isOpen = false;
                openObjAnimator.SetBool("On", false);
                closeDetailBtn.gameObject.SetActive(false);
                imgCloseArrow.gameObject.SetActive(false);
                LayoutRebuilder.ForceRebuildLayoutImmediate(viewscrollRect.content);
            }    
        }
    }
}
