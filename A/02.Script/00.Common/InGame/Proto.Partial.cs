using UnityEngine;

namespace CAPYBARA.Partial
{
    public enum BetSizeType
    {
        BsNone=-1,
        BsFold=0,
        BsCheck,
        BsBbing,
        BsCall,
        BsDdadang,
        BsQuater,
        BsHalf,
        BsAllin,
        BsMax,
        BsEnd,
    }

    public enum ActionType
    {
        AtNone=-1,
        AtFold=0,
        AtCheck,
        AtBet,
        AtCall,
        AtRaise,
        AtAllin,
    }
    public interface IEmoteRes
    {
        int tableId { get; }
        int chairId { get; }
    }
    public interface IEmoteNoti
    {
        int tableId { get; }
        int fromChairId { get; }
        int toChairId { get; }
        string emoteName { get; }
    }

    public interface IActionNoti
    {
        ActionType actionType { get; }
        BetSizeType betSizeType { get; }
        
    }

  
}
namespace CAPYBARA.holdem
{
    public sealed partial class EmoteRes : Partial.IEmoteRes
    {
        public int tableId => this.TableId;
        public int chairId => this.ChairId;
    }
    
    public sealed partial class EmoteNoti :Partial. IEmoteNoti
    {
        public int tableId => this.TableId;
        public int fromChairId => this.From;
        public int toChairId => this.To;
        public string emoteName => this.Emotion;
    }
    
    public sealed partial class ActionNoti : Partial.IActionNoti
    {
        public Partial.ActionType actionType=>ConvertActionType(this.Action);
        public Partial.BetSizeType betSizeType => ConvertBetType(this.BetSizeType);
        
        private Partial.BetSizeType ConvertBetType(Common.BetSizeType src)
        {
            return src switch
            {
                Common.BetSizeType.BsNone    => Partial.BetSizeType.BsNone,
                Common.BetSizeType.BsFold    => Partial.BetSizeType.BsFold,
                Common.BetSizeType.BsCheck   => Partial.BetSizeType.BsCheck,
                Common.BetSizeType.BsBbing   => Partial.BetSizeType.BsBbing,
                Common.BetSizeType.BsCall    => Partial.BetSizeType.BsCall,
                Common.BetSizeType.BsDdadang => Partial.BetSizeType.BsDdadang,
                Common.BetSizeType.BsQuater  => Partial.BetSizeType.BsQuater,
                Common.BetSizeType.BsHalf    => Partial.BetSizeType.BsHalf,
                Common.BetSizeType.BsAllin   => Partial.BetSizeType.BsAllin,
                Common.BetSizeType.BsMax     => Partial.BetSizeType.BsMax,
                Common.BetSizeType.BsEnd     => Partial.BetSizeType.BsEnd,
                _                             => Partial.BetSizeType.BsNone
            };
        }
        private Partial.ActionType ConvertActionType(Common.ActionType src)
        {
            return src switch
            {
                Common.ActionType.AtNone     => Partial.ActionType.AtNone,
                Common.ActionType.AtFold     => Partial.ActionType.AtFold,
                Common.ActionType.AtCheck    => Partial.ActionType.AtCheck,
                Common.ActionType.AtBet      => Partial.ActionType.AtBet,
                Common.ActionType.AtCall     => Partial.ActionType.AtCall,
                Common.ActionType.AtRaise    => Partial.ActionType.AtRaise,
                Common.ActionType.AtAllin    => Partial.ActionType.AtAllin,
            };
        }
    }
}

namespace CAPYBARA.badugi
{
    public sealed partial class EmoteRes : Partial.IEmoteRes
    {
        public int tableId => this.TableId;
        public int chairId => this.ChairId;
    }
    
    public sealed partial class EmoteNoti :Partial. IEmoteNoti
    {
        public int tableId => this.TableId;
        public int fromChairId => this.From;
        public int toChairId => this.To;
        public string emoteName => this.Emotion;
    }
    public sealed partial class ActionNoti : Partial.IActionNoti
    {
        public Partial.ActionType actionType=>ConvertActionType(this.Action);
        public Partial.BetSizeType betSizeType => ConvertBetType(this.BetSizeType);
        
        private Partial.BetSizeType ConvertBetType(Common.BetSizeType src)
        {
            return src switch
            {
                Common.BetSizeType.BsNone    => Partial.BetSizeType.BsNone,
                Common.BetSizeType.BsFold    => Partial.BetSizeType.BsFold,
                Common.BetSizeType.BsCheck   => Partial.BetSizeType.BsCheck,
                Common.BetSizeType.BsBbing   => Partial.BetSizeType.BsBbing,
                Common.BetSizeType.BsCall    => Partial.BetSizeType.BsCall,
                Common.BetSizeType.BsDdadang => Partial.BetSizeType.BsDdadang,
                Common.BetSizeType.BsQuater  => Partial.BetSizeType.BsQuater,
                Common.BetSizeType.BsHalf    => Partial.BetSizeType.BsHalf,
                Common.BetSizeType.BsAllin   => Partial.BetSizeType.BsAllin,
                Common.BetSizeType.BsMax     => Partial.BetSizeType.BsMax,
                Common.BetSizeType.BsEnd     => Partial.BetSizeType.BsEnd,
                _                             => Partial.BetSizeType.BsNone
            };
        }
        private Partial.ActionType ConvertActionType(Common.ActionType src)
        {
            return src switch
            {
                Common.ActionType.AtNone     => Partial.ActionType.AtNone,
                Common.ActionType.AtFold     => Partial.ActionType.AtFold,
                Common.ActionType.AtCheck    => Partial.ActionType.AtCheck,
                Common.ActionType.AtBet      => Partial.ActionType.AtBet,
                Common.ActionType.AtCall     => Partial.ActionType.AtCall,
                Common.ActionType.AtRaise    => Partial.ActionType.AtRaise,
                Common.ActionType.AtAllin    => Partial.ActionType.AtAllin,
            };
        }
    }
}

namespace CAPYBARA.sevenPoker
{
    public sealed partial class EmoteRes : Partial.IEmoteRes
    {
        public int tableId => this.TableId;
        public int chairId => this.ChairId;
    }
    
    public sealed partial class EmoteNoti : Partial.IEmoteNoti
    {
        public int tableId => this.TableId;
        public int fromChairId => this.From;
        public int toChairId => this.To;
        public string emoteName => this.Emotion;
    }
    public sealed partial class ActionNoti : Partial.IActionNoti
    {
        public Partial.ActionType actionType=>ConvertActionType(this.Action);
        public Partial.BetSizeType betSizeType => ConvertBetType(this.BetSizeType);
        
        private Partial.BetSizeType ConvertBetType(Common.BetSizeType src)
        {
            return src switch
            {
                Common.BetSizeType.BsNone    => Partial.BetSizeType.BsNone,
                Common.BetSizeType.BsFold    => Partial.BetSizeType.BsFold,
                Common.BetSizeType.BsCheck   => Partial.BetSizeType.BsCheck,
                Common.BetSizeType.BsBbing   => Partial.BetSizeType.BsBbing,
                Common.BetSizeType.BsCall    => Partial.BetSizeType.BsCall,
                Common.BetSizeType.BsDdadang => Partial.BetSizeType.BsDdadang,
                Common.BetSizeType.BsQuater  => Partial.BetSizeType.BsQuater,
                Common.BetSizeType.BsHalf    => Partial.BetSizeType.BsHalf,
                Common.BetSizeType.BsAllin   => Partial.BetSizeType.BsAllin,
                Common.BetSizeType.BsMax     => Partial.BetSizeType.BsMax,
                Common.BetSizeType.BsEnd     => Partial.BetSizeType.BsEnd,
                _                             => Partial.BetSizeType.BsNone
            };
        }
        private Partial.ActionType ConvertActionType(Common.ActionType src)
        {
            return src switch
            {
                Common.ActionType.AtNone     => Partial.ActionType.AtNone,
                Common.ActionType.AtFold     => Partial.ActionType.AtFold,
                Common.ActionType.AtCheck    => Partial.ActionType.AtCheck,
                Common.ActionType.AtBet      => Partial.ActionType.AtBet,
                Common.ActionType.AtCall     => Partial.ActionType.AtCall,
                Common.ActionType.AtRaise    => Partial.ActionType.AtRaise,
                Common.ActionType.AtAllin    => Partial.ActionType.AtAllin,
            };
        }
    }
}
