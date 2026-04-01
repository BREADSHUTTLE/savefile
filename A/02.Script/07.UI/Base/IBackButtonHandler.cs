namespace CAPYBARA
{
    public interface IBackButtonHandler
    {
        // 뒤로가기 처리 우선순위 (높을수록 먼저 처리)
        // 팝업: 100 이상, View: 0~99
        int BackButtonPriority { get; }
        
        // 현재 뒤로가기 처리가 가능한 상태인지 (활성화 상태 등)
        bool CanHandleBackButton { get; }
        
        // 뒤로가기 버튼이 눌렸을 때 호출
        void OnBackButtonPressed();
    }
}

