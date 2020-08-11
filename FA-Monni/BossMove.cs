using UnityEngine;
using System.Collections;

public class BossMove : MonoBehaviour {

    public AudioClip deathSound;
    float bossFullHP = 100;
	float myHP = 100;

	BossEffect bossEffect;

	private float MoveSpeed;
	private float oldSpeed;
	private GameManager manager;

	private Transform character;

	private Animator _ani;

	private GameObject ch_model;
	private CharacterMove ch_move;
	private Transform bossHpObj;
	private UIFilledSprite bossHP;
    private CharacterAttack chAttack;
    private Pet ch_pet;

    private int A_chance;
    private int B_chance;
    private int C_chance;

	private float changeTime = 0;
	public float timeSpeed = 1f;
	private float speedChange = 0;
	float bossSpeed;
	Transform[] tempEffect;

	Vector3 temPos = new Vector3(0,0,0);

	AnimatorStateInfo currentBaseState ;

	private static int idleState = Animator.StringToHash("Base Layer.idle"); 
	private static int laserState = Animator.StringToHash("Base Layer.Laser"); 
	private static int centerLaser = Animator.StringToHash("Base Layer.Laser_center"); 
	private static int leftLaser = Animator.StringToHash("Base Layer.Laser_Left"); 
	private static int rightLaser = Animator.StringToHash("Base Layer.Laser_Right"); 
	private static int endLaser = Animator.StringToHash("Base Layer.end"); 

	GameObject smallObj;
	GameObject summonEff;
	GameObject smallEff;
	float posX;

	Transform smallPar;
	Vector3 oldPos;
	Vector3 rePos;
	float smallPos = 10f;

	private InfiniteManager infiManager;
	bool infiniteStand = false;
	float infiniteDir = 0;
	bool _summon = false;
	bool smallDie = false;
	bool summonState = false;

    float poison_Time = 0;
    bool poison_hp = false;
    float hp_Time = 0;

    private BossFinish finish;

    private Animator boss_camera;
    private GameObject _petPos;

    int pattren_index;

    public int A_pattern_power;
    public int B_pattern_power;
    public int C_pattern_power;

    private In_Game_Sound _sound;

	void Awake() {

        _sound = GameObject.Find("In_Game_Sound").GetComponent<In_Game_Sound>();
        

		bossHpObj = GameObject.Find ("Game").transform.FindChild ("state").FindChild ("BossHP");
		bossHP = bossHpObj.GetComponent<UIFilledSprite> ();
		bossEffect = gameObject.GetComponent<BossEffect> ();
        finish = transform.parent.GetComponent<BossFinish>();
		character = GameObject.Find ("Character").transform;
        ch_pet = character.FindChild("Pet").GetComponent<Pet>();
		ch_model = character.FindChild ("Mongni").gameObject;
		ch_move = ch_model.GetComponent<CharacterMove>();
        chAttack = ch_model.GetComponent<CharacterAttack>();
		tempEffect = gameObject.GetComponentsInChildren<Transform> ();
		_ani = gameObject.GetComponent<Animator> ();
		manager = GameObject.Find ("GM").GetComponent<GameManager>();
		smallPar = GameObject.Find ("small").transform;
        boss_camera = Camera.main.GetComponent<Animator>();

        _petPos = GameObject.Find("pet_pos");

        #region 보스 데이터 설정

        bossFullHP = MyConst.Common.GameBossDataList[0]._hp;
        myHP = bossFullHP;
        //Debug.Log(myHP);

        A_pattern_power = MyConst.Common.GameBossDataList[0]._att_01;
        B_pattern_power = MyConst.Common.GameBossDataList[0]._att_02;
        C_pattern_power = MyConst.Common.GameBossDataList[0]._att_03;

        #endregion

        #region 보스 패턴 확률 설정

        pattren_index = MyConst.Common.Get_Boss_pattern_index(701, Game_stage.map_Mode);

        //Debug.Log(MyConst.Common.GameBossPatternDataList[pattren_index]._pattern_rate_01 / 100);
        //Debug.Log(MyConst.Common.GameBossPatternDataList[pattren_index]._pattern_rate_02 / 100);
        //Debug.Log(MyConst.Common.GameBossPatternDataList[pattren_index]._pattern_rate_03 / 100);

        A_chance = MyConst.Common.GameBossPatternDataList[pattren_index]._pattern_rate_01 / 100;
        B_chance = MyConst.Common.GameBossPatternDataList[pattren_index]._pattern_rate_02 / 100;
        C_chance = MyConst.Common.GameBossPatternDataList[pattren_index]._pattern_rate_03 / 100;

        #endregion

        MoveSpeed = ch_move.moveSpeed;
        oldSpeed = MoveSpeed;
	}

	void Start () {
        
		if (ex_mode.infiniteMode) {
			infiManager = GameObject.Find ("GM").GetComponent<InfiniteManager>();
			infiniteStand = true;
		}
        if (Option_music.SOUND_option)
            audio.PlayOneShot(_sound._one_boss.appear);

	}

	void Update () {
		currentBaseState = _ani.GetCurrentAnimatorStateInfo(0);
        if (!manager.countDown && ch_move._hp != 0 && !manager.hit && !manager.stageClear && !manager.Game_Stop)
        {
			if(ex_mode.infiniteMode) {
                infiniteDir = transform.parent.position.z - character.transform.position.z;
				if (infiniteDir < 6.2f )
					infiniteStand = false;
				if(!infiniteStand) {
					if(!infiManager.summonTime)
						MoveSpeed = ch_move.moveSpeed;
					float bossSpeed = MoveSpeed * Time.deltaTime;
					transform.parent.Translate (Vector3.forward * bossSpeed);
				}
			}else {
				float bossSpeed = MoveSpeed * Time.deltaTime;
				transform.parent.Translate (Vector3.forward * bossSpeed);
			}

			if(!infiniteStand) {

                //// 바로 죽는 거 테스트
                //myHP = 0;
                //bossState(myHP);
                ////


				if(currentBaseState.nameHash == idleState && !smallDie && !_summon ) {
					_ani.speed = 1f;
					changeTime += Time.deltaTime;
                    //speedChange += Time.deltaTime;
                    //if(speedChange >= 5f) {
                    //    speedChange = 0;
                    //    timeSpeed -= 0.2f;
                    //    if(timeSpeed <= 0.3f) {
                    //        timeSpeed = 0.3f;
                    //    }
                    //}
                    //Debug.Log(changeTime);
                    if (changeTime >= (MyConst.Common.GameBossPatternDataList[pattren_index]._pattern_term) / 100f)
                    {
						changeTime = 0;
						attack_ani();
					}
				}
			}

			if( _summon ) {
				rePos = transform.parent.localPosition;
				if( rePos.z > oldPos.z + smallPos ){
					MoveSpeed = oldSpeed;
					_ani.SetBool("Summon", true);
					_summon = false;
				}
			}

			if(smallDie) {
                float _dis = transform.parent.position.z - character.transform.position.z;
				if(bossEffect.smallNum > bossEffect.smallCount * 80 / 100) {
					rePos = transform.parent.localPosition;

					MoveSpeed = -2f;
					if( _dis <= 6.5f){
						_ani.SetBool("Summon_end", false);
						MoveSpeed = oldSpeed;
						changeTime = 0;
						bossEffect.smallNum = 0;
						smallDie = false;
						if(ex_mode.infiniteMode)
							infiManager.summonTime = false;
					}
				}
			}

            if (poison_hp)
            {
                poison_Time += Time.deltaTime;
                if (poison_Time >= 15f)
                {
                    poison_Time = 0;
                    poison_hp = false;
                    hp_Time = 0;
                }
                else
                {
                    hp_Time += Time.deltaTime;
                    //Debug.Log(hp_Time);
                    if (hp_Time >= 2f)
                    {
                        hp_Time = 0;
                        damage_poison();
                    }
                }
                
            }

            
		}

	}
	public void attack_ani () {
		changeTime = 0;
        int value = Random.Range(1, A_chance + B_chance + C_chance);
        //Debug.Log(value);
        if (value <= A_chance)
        {
			_ani.SetBool("A_attack", true);
        }
        else if (value <= A_chance + B_chance)
        {
			LaserPos();
		}
        else
        {
			if(ex_mode.infiniteMode)
				infiManager.summonTime = true;
			SummonPos ();
		}
	}

 
	void OnTriggerEnter ( Collider other ) {
		if (other.tag == "shotObject") {
            if (ch_move._hp != 0)
            {
                if (Option_music.SOUND_option)
                    audio.PlayOneShot(_sound._one_boss.damage);

                myHP -= chAttack.attackPower;
                if (ch_pet.addHit)
                {
                    ch_pet.boss_add_damage(bossFullHP);
                    myHP -= ch_pet.addHit_HP;
                }

                if (ch_pet.poison_skill)
                {
                    int ran = Random.Range(0, 100);
                    if (ran < 30)
                        if (!poison_hp)
                        {
                            poison_hp = true;
                            //Debug.Log(poison_hp);
                        }

                }
                bossState(myHP);
                manager.BossScore();
            }
		}
	}
    void damage_poison()
    {
        //Debug.Log("독");
        myHP -= ch_pet.poison_point;
        bossHP.fillAmount = myHP / bossFullHP;
    }

	public void LaserPos () {
		_ani.SetBool("Laser", true);

		if (ch_model.transform.position.x > 0.5) {
			_ani.SetBool("Left", true);
		}
		else if (ch_model.transform.position.x < -0.5){
			_ani.SetBool("Right", true);
		}
		else {
			_ani.SetBool("Center", true);
		}
	}

	public void bossState (float hp) {
		if (hp > 0) {
			bossHP.fillAmount = hp / bossFullHP;
		} else {
			transform.collider.enabled = false;
			bossHP.fillAmount = 0f;
			_ani.SetBool("Die", true);

            if (Option_music.SOUND_option)
                audio.PlayOneShot(_sound._one_boss.death);

            manager._score += MyConst.Common.GameBossDataList[0]._score;
            manager._mon_Score += MyConst.Common.GameBossDataList[0]._score;
            manager._scoreText.text = MyConst.Common.Get_Num_Comma( manager._score );

            if ( ex_mode.infiniteMode )
                infiManager.summonTime = false;
            _summon = false;
            summonState = false;
			if(!ex_mode.infiniteMode ) {
                ch_move._unbeatable = true;
                transform.position = new Vector3(transform.position.x, transform.position.y, character.transform.position.z + 6f);
                boss_camera.enabled = true;
                boss_camera.SetBool("End", true);

				StartCoroutine(resultUI());
				manager.stageClear = true;
			} else {
                //ch_model.collider.enabled = false;
				StartCoroutine(infiniteDie ());	
			}
		}
	}

	public IEnumerator resultUI () {
		yield return new WaitForSeconds (2f);
        finish.bossDie = true;
        yield return new WaitForSeconds(4f);
        manager.gameResult();
	}

	public IEnumerator infiniteDie () {
		yield return new WaitForSeconds (1.5f);

		infiManager.bossAppear = false;
		infiManager.bossDie = true;
        ch_pet.run_item_active = true;
		bossHpObj.gameObject.SetActive (false);
		bossHP.fillAmount = 1f;
        ch_move._unbeatable = false;
		Destroy (transform.parent.gameObject);
	}

	public void SummonPos () {
		summonState = true;
		changeTime = 0;
		_summon = true;
		oldSpeed = MoveSpeed;
		MoveSpeed *= 4;
		oldPos = transform.parent.localPosition;
	}

	public void a_del () {
		_ani.SetBool ("A_attack", false);
	}
	public void s_del () {
//		if(bossEffect.smallCount > bossEffect.smallPull * 80 / 100) {
			smallDie = true;
			oldSpeed = MoveSpeed;
			oldPos = transform.parent.localPosition;
			_ani.SetBool("Summon", false);
//		}
	}

	public void summonEnd () {
		if (summonState) {
			StartCoroutine(bossEffect.summon_Time ()) ;
			summonState = false;
		}
		if(bossEffect.smallNum > bossEffect.smallCount * 80 / 100) {
			_ani.SetBool("Summon_end", true);
		}
	}

	public void l_del () {
		if(currentBaseState.nameHash == centerLaser) {
			_ani.SetBool("Center", false);
			StartCoroutine( twoLaser () );
		} else if (currentBaseState.nameHash == leftLaser) {
			_ani.SetBool("Left", false);
			StartCoroutine( twoLaser () );
		} else if (currentBaseState.nameHash == rightLaser) {
			_ani.SetBool("Right",false);
			StartCoroutine( twoLaser () );
		}
	}
	public IEnumerator twoLaser () {
		float value = Random.Range (1, 100);
        if (value <= 20 && ch_move._hp != 0)
        {
			yield return new WaitForSeconds (0.05f);
			LaserPos();
		}
        else
        {
			_ani.SetBool("Laser", false);
		}
	}

}