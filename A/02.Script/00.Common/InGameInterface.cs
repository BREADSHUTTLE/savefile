namespace CAPYBARA
{
    public interface ICardTouchCallbackListen
    {
        void CardTouchCallback(int cardRankIndex, bool activeForChange);
    }

    public interface ICardBadugiCardRecommend
    {
        void HighlightCardforChange(int cardRankIndex, bool activeForChange);
    }
    
    public interface IInGameController                                               
    {                                                                                
        void OnOtherPlayerModalInactive(int chairId);                                
    }   
}
