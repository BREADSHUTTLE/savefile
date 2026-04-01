using System;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class CardViewer : Poolable
    {
        public Image cardImage;
        public Image mask;
        public Image cardOpenOutline;
        public Image cardHighLight;
        public Image cardHighLightForBadugi;

        public Image cardInnerGradientImg;

        public CPButton cardBtn;

        public int cardInfoIndex=-1;
        protected bool isCardTouched;

        public RectTransform cardAllObj;

        public RectTransform unTouchedPos;
        public RectTransform touchedPos;

        //[HideInInspector]public Material commonCardMaterial;
        private Animator cardAnimator;
        [SerializeField]private ShaderControl cardShaderControl; 
        
        public static Action<int, bool> CardTouchCallbackforCardOpen;
        
        protected virtual void Awake()
        {
            cardBtn?.onClick.AddListener(TouchCard);
            isCardTouched = false;
            
            cardAnimator=cardImage.GetComponent<Animator>();
            cardAnimator.Play("idle");
            //cardShaderControl=cardImage.GetComponent<ShaderControl>();
          
            
        }

        private void OnEnable()
        {
            cardAnimator.Play("idle");
            //cardShaderControl.currentMaterial?.SetFloat("_GlossEnabled", 0f);
        }

        public void InitCardSet()
        {
            cardAnimator.Play("idle");
            cardShaderControl.currentMaterial?.SetFloat("_GlossEnabled", 0f);
            cardOpenOutline.gameObject.SetActive(false);
            cardHighLight.gameObject.SetActive(false);
            cardInnerGradientImg.gameObject.SetActive(false);
            cardHighLightForBadugi.gameObject.SetActive(false);
            mask.gameObject.SetActive(false);
            isCardTouched = false;
            //cardInfoIndex = -1;
            //cardAllObj.transform.position = unTouchedPos.transform.position;
            //cardImage.sprite = InGameResourcesBundle.Loaded.noneImageForCard;
        }

        public virtual void Inactive()
        {
            listener = null;
            listenerBadugi = null;
            cardAnimator.Play("idle");
            cardShaderControl.currentMaterial?.SetFloat("_GlossEnabled", 0f);
            cardOpenOutline.gameObject.SetActive(false);
            cardHighLight.gameObject.SetActive(false);
            cardInnerGradientImg.gameObject.SetActive(false);
            cardHighLightForBadugi.gameObject.SetActive(false);
            mask.gameObject.SetActive(false);
            isCardTouched = false;
            cardInfoIndex = -1;
            cardAllObj.transform.position = unTouchedPos.transform.position;
            cardImage.sprite = InGameResourcesBundle.Loaded.noneImageForCard;
        }

        public void CardHighlightActive(bool active)
        {
            cardHighLight.gameObject.SetActive(active);
            cardOpenOutline.gameObject.SetActive(active);
            cardInnerGradientImg.gameObject.SetActive(active);
            if(active==false)
            {
                cardAnimator.Play("idle");
                cardShaderControl.currentMaterial?.SetFloat("_GlossEnabled", 0f);
            }
        }

        private Sequence seq;
        
        public void HighLightCardCallback(int cardRankIndex, bool activeForChange,Action callback = null,bool isMyCard=false)
        {
            if (cardRankIndex == cardInfoIndex)
            {
                float highlightUpTime = 0.3f;
                float highlightDownTime = 0.3f;
                if (CPPlayer.Server.visualEffectTimeConfig.ContainsKey("SELECT_UP_MS"))
                {
                    highlightUpTime= (float)CPPlayer.Server.visualEffectTimeConfig["SELECT_UP_MS"]/1000f; 
                }
                if (CPPlayer.Server.visualEffectTimeConfig.ContainsKey("SELECT_DOWN_MS"))
                {
                    highlightDownTime= (float)CPPlayer.Server.visualEffectTimeConfig["SELECT_DOWN_MS"]/1000f; 
                }
                
                isCardTouched = activeForChange;
                //이미 하이라이트 활성화 되있으면 안해도 됨
                if (isCardTouched && cardHighLight.color.a >= 0.8f && cardHighLight.gameObject.activeInHierarchy)
                    return;
                Extension.eLog($"{cardRankIndex}번 카드 활성화 : {isCardTouched}");
                seq?.Kill();
                if (isCardTouched)
                {
                    var c = cardHighLight.color;
                    c.a = 0;
                    cardHighLight.color = c;
                    cardHighLight.gameObject.SetActive(true);

                    ColorUtility.TryParseHtmlString("#E9D477", out Color innerColor);
                    innerColor.a = 0;
                    cardInnerGradientImg.color = innerColor;
                    cardInnerGradientImg.gameObject.SetActive(true);

                    seq = DOTween.Sequence();
                    seq.Append(cardHighLight.DOFade(1f, (float)highlightUpTime));
                    seq.Append(cardInnerGradientImg.DOFade(1f, (float)highlightUpTime));
                    //seq.Join(cardAllObj.DOAnchorPos(touchedPos.anchoredPosition, (float)highlightUpTime));
                    seq.SetEase(Ease.OutQuad);
                    seq.OnComplete(() =>
                    {
                        callback?.Invoke();
                        cardAnimator.Play("Card_PunchUp");
                        cardShaderControl.currentMaterial?.SetFloat("_GlossEnabled", 1f);
                    });
                    
                }
                else
                {
                    seq = DOTween.Sequence();
                    seq.Append(cardHighLight.DOFade(0f, (float)highlightDownTime));
                    seq.Append(cardInnerGradientImg.DOFade(0f, (float)highlightUpTime));
                    seq.Join(cardAllObj.DOAnchorPos(unTouchedPos.anchoredPosition, (float)highlightDownTime));
                    seq.SetEase(Ease.OutQuad);

                    seq.OnComplete(() =>
                    {
                        cardHighLight.gameObject.SetActive(false);
                        cardInnerGradientImg.gameObject.SetActive(false);
                        callback?.Invoke();
                    });
                    cardAnimator.Play("idle");
                    cardShaderControl.currentMaterial?.SetFloat("_GlossEnabled", 0f);
                }
            }
        }
        
          public void HighLightCardCallbackAtBadugi(int cardRankIndex, bool activeForChange,Action callback = null)
        {
            if (cardRankIndex == cardInfoIndex)
            {
                float highlightUpTime = 0.3f;
                float highlightDownTime = 0.3f;
                if (CPPlayer.Server.visualEffectTimeConfig.ContainsKey("SELECT_UP_MS"))
                {
                    highlightUpTime= (float)CPPlayer.Server.visualEffectTimeConfig["SELECT_UP_MS"]/1000f; 
                }
                if (CPPlayer.Server.visualEffectTimeConfig.ContainsKey("SELECT_DOWN_MS"))
                {
                    highlightDownTime= (float)CPPlayer.Server.visualEffectTimeConfig["SELECT_DOWN_MS"]/1000f; 
                }
                
                isCardTouched = activeForChange;
                //이미 하이라이트 활성화 되있으면 안해도 됨
                if (isCardTouched && cardHighLightForBadugi.color.a >= 0.8f && cardHighLightForBadugi.gameObject.activeInHierarchy)
                    return;
                Extension.eLog($"{cardRankIndex}번 카드 활성화 : {isCardTouched}");
                seq?.Kill();
                if (isCardTouched)
                {
                    var c = cardHighLightForBadugi.color;
                    c.a = 0;
                    cardHighLightForBadugi.color = c;
                    cardHighLightForBadugi.gameObject.SetActive(true);
                    
                    ColorUtility.TryParseHtmlString("#A094A4", out Color innerColor);
                    innerColor.a = 0;
                    cardInnerGradientImg.color = innerColor;
                    cardInnerGradientImg.gameObject.SetActive(true);

                    seq = DOTween.Sequence();
                    seq.Append(cardHighLightForBadugi.DOFade(1f, (float)highlightUpTime));
                    seq.Append(cardInnerGradientImg.DOFade(1f, (float)highlightUpTime));
                    seq.Join(cardAllObj.DOAnchorPos(touchedPos.anchoredPosition, (float)highlightUpTime));
                    seq.SetEase(Ease.OutQuad);
                    seq.OnComplete(() =>
                    {
                        callback?.Invoke();
                    });
                }
                else
                {
                    seq = DOTween.Sequence();
                    seq.Append(cardHighLightForBadugi.DOFade(0f, (float)highlightDownTime));
                    seq.Append(cardInnerGradientImg.DOFade(0f, (float)highlightUpTime));
                    seq.Join(cardAllObj.DOAnchorPos(unTouchedPos.anchoredPosition, (float)highlightDownTime));
                    seq.SetEase(Ease.OutQuad);

                    seq.OnComplete(() =>
                    {
                        cardHighLightForBadugi.gameObject.SetActive(false);
                        cardInnerGradientImg.gameObject.SetActive(false);
                        callback?.Invoke();
                    });
                }
            }
        }
        
        public void HighLightCardCallbackAtTouch(int cardRankIndex, bool activeForChange,Action callback = null)
        {
            if (cardRankIndex == cardInfoIndex)
            {
                float highlightUpTime = 0.3f;
                float highlightDownTime = 0.3f;
                if (CPPlayer.Server.visualEffectTimeConfig.ContainsKey("SELECT_UP_MS"))
                {
                    highlightUpTime= (float)CPPlayer.Server.visualEffectTimeConfig["SELECT_UP_MS"]/1000f; 
                }
                if (CPPlayer.Server.visualEffectTimeConfig.ContainsKey("SELECT_DOWN_MS"))
                {
                    highlightDownTime= (float)CPPlayer.Server.visualEffectTimeConfig["SELECT_DOWN_MS"]/1000f; 
                }
                
                isCardTouched = activeForChange;
                //이미 하이라이트 활성화 되있으면 안해도 됨
                if (isCardTouched && cardOpenOutline.color.a >= 0.8f && cardOpenOutline.gameObject.activeInHierarchy)
                    return;
                Extension.eLog($"{cardRankIndex}번 카드 활성화 : {isCardTouched}");
                seq?.Kill();
                if (isCardTouched)
                {
                    var c = cardOpenOutline.color;
                    c.a = 0;
                    cardOpenOutline.color = c;
                    cardOpenOutline.gameObject.SetActive(true);
                    
                    ColorUtility.TryParseHtmlString("#5CBFFF", out Color innerColor);
                    innerColor.a = 0;
                    cardInnerGradientImg.color = innerColor;
                    cardInnerGradientImg.gameObject.SetActive(true);

                    seq = DOTween.Sequence();
                    seq.Append(cardOpenOutline.DOFade(1f, (float)highlightUpTime));
                    seq.Append(cardInnerGradientImg.DOFade(1f, (float)highlightUpTime));
                    seq.Join(cardAllObj.DOAnchorPos(touchedPos.anchoredPosition, (float)highlightUpTime));
                    seq.SetEase(Ease.OutQuad);
                    seq.OnComplete(() =>
                    {
                        callback?.Invoke();
                    });
                }
                else
                {
                    seq = DOTween.Sequence();
                    seq.Append(cardOpenOutline.DOFade(0f, (float)highlightDownTime));
                    seq.Append(cardInnerGradientImg.DOFade(0f, (float)highlightUpTime));
                    seq.Join(cardAllObj.DOAnchorPos(unTouchedPos.anchoredPosition, (float)highlightDownTime));
                    seq.SetEase(Ease.OutQuad);

                    seq.OnComplete(() =>
                    {
                        cardOpenOutline.gameObject.SetActive(false);
                        cardInnerGradientImg.gameObject.SetActive(false);
                        callback?.Invoke();
                    });
                }
            }
        }
        
    
        public virtual void InactiveSelectEffectAtFold(float _highlightDownTime)
        {
            float highlightDownTime =_highlightDownTime;
           
            
            if (isCardTouched)
            {
                seq = DOTween.Sequence();
                seq.Append(cardHighLightForBadugi.DOFade(0f, (float)highlightDownTime));
                seq.Append(cardOpenOutline.DOFade(0f, highlightDownTime));
                seq.Append(cardHighLight.DOFade(0f, highlightDownTime));
                seq.Append(cardInnerGradientImg.DOFade(0f, (float)highlightDownTime));
                seq.Join(cardAllObj.DOAnchorPos(unTouchedPos.anchoredPosition, highlightDownTime));
                seq.SetEase(Ease.OutQuad);

                seq.OnComplete(() =>
                {
                    CardHighlightActive(false);
                    //HighLightActive(false);
                });
                cardAnimator.Play("idle");
                cardShaderControl.currentMaterial?.SetFloat("_GlossEnabled", 0f);
            }
            mask.gameObject.SetActive(true);
            isCardTouched = false;
        }

        public void SetMaskFade()
        {
            float dieDim = (float)CPPlayer.Server.visualEffectTimeConfig["DIE_ME_DIM_MS"]/1000f;
            
            mask.gameObject.SetActive(true);
            var c = mask.color;
            c.a = 0f;
            mask.color = c;

            mask.DOFade(0.5f, dieDim);
        }
    
        protected void TouchCard()
        {
            listenerBadugi?.HighlightCardforChange(cardInfoIndex,!isCardTouched);
            listener?.CardTouchCallback(cardInfoIndex,!isCardTouched);
        }
        ICardTouchCallbackListen listener;
        ICardBadugiCardRecommend listenerBadugi;
        public void AddListenerForCardTouch(ICardTouchCallbackListen _listener)
        {
            listener = _listener;
        }
        
        public void AddListenerForCardRecommendInBadugi(ICardBadugiCardRecommend _listener)
        {
            listenerBadugi = _listener;
        }

        private void OnDestroy()
        {
            listener = null;
            listenerBadugi = null;
        }
    }
}
