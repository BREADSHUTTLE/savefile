using System;
using System.Collections.Generic;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    //카드 선택 윈도우 안에 있는 카드
    public class SPokerCardViewForSelect : MonoBehaviour
    {
        public GameObject cardObj;
        public CPButton selectBtn;
        public Image cardImage;
        public Image cardSelectedImage;
        public CPButton unSelectBtn;
        public Image unSelectBtnImage;

        public Transform selectedPos;
        public Transform unselectedPos;

        public bool isSelected;
        
        Action<SPokerCardViewForSelect,bool> isOnStartCallback;
        Action<SPokerCardViewForSelect,bool> isOnEndCallback;

        private bool cantTouch=false;
        private Sequence currentSeq;
        public void Init()
        {
            selectBtn.onClick.AddListener(TouchCard);
            unSelectBtn.onClick.AddListener(TouchCard);
        }

        const float outlineDisplayTime=0.4f;
        private void TouchCard()
        {
            if (cantTouch)
                return;
            
            isSelected = !isSelected;
            isOnStartCallback?.Invoke(this,isSelected);

            currentSeq?.Kill();
            currentSeq = DOTween.Sequence();
            Sequence seq = currentSeq;
            if (isSelected)
            {
                cardSelectedImage.gameObject.SetActive(true);
                seq.Append(cardObj.transform.DOMove(selectedPos.transform.position, outlineDisplayTime));
                seq.Join(cardSelectedImage.DOFade(1.0f, outlineDisplayTime));
                seq.SetEase(Ease.OutQuad);
            }
            else
            {
                seq.Append(cardObj.transform.DOMove(unselectedPos.transform.position, outlineDisplayTime));
                seq.Join(cardSelectedImage.DOFade(0.0f, outlineDisplayTime));
                seq.SetEase(Ease.OutQuad);
            }
            
            seq.OnComplete(() =>
            {
                if (!isSelected)
                {
                    cardSelectedImage.gameObject.SetActive(true);
                }
                isOnEndCallback?.Invoke(this,isSelected);
            });
        
        }

        public void UnselectImageActive(bool active)
        {
            Sequence seq = DOTween.Sequence();
            
            unSelectBtn.gameObject.SetActive(true);
            if (active)
            {
                seq.Append(unSelectBtnImage.DOFade(1.0f, outlineDisplayTime));    
            }
            else
            {
                seq.Append(unSelectBtnImage.DOFade(0.0f, outlineDisplayTime));
            }
            seq.SetEase(Ease.OutQuad);
            seq.OnComplete(() =>
            {
                if (!isSelected)
                {
                    unSelectBtn.gameObject.SetActive(false);
                }
            });
        }

        public void LockTouch()
        {
            cantTouch = true;
            selectBtn.interactable = false;
            unSelectBtn.interactable = false;
        }
        
        

        public void InitCardImage(Sprite _cardImage)
        {
            isSelected = false;
            cantTouch=false;
            cardImage.sprite = _cardImage;
            unSelectBtn.gameObject.SetActive(false);
            cardSelectedImage.gameObject.SetActive(false);
            cardObj.transform.position=unselectedPos.position;

            selectBtn.interactable = true;
            unSelectBtn.interactable = true;
        }
        
        public void AddListenerToSelectCardStart(Action<SPokerCardViewForSelect,bool> _isOnCallback)
        {
            isOnStartCallback+=_isOnCallback;
        }
        
        public void AddListenerToSelectCardEnd(Action<SPokerCardViewForSelect,bool> _isOnCallback)
        {
            isOnEndCallback+=_isOnCallback;
        }
        
        public void RemoveListenerToSelectCard()
        {
            isOnStartCallback = null;
            isOnEndCallback= null;
        }

        public void BtnDisable()
        {
            selectBtn.interactable = false;
            unSelectBtn.interactable = false;
        }
    }
}
