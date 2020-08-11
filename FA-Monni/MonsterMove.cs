using UnityEngine;
using System.Collections;

public class MonsterMove : MonoBehaviour {

    Animator _Ani;
    AnimatorStateInfo currentHornet;
    private static int RunState = Animator.StringToHash("Base Layer.Run");
    GameObject bar;

    private Pet _pet;

    private AudioSource _audio;
    private AudioClip run_sound;
    private AudioClip die_sound;
    private MonsterDie[] monster_data;
    bool state_run = false;
	void Awake () {
        monster_data = gameObject.GetComponentsInChildren<MonsterDie>();
        
        _Ani = transform.FindChild("Monster").GetComponent<Animator>();
        bar = gameObject.transform.FindChild("HP").gameObject;
        _pet = GameObject.Find("Pet").GetComponent<Pet>();

        
	}

    void Start()
    {
        foreach (MonsterDie Monster in monster_data)
        {
            _audio = Monster.gameObject.GetComponent<AudioSource>();
            run_sound = Monster.move_sound;
            die_sound = Monster.die_sound;
        }
    }

	void Update () {

        Vector3 view = Camera.main.WorldToScreenPoint(transform.position);
        if (view.y < -200f)
            StartCoroutine(del());

        if (gameObject.transform.childCount > 1)
        {
            currentHornet = _Ani.GetCurrentAnimatorStateInfo(0);
            if (currentHornet.nameHash == RunState)
            {
                if (_Ani.GetBool("Find") && !_Ani.GetBool("Die"))
                {
                    gameObject.transform.Translate(Vector3.back * 5f * Time.deltaTime);
                }

                if (!state_run)
                {
                    state_run = true;
                    run_state_sound();
                }
            }

            if (_Ani.GetBool("Die") && state_run)
            {
                if (Option_music.SOUND_option)
                {
                    state_run = false;
                    _audio.audio.loop = false;
                    //_audio.audio.Stop();
                    _audio.audio.clip = die_sound;
                    _audio.audio.Play();
                }
            }
        }
        else
        {
            bar.SetActive(false);
            gameObject.collider.enabled = false;
        }
	}

    void run_state_sound()
    {
        if (Option_music.SOUND_option)
        {
            _audio.audio.loop = true;
            _audio.audio.clip = run_sound;
            _audio.audio.Play();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.collider.tag == "player")
        {
            int percent = _pet.hide_monster();

            if (percent != 0 && percent <= _pet.hide_value)
            {
                //Debug.Log("인식불가");
                _pet.text_effect();
            }
            else
            {
                gameObject.collider.enabled = false;
                _Ani.SetBool("Find", true);
            }
        }
    }

    public IEnumerator del()
    {
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }

}
