using System;

public class Solution
{
    /// 유연근무제 - [프로그래머스 - 2025 프로그래머스 코드챌린지 1차 예선 문제]
    /// <summary>
    /// 각 사람마다 자신의 timelog를 1차원 배열에 담아서 하나씩 검사한다.
    /// day는 계속 증가하고, 주말은 패스해야 하므로 주말을 따로 체크하도록 했다.
    /// 60분이 넘으면 시간 단위를 바꿔야 하기 때문에 60분 계산이 필요하고,
    /// 24시가 되면 0시로 바꿔야 하니 코드를 추가해야한다. (핵심)
    /// </summary>
    public int solution(int[] schedules, int[,] timelogs, int startday)
    {
        int answer = 0;

        for (int i = 0; i < schedules.Length; i++)
        {
            bool product = true;
            int currentDay = startday;

            int cols = timelogs.GetLength(1);
            for (int t = 0; t < cols; t++)
            {
                if (IsWeekend(currentDay))
                {
                    currentDay = Day(currentDay);
                    continue;
                }

                int time = Time(timelogs[i, t]);
                if (time > Time(schedules[i] + 10))
                {
                    product = false;
                    break;
                }

                currentDay = Day(currentDay);
            }

            if (product)
                answer++;
        }

        return answer;
    }

    private int Time(int time)
    {
        int hour = time / 100;
        int min = time % 100;

        hour += min / 60;   // 60분 넘으면 시간 단위 바뀜
        min %= 60;          // 남은 시간 분으로 
        hour %= 24;         // 24시간이면 0시로 바꿈

        return hour * 100 + min;
    }

    private int Day(int day) => day >= 7 ? 1 : day + 1;
    private bool IsWeekend(int day) => day == 6 || day == 7;
}