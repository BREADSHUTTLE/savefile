using System.Collections.Generic;

namespace CAPYBARA
{
        public static class Constraints
        {
                public const string COOKIE_PREF_KEY = "WebViewCookies";
                public const float BadugiKickTime = 120;
                public const float HoldemKickTime = 150;

                public const int BadugiKickMaxCount = 3;
                public const int HoldemKickMaxCount = 4;

                public const int MaxGameCountForKickVote = 5;

                public const int MaxSpokerPlayerCount = 5;
                public const int MaxBadugiPlayerCount = 5;
                public const int MaxHoldemPlayerCount = 9;
                public const float SpokerActionMaxTime = 6.0f;

                public const float DealingCardTime = 5.0f;

                public const string PlayerPrefsKey = "USER_LOCAL_SAVED_DATA";

                public const string TermsOfServiceUrl = "https://www.atozgames.net/terms";
                public const string operatingpolicyUrl = "https://www.atozgames.net/operating-policy";
                public const string privacypolicyUrl = "https://www.atozgames.net/privacy-policy";
                
                public const string OneLinkBaseUrl = "https://atoz.onelink.me/uHhr";

                public const long MaxBetChip = 100000000;

                public const int LossLimitTen = 1000000000;
                public const int LossLimitThree = 300000000;

                public const int RealPurchaseMaxMoney = 1000000;

                public const int REQUIRED_POINTS = 100000;
                public const int SevenOdiMaxCardCount = 7;

                // 클래스별 한도 (보유금액 + 금고)
                public static long GetMaxLimit()
                {
                        return CPPlayer.Inventory.classNumber switch
                        {
                                1 => 3000000000,   // B 클래스: 30억
                                2 => 7000000000,   // A 클래스: 70억
                                3 => 14000000000,  // S 클래스: 140억
                                _ => 1500000000    // 클래스 없음: 15억
                        };
                }
                
                // 클래스별 골드 보유 한도
                public static long GetMaxLimitGold() => GetMaxLimit();
                
                // 클래스별 금고 한도
                public static long GetMaxLimitVault() => GetMaxLimit();

                /// <summary>
                /// 홀덤연출시간 (서버에서 가져오기전 임시 변수)
                /// </summary>

                #region holdem time

                public const float holdemCommunityFadeTime = 0.4f;
                public const float holdemCommunityFlipTime = 0.4f;
                public const float holdemCommunitySlideTime = 0.4f;


                #endregion

                #region badugi time

                public const float badugiRecommendCardHighlightTime = 0.3f;
                public const float badugiResultWaitTime = 0.3f;

                public const float badugiEachPlayerResultEventTime = 0.4f;
                public const float badugiEachPlayerResultEventWaitTime = 0.2f;
                public const float badugiTotalResultEventTime = 3.0f;

                #endregion


                #region sevenodi time

                public const float sevenodiCardFadeTime = 0.4f;
                public const float sevenodiResultWaitTime = 0.3f;

                public const float sevenodiEachPlayerResultEventTime = 0.4f;
                public const float sevenodiEachPlayerResultEventWaitTime = 0.2f;
                public const float sevenodiTotalResultEventTime = 3.0f;
                public const int MAX_WEEKLY_COUNT = 3;

                #endregion







#if SERVER_PRODUCTION
public const string APIxKey="84XwkaNt3umDcUAXRJcBgQPEjfN6KGwK";
public const string APIxSecret="65CiGI7LtoZc4qiGlHion8JvIkCLaPHu";
#elif SERVER_STAGE
        public const string APIxKey = "40IeaTBVR7b3OtzG0dOVpfL7DITRpex6";
        public const string APIxSecret = "AA8T6g4U8vBQYRzhnbEUnWJWM3JQ8uWl";
#else
                public const string APIxKey = "84XwkaNt3umDcUAXRJcBgQPEjfN6KGwK";
                public const string APIxSecret = "65CiGI7LtoZc4qiGlHion8JvIkCLaPHu";
#endif


#if SERVER_PRODUCTION
    public const string BASE_URL = "https://envoy.atozgames.net";
#elif SERVER_STAGE
        public const string BASE_URL = "https://envoy.staging.atozgames.net";
#else
                public const string BASE_URL = "https://envoy.dev.atozgames.net";
#endif
                public static readonly Dictionary<string, int> HandStars = new()
                {
                        // ★★★★★
                        ["AA"]=5, ["KK"]=5, ["QQ"]=5, ["AKs"]=5, ["AKo"]=5,

                        // ★★★★
                        ["JJ"]=4, ["TT"]=4, ["AQs"]=4, ["99"]=4, ["AJs"]=4, ["KQs"]=4,
                        ["88"]=4, ["ATs"]=4, ["AQo"]=4, ["KJs"]=4, ["KTs"]=4,
                        ["QJs"]=4, ["AJo"]=4,

                        // ★★★
                        ["KQo"]=3, ["QTs"]=3, ["A9s"]=3, ["77"]=3, ["ATo"]=3,
                        ["JTs"]=3, ["A5s"]=3, ["KJo"]=3, ["A8s"]=3, ["K9s"]=3,
                        ["QJo"]=3, ["A7s"]=3, ["KTo"]=3, ["Q9s"]=3, ["66"]=3,
                        ["A6s"]=3, ["QTo"]=3, ["J9s"]=3, ["A9o"]=3, ["T9s"]=3,
                        ["A4s"]=3, ["K8s"]=3, ["JTo"]=3,

                        // ★★
                        ["K7s"]=2, ["A8o"]=2, ["A3s"]=2, ["Q8s"]=2, ["K9o"]=2,
                        ["A2s"]=2, ["K6s"]=2, ["J8s"]=2, ["T8s"]=2, ["A7o"]=2,
                        ["55"]=2, ["Q9o"]=2, ["98s"]=2, ["K5s"]=2, ["Q7s"]=2,
                        ["J9o"]=2, ["A5o"]=2, ["T9o"]=2, ["A6o"]=2, ["K4s"]=2,
                        ["K8o"]=2, ["Q6s"]=2, ["J7s"]=2, ["T7s"]=2, ["A4o"]=2,
                        ["97s"]=2, ["K3s"]=2, ["87s"]=2, ["Q5s"]=2, ["K7o"]=2,
                        ["44"]=2, ["Q8o"]=2, ["A3o"]=2,

                        // ★
                        ["K2s"]=1, ["J8o"]=1, ["Q4s"]=1, ["T8o"]=1, ["J6s"]=1,
                        ["K6o"]=1, ["A2o"]=1, ["T6s"]=1, ["98o"]=1, ["76s"]=1,
                        ["86s"]=1, ["96s"]=1, ["Q3s"]=1, ["J5s"]=1, ["K5o"]=1,
                        ["Q7o"]=1, ["Q2s"]=1, ["J4s"]=1, ["33"]=1, ["65s"]=1,
                        ["J7o"]=1, ["T7o"]=1, ["K4o"]=1, ["75s"]=1, ["T5s"]=1,
                        ["Q6o"]=1, ["J3s"]=1, ["95s"]=1, ["87o"]=1, ["85s"]=1,
                        ["97o"]=1, ["T4s"]=1, ["K3o"]=1, ["J2s"]=1, ["54s"]=1,
                        ["Q5o"]=1, ["64s"]=1, ["T3s"]=1, ["22"]=1
                };
                


        }

}
