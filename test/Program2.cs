using System;

/*
 * 비트 다루기
 *    - MakeBitRange 함수를 구현하시면 됩니다.
 *    - 출력값은 from 번째 비트부터 to 번째 비트까지 1로 세팅된 값이어야 합니다.
 *    - 예) MakeBitRange(1,4) = 00011110 (0x0000001E)
 *    - from, to는 0~31까지 값이며, to >= from 입니다.
 */
class Program
{
	static uint MakeBitRange(uint from, uint to)
	{
		uint bits = to - from + 1;
		uint mask = bits >= 32 ? 0xFFFFFFFFu : (1u << (int)bits) - 1;	// uint unsigned 32칸일 경우 전부 1
		return mask << (int)from;	// 위치로 밀기
	}
}

