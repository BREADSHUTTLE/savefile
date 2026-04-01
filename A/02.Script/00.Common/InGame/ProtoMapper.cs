using UnityEngine;
using CAPYBARA.Partial;

namespace CAPYBARA
{
    public static class ProtoMapper
    {
        public static Common.BetSizeType HoldemBettingActionType(Partial.BetSizeType bettingActionType)
        {
            return bettingActionType switch
            {
                Partial.BetSizeType.BsNone    => Common.BetSizeType.BsNone,
                Partial.BetSizeType.BsFold    => Common.BetSizeType.BsFold,
                Partial.BetSizeType.BsCheck   => Common.BetSizeType.BsCheck,
                Partial.BetSizeType.BsBbing   => Common.BetSizeType.BsBbing,
                Partial.BetSizeType.BsCall    => Common.BetSizeType.BsCall,
                Partial.BetSizeType.BsDdadang => Common.BetSizeType.BsDdadang,
                Partial.BetSizeType.BsQuater  => Common.BetSizeType.BsQuater,
                Partial.BetSizeType.BsHalf    => Common.BetSizeType.BsHalf,
                Partial.BetSizeType.BsAllin   => Common.BetSizeType.BsAllin,
                Partial.BetSizeType.BsMax     => Common.BetSizeType.BsMax,
                Partial.BetSizeType.BsEnd     => Common.BetSizeType.BsEnd,
                _                             => Common.BetSizeType.BsNone
            };
        }
        public static Common.ActionType HoldemActionType(Partial.ActionType bettingActionType)
        {
            return bettingActionType switch
            {
                Partial.ActionType.AtNone     => Common.ActionType.AtNone,
                Partial.ActionType.AtFold     => Common.ActionType.AtFold,
                Partial.ActionType.AtCheck    => Common.ActionType.AtCheck,
                Partial.ActionType.AtBet      => Common.ActionType.AtBet,
                Partial.ActionType.AtCall     => Common.ActionType.AtCall,
                Partial.ActionType.AtRaise    => Common.ActionType.AtRaise,
                Partial.ActionType.AtAllin    => Common.ActionType.AtAllin,
            };
        }
        
        public static Common.BetSizeType BadugiBettingActionType(Partial.BetSizeType bettingActionType)
        {
            return bettingActionType switch
            {
                Partial.BetSizeType.BsNone    => Common.BetSizeType.BsNone,
                Partial.BetSizeType.BsFold    => Common.BetSizeType.BsFold,
                Partial.BetSizeType.BsCheck   => Common.BetSizeType.BsCheck,
                Partial.BetSizeType.BsBbing   => Common.BetSizeType.BsBbing,
                Partial.BetSizeType.BsCall    => Common.BetSizeType.BsCall,
                Partial.BetSizeType.BsDdadang => Common.BetSizeType.BsDdadang,
                Partial.BetSizeType.BsQuater  => Common.BetSizeType.BsQuater,
                Partial.BetSizeType.BsHalf    => Common.BetSizeType.BsHalf,
                Partial.BetSizeType.BsAllin   => Common.BetSizeType.BsAllin,
                Partial.BetSizeType.BsMax     => Common.BetSizeType.BsMax,
                Partial.BetSizeType.BsEnd     => Common.BetSizeType.BsEnd,
                _                             => Common.BetSizeType.BsNone
            };
        }
        
        public static Common.ActionType BadugiActionType(Partial.ActionType bettingActionType)
        {
            return bettingActionType switch
            {
                Partial.ActionType.AtNone     => Common.ActionType.AtNone,
                Partial.ActionType.AtFold     => Common.ActionType.AtFold,
                Partial.ActionType.AtCheck    => Common.ActionType.AtCheck,
                Partial.ActionType.AtBet      => Common.ActionType.AtBet,
                Partial.ActionType.AtCall     => Common.ActionType.AtCall,
                Partial.ActionType.AtRaise    => Common.ActionType.AtRaise,
                Partial.ActionType.AtAllin    => Common.ActionType.AtAllin,
            };
        }

        
        public static Common.BetSizeType SevenPokerBettingActionType(Partial.BetSizeType bettingActionType)
        {
            return bettingActionType switch
            {
                Partial.BetSizeType.BsNone    => Common.BetSizeType.BsNone,
                Partial.BetSizeType.BsFold    => Common.BetSizeType.BsFold,
                Partial.BetSizeType.BsCheck   => Common.BetSizeType.BsCheck,
                Partial.BetSizeType.BsBbing   => Common.BetSizeType.BsBbing,
                Partial.BetSizeType.BsCall    => Common.BetSizeType.BsCall,
                Partial.BetSizeType.BsDdadang => Common.BetSizeType.BsDdadang,
                Partial.BetSizeType.BsQuater  => Common.BetSizeType.BsQuater,
                Partial.BetSizeType.BsHalf    => Common.BetSizeType.BsHalf,
                Partial.BetSizeType.BsAllin   => Common.BetSizeType.BsAllin,
                Partial.BetSizeType.BsMax     => Common.BetSizeType.BsMax,
                Partial.BetSizeType.BsEnd     => Common.BetSizeType.BsEnd,
                _                             => Common.BetSizeType.BsNone
            };
        }
        
        public static Common.ActionType SevenPokerActionType(Partial.ActionType bettingActionType)
        {
            return bettingActionType switch
            {
                Partial.ActionType.AtNone     => Common.ActionType.AtNone,
                Partial.ActionType.AtFold     => Common.ActionType.AtFold,
                Partial.ActionType.AtCheck    => Common.ActionType.AtCheck,
                Partial.ActionType.AtBet      => Common.ActionType.AtBet,
                Partial.ActionType.AtCall     => Common.ActionType.AtCall,
                Partial.ActionType.AtRaise    => Common.ActionType.AtRaise,
                Partial.ActionType.AtAllin    => Common.ActionType.AtAllin,
            };
        }
    }

}
