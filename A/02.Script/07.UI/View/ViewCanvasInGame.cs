using CAPYBARA.Bundles;
using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class ViewCanvasInGame : ViewCanvas
    {
        public HoldemViewer HoldemView;
        public BadugiViewer badugiView;
        public SPokerViewer sevenpokerView;

        public GameObject afkPanel;
        public CPButton afkBtn;
        
        public GameObject waitGamePanel;
        
        public InGameOption ingameOptionWindow;
        public InGameOption ingameOptionWindow_badugi;
        public InGameOption ingameOptionWindow_SPoker;
        
        private void OnApplicationPause(bool pauseStatus)
        {
            // 인게임 상태일 때만 AFK 설정 (아웃게임/로비에서는 AFK 팝업 표시 안함)
            if(pauseStatus && CPPlayer.InGame.isInGame)
                CPPlayer.InGame.isUserAFK = true;
        }

        private void OnApplicationQuit()
        {
            if(CPPlayer.InGame.isInGame)
                CPPlayer.InGame.isUserAFK = true;
        }
        
        public TMP_Text fpsText;
        private float deltaTime = 0.0f;

        void Update()
        {
            // 델타 타임 누적 (부드러운 평균값을 위해)
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        
            float fps = 1.0f / deltaTime;
            fpsText.text = string.Format("{0:0.} FPS", fps);
        }
    }
}
