using System;
using System.Globalization;

public class Solution
{
    /// 동영상 재생기 - [프로그래머스 - PCCP 기출문제]
    /// <summary>
    /// 시작 때 오프닝 안에 있는지 여부를 먼저 검사 한 뒤,
    /// command 동작마다 오프닝에 있으면 오프닝 끝으로 보내고, 비디오 길이보다 넘어가면 비디오 최대 길이로 보정한다.
    /// </summary>
    public string solution(string video_len, string pos, string op_start, string op_end, string[] commands)
    {
        string answer = "";

        TimeSpan time = TimeSpan.ParseExact(pos, "mm\\:ss", CultureInfo.InvariantCulture);
        TimeSpan opStart = new TimeSpan();
        TimeSpan opEnd = new TimeSpan();

        // commands 시작 전, 현재 위치가 오프닝 안에 있는지 검사
        CheckOpening(time, op_start, op_end, out time, out opStart, out opEnd);

        // commands에 따른 시간 변화
        for (int i = 0; i < commands.Length; i++)
        {
            if (commands[i].ToLower() == "prev")
            {
                if ((time.TotalSeconds + -10) <= 0)
                    time = TimeSpan.Zero;
                else
                    time = time.Add(TimeSpan.FromSeconds(-10));
            }
            else if (commands[i].ToLower() == "next")
            {
                time = time.Add(TimeSpan.FromSeconds(10));
            }

            // command 후 시간이 오프닝 안에 있는지 체크
            CheckOpening(time, op_start, op_end, out time, out opStart, out opEnd);
            // 비디오 마지막 시간 체크 및 비디오 길이보다 길면 end로 보정
            CheckEndVideo(video_len, time, out time);
        }

        answer = StringTime(time);
        return answer;
    }

    private string StringTime(TimeSpan time) => time.ToString(@"mm\:ss");
    private void CheckOpening(TimeSpan pos, string op_start, string op_end, out TimeSpan time, out TimeSpan opStart, out TimeSpan opEnd)
    {
        time = pos;
        opStart = TimeSpan.ParseExact(op_start, "mm\\:ss", CultureInfo.InvariantCulture);
        opEnd = TimeSpan.ParseExact(op_end, "mm\\:ss", CultureInfo.InvariantCulture);

        if (time >= opStart && time <= opEnd)
            time = TimeSpan.FromMinutes(opEnd.Minutes) + TimeSpan.FromSeconds(opEnd.Seconds);
    }

    private void CheckEndVideo(string video_len, TimeSpan pos, out TimeSpan time)
    {
        TimeSpan len = TimeSpan.ParseExact(video_len, "mm\\:ss", CultureInfo.InvariantCulture);
        if (pos >= len)
            time = TimeSpan.FromMinutes(len.Minutes) + TimeSpan.FromSeconds(len.Seconds);
        else
            time = pos;
    }
}