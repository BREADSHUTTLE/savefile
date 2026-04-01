using System;
using CAPYBARA.Bundles;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class CustomerServiceAttachmentSlot : MonoBehaviour
    {
        [SerializeField] private RawImage thumbnailImage;
        [SerializeField] private CPButton deleteButton;
        [SerializeField] private CPButton slotButton;
        [SerializeField] private GameObject videoIcon;

        private Texture2D loadedTexture;

        public string FilePath { get; private set; }
        public bool IsVideo { get; private set; }

        public void Setup(string filePath, Texture2D thumbnail, bool isVideo, Action onDelete, Action onTap = null)
        {
            FilePath = filePath;
            IsVideo = isVideo;

            if (loadedTexture != null && loadedTexture != thumbnail)
            {
                UnityEngine.Object.Destroy(loadedTexture);
                loadedTexture = null;
            }

            loadedTexture = thumbnail;

            if (thumbnailImage != null)
            {
                thumbnailImage.texture = thumbnail;
                thumbnailImage.enabled = thumbnail != null;
            }

            if (videoIcon != null)
                videoIcon.SetActive(isVideo);

            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveAllListeners();
                if (onDelete != null)
                    deleteButton.onClick.AddListener(() => onDelete());
            }

            if (slotButton != null)
            {
                slotButton.onClick.RemoveAllListeners();
                if (onTap != null)
                    slotButton.onClick.AddListener(() => onTap());
            }
        }

        public void Clear(bool destroyTexture = true)
        {
            FilePath = null;
            IsVideo = false;

            if (thumbnailImage != null)
            {
                thumbnailImage.texture = null;
                thumbnailImage.enabled = false;
            }

            if (videoIcon != null)
                videoIcon.SetActive(false);

            if (deleteButton != null)
                deleteButton.onClick.RemoveAllListeners();

            if (slotButton != null)
                slotButton.onClick.RemoveAllListeners();

            if (destroyTexture && loadedTexture != null)
                UnityEngine.Object.Destroy(loadedTexture);
                
            loadedTexture = null;
        }

        private void OnDestroy()
        {
            if (loadedTexture != null)
            {
                UnityEngine.Object.Destroy(loadedTexture);
                loadedTexture = null;
            }
        }
    }
}
