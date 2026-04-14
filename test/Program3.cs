using System;
using System.Collections.Generic;


/*
 * 테트리스 게임을 구현 중입니다.
 * 
 * TetrisState 클래스는 테트리스 판에 배치된 블록들의 상태를 나타냅니다.
 * Get, Set, CheckCompleteLine 함수를 구현하기 전에
 * TetrisState 클래스의 내부 변수를 1안과 2안 중 어떤 방식으로 할지 먼저 결정해야 합니다.
 * 
 * 코드의 가독성을 우선시 한다면 1안과 2안 중 어떤 방식을 선택하겠습니까? 그리고 그 이유는 무엇입니까?
 */

//여기 아래에 답변을 적어주세요.
//
// 가독성을 우선시한다면 1안을 선택하겠습니다.
//
// Get/Set 구현이 blocks[x, y]로 배열로 직관적이어서, 비트 연산 지식 없이도 코드를 바로 이해할 수 있다고 생각합니다.
// 2안은 비트로 계산해서 풀면 가능 할 것 같은데 코드를 읽는 사람이 비트 연산에 익숙하지 않으면 이해하기 힘들지 않을까 생각합니다.
// 2안은 그냥 list remove 하면 한줄로 작성이 가능하다고 생각하지만, 1안도 for문으로 사용할 수 있다고 생각합니다. 코드가 길어지더라도 직관적이라 생각합니다.

class TetrisState
{
	const int TETRIS_SIZE_X = 10;
	const int TETRIS_SIZE_Y = 20;

    // bool Get(int x, int y) { throw new NotImplementedException(); }         // x, y 지점에 블록이 있는지 체크하는 함수
    // void Set(int x, int y) { throw new NotImplementedException(); }         // x, y 지점에 블록이 있다고 설정하는 함수
    // void CheckCompleteLine() { throw new NotImplementedException(); }       // 완성된 줄을 체크하고 제거하는 함수

	//1안)
	bool[,] blocks = new bool[TETRIS_SIZE_X, TETRIS_SIZE_Y];
	//2안)
	// List<ushort> lines = new List<ushort>();
	// 2안의 경우
	// lines를 비트로 연산 ex 1100100100 
	// 1이 블록이 있고, 0이 없는것으로 gat 에서 lines 의 1 << x 이런 느낌으로 비트 확인
	// set 에서 lines |= 1 << x 이런 느낌으로 비트 설정
	// 전부 1이되면 리스트에서 Remove 하면 끝남
	bool Get(int x, int y) { return blocks[x, y]; }

	void Set(int x, int y) { blocks[x, y] = true; }

	void CheckCompleteLine()
	{
		for (int y = TETRIS_SIZE_Y - 1; y >= 0; y--)	// 위에서 아래로 내려가면서 체크
		{
			bool isComplete = true;
			for (int x = 0; x < TETRIS_SIZE_X; x++)	// 줄 전체를 체크
			{
				if (!blocks[x, y])	// 블록이 없으면 완성된 줄이 아님
				{
					isComplete = false;
					break;
				}
			}

			if (isComplete)	// 완성된 줄이면 제거
			{
				for (int moveY = y; moveY < TETRIS_SIZE_Y - 1; moveY++)
				{
					for (int x = 0; x < TETRIS_SIZE_X; x++)
						blocks[x, moveY] = blocks[x, moveY + 1];	// 한줄씩 복사
				}

				for (int x = 0; x < TETRIS_SIZE_X; x++)
					blocks[x, TETRIS_SIZE_Y - 1] = false; //마지막 줄은 가져올거 없음

				y++;
			}
		}
	}
}

