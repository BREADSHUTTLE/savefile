using System.Collections.Generic;
using UnityEngine;


namespace CAPYBARA
{
    public class GameRuleConfig
    {
        public bool allowRaiseAfterCheck = true;  // 체크 후 레이드 허용 여부
        public bool allowRaiseAfterCall = true;        // 콜 후 레이즈 허용 여부
        public int maxRaisesPerRound = 2;               // 라운드당 최대 레이즈 횟수
        public bool oneDdadangPerRound = true;          // 라운드당 따당 1회 제한

    }

    public interface IGameRuleProvider
    {
       List<GameRuleConfig>  GetRuleConfig(GameType gameType);
        void UpdateRules(GameType gameType, List<GameRuleConfig>   newRules);
    }

    public class GameRuleProvider : IGameRuleProvider
    {
        private readonly Dictionary<GameType, List<GameRuleConfig>> _gameRules;
    
        public GameRuleProvider()
        {
            _gameRules = new Dictionary<GameType, List<GameRuleConfig>>();
            InitializeDefaultRules();
        }
    
        private void InitializeDefaultRules()
        {
            _gameRules[GameType.LOW_BADUGI] = new List<GameRuleConfig>();

            _gameRules[GameType.LOW_BADUGI].Add(new GameRuleConfig() { allowRaiseAfterCheck = false,allowRaiseAfterCall = false,  maxRaisesPerRound = 1,  oneDdadangPerRound = true});
            _gameRules[GameType.LOW_BADUGI].Add(new GameRuleConfig() { allowRaiseAfterCheck = false,allowRaiseAfterCall = true,  maxRaisesPerRound = 2,  oneDdadangPerRound = true});
            _gameRules[GameType.LOW_BADUGI].Add(new GameRuleConfig() { allowRaiseAfterCheck = false,allowRaiseAfterCall = true,  maxRaisesPerRound = 2,  oneDdadangPerRound = true});
            _gameRules[GameType.LOW_BADUGI].Add(new GameRuleConfig() { allowRaiseAfterCheck = false,allowRaiseAfterCall = true,  maxRaisesPerRound = 2,  oneDdadangPerRound = true});
            
            _gameRules[GameType.SEVEN_POKER] = new List<GameRuleConfig>();

            _gameRules[GameType.SEVEN_POKER].Add(new GameRuleConfig() { allowRaiseAfterCheck = false,allowRaiseAfterCall = false,  maxRaisesPerRound = 1,  oneDdadangPerRound = true});
            _gameRules[GameType.SEVEN_POKER].Add(new GameRuleConfig() { allowRaiseAfterCheck = false,allowRaiseAfterCall = true,  maxRaisesPerRound = 2,  oneDdadangPerRound = true});
            _gameRules[GameType.SEVEN_POKER].Add(new GameRuleConfig() { allowRaiseAfterCheck = false,allowRaiseAfterCall = true,  maxRaisesPerRound = 2,  oneDdadangPerRound = true});
            _gameRules[GameType.SEVEN_POKER].Add(new GameRuleConfig() { allowRaiseAfterCheck = false,allowRaiseAfterCall = true,  maxRaisesPerRound = 2,  oneDdadangPerRound = true});
        }
    
        public List<GameRuleConfig> GetRuleConfig(GameType gameType)
        {
            return _gameRules.GetValueOrDefault(gameType, _gameRules[GameType.LOW_BADUGI]);
        }
    
        public void UpdateRules(GameType gameType, List<GameRuleConfig> newRules)
        {
            _gameRules[gameType] = newRules;
        }
    }

}
