using System;
using CAPYBARA.Bundles;
using UnityEngine;
using UnityEngine.UI;

public class Emoji : MonoBehaviour
{
    public Image emojiImage;
    public CPButton emojiButton;
    public Animation emojiAnimation;
    
    public string emotionId;
    public Action<string> OnClick;
    
    private void Awake()
    {
        if (emojiButton != null)
            emojiButton.onClick.AddListener(HandleClick);
    }
    
    private void HandleClick()
    {
        OnClick?.Invoke(emotionId);
    }
    
    public void SetSprite(Sprite sprite)
    {
        if (emojiImage != null)
            emojiImage.sprite = sprite;
    }
}
