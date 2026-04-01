using UnityEngine;


namespace CAPYBARA
{
    public class PlayerActionHistory
    {
        public bool hasChecked { get; private set; } = false;
        public bool hasCalled { get; private set; } = false;
        public bool hasRaised { get; private set; } = false;
        public bool hasBet { get; private set; } = false;
        public int raiseCount { get; private set; } = 0;
        public int callCount { get; private set; } = 0;
    
        public void RecordAction(Partial.ActionType actionType)
        {
            switch (actionType)
            {
                case Partial.ActionType.AtCheck:
                    hasChecked = true;
                    break;
                case Partial.ActionType.AtCall:
                    hasCalled = true;
                    callCount++;
                    break;
                case Partial.ActionType.AtRaise:
                    hasRaised = true;
                    raiseCount++;
                    break;
                case Partial.ActionType.AtBet:
                    hasBet = true;
                    break;
            }
        }
    
        public void ResetForNewRound()
        {
            hasChecked = false;
            hasCalled = false;
            hasRaised = false;
            hasBet = false;
            raiseCount = 0;
            callCount = 0;
        }

    }
}

