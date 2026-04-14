using System;
using System.Collections.Generic;
using System.Linq;

/*
 * 다음은 2D 슈팅 게임의 스테이지를 관리하는 코드입니다.
 * 적들과 플레이어는 서로에게 총알을 쏴서 공격할 수 있습니다.
 * 총알은 한 스테이지에 수십만 개가 나올 정도로 많을 수 있습니다.
 *
 * 아래 코드는 pseudo code로 작성된 것으로, 실제로 컴파일이 되지는 않습니다.
 * 일부 코드는 생략되어 있으며, 일부 코드는 의도적으로 비효율적으로 작성되어 있습니다.
 *
 * 아래 코드를 보고 고쳐야 할 부분을 찾아 그 이유와 어떻게 고쳐야 할지 설명해 주세요.
 * 우선순위에 따라 세가지만 나열해 주시고 코드를 작성할 필요는 없습니다.
 */

//여기 아래에 답변을 적어주세요.
//
// 1) foreach 순회중에 내용을 수정하는 문제가 있습니다.
// update 에서 foreach로 stageObjects를 순회하는 중에 Enemy.FireBullet()이 AddStageObject()를 호출하고, EnemyBullet/PlayerBullet의 Update()가 RemoveStageObject()를 호출합니다.
// 별도 list에 넣어뒀다가 관리하면?? 될 것 같습니다.
//
// 2) EnemyBullet, PlayerBullet에서 base.Update() 호출을 하고 있지 않습니다. Bullet의 update가 위치를 갱신하고 있는데, Bullet 의 Update() 를 호출하지 않으면 총알의 위치가 갱신되지 않습니다.
// 
// 3) player 접근 할때마다 stageObject를 전체 다 돌고 있습니다. FirstOrDefault 로 player를 찾고 있음
// EnemyBullet.Update()에서 GameStage.currentStage.player.AddDamage(this.power); 매 프레임마다 호출되고 있어서, 총알이 많으면 매 프레임마다 전체 탐색을 하니까 성능에 안좋아보입니다.
// 별도로 player를 따로 두고 갱신해서 사용 해야 할 것 같습니다.

class GameStage
{
	public static GameStage currentStage;

	List<StageObject> stageObjects = new List<StageObject>();

	public Player player => stageObjects.FirstOrDefault(x=>x is Player) as Player;

	public void Update(float deltaTime)
	{
		foreach( var stageObject in stageObjects )
		{
			stageObject.Update(deltaTime);
		}
	}

	public void AddStageObject(StageObject stageObject)
	{
		stageObjects.Add(stageObject);
	}

	public void RemoveStageObject(StageObject stageObject)
	{
		stageObjects.Remove(stageObject);
	}
}

class StageObject
{
	public virtual void Update(float deltaTime)
	{
		// 스테이지 오브젝트의 상태를 업데이트하는 함수
	}
}

class Enemy : StageObject
{
	override public void Update(float deltaTime)
	{
		if( ShouldFireBullet() )
		{
			FireBullet();
		}
	}

	void FireBullet()
	{
		Bullet bullet = new EnemyBullet(this.pos, GameStage.currentStage.player.pos);
		GameStage.currentStage.AddStageObject(bullet);
	}
}

class Player : StageObject
{
	//...
}

class Bullet : StageObject
{
	protected Vector3 pos;
	protected Vector3 dir;
	protected float speed;
	protected float radius;
	protected float power;

	public override void Update(float deltaTime)
	{
		pos += dir * speed * deltaTime;
	}
}

class EnemyBullet : Bullet
{
	public EnemyBullet(Vector3 startPos, Vector3 targetPos)
	{
		//...
	}

	public override void Update(float deltaTime)
	{
		float distanceToPlayer = Vector3.length(pos - GameStage.currentStage.player.pos);

		if( distanceToPlayer < radius )
		{
			// 플레이어가 총알에 맞았을 때의 처리
			GameStage.currentStage.RemoveStageObject(this);

			GameStage.currentStage.player.AddDamage(this.power);
		}
	}
}

class PlayerBullet : Bullet
{
	public PlayerBullet(Vector3 startPos, Vector3 targetPos)
	{
		//...
	}

	public override void Update(float deltaTime)
	{
		foreach(var stageObject in GameStage.currentStage.stageObjects )
		{
			if( stageObject is Enemy enemy)
			{
				float distanceToEnemy = Vector3.length(pos - enemy.pos);
				if( distanceToEnemy < radius )
				{
					// 적이 총알에 맞았을 때의 처리
					GameStage.currentStage.RemoveStageObject(this);

					enemy.AddDamage(this.power);
				}
			}
		}
	}
}
