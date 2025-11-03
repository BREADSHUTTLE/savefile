using System;

public class Solution
{
    /// 유연근무제 - [프로그래머스 - 2025 프로그래머스 코드챌린지 1차 예선 문제]
    /// <summary>
    /// 각 사람마다 자신의 timelog를 1차원 배열에 담아서 하나씩 검사한다.
    /// day는 계속 증가하고, 주말은 패스해야 하므로 주말을 따로 체크하도록 했다.
    /// </summary>
    int currentDay = -1;

    public int solution(int[] schedules, int[,] timelogs, int startday)
    {
        int answer = 0;
        currentDay = startday;

        for (int i = 0; i < schedules.Length; i++)
        {
            bool product = true;

            int cols = timelogs.GetLength(1);
            int[] time = new int[cols];
            for (int j = 0; j < cols; j++)
                time[j] = timelogs[i, j];   // 새로운 배열에 복사

            for (int t = 0; t < time.Length; t++)
            {
                if (IsWeekend(currentDay))
                {
                    currentDay = Day();
                    continue;
                }

                if (time[t] > (schedules[i] + 10))
                {
                    product = false;
                    break;
                }

                currentDay = Day();
            }

            if (product)
                answer++;
        }

        return answer;
    }

    private bool IsWeekend(int day) => day > 5 && day < 8;
    private int Day() => currentDay >= 7 ? 0 : currentDay + 1;
}