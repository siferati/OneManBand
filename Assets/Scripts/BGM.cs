﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGM : MonoBehaviour {
    [System.Serializable]
    public class AudioList
    {
        public AudioClip[] aClips;
    }

	// start > while > end
	private abstract class State {
        protected BGM inst;
        protected int iAudio;
		protected bool already;

        public State(BGM inst)
        {
            this.inst = inst;
			already = false;
        }

		public abstract void update();

        public void updateVolume(float volume)
        {
            inst.bgmASrc.volume = volume;
        }
	}

	private class StartState: State {

		public StartState(BGM inst) : base(inst)
		{
			iAudio = 0;
            inst.bgmASrc.clip = inst.aStart[iAudio];
            inst.bgmASrc.volume = StaticSettings.volumeBGM;
			inst.bgmASrc.Play();
		}

		public override void update() {
            if (!inst.bgmASrc.isPlaying)
            {
                if (iAudio < inst.aStart.Length - 1)
                {
                    inst.bgmASrc.clip = inst.aStart[++iAudio];
                    inst.bgmASrc.volume = StaticSettings.volumeBGM;
                    inst.bgmASrc.Play();
                }
				else
				{
					inst.state = new WhileState(inst);
				}
            }

            // randomly play theme of instruments in party
            if ((int)Time.time % 8 == 0)
            {
                if (!already)
                {
                    inst.playInstTheme();
                    already = true;
                }
            }
            else
            {
                already = false;
            }
		}
	}

    private class WhileState : State
    {
		private int reps;
		private int iTheme;

        public WhileState(BGM inst) : base(inst)
        { 
            iTheme = Random.Range(0, inst.aWhile.Length);
			iAudio = Random.Range(0, inst.aWhile[iTheme].aClips.Length);
			reps = 0;
		}

        public override void update()
        {
            if (!inst.bgmASrc.isPlaying)
            {
                reps++;

				if (reps >= 3)
				{
					int oldITheme = iTheme;
					iTheme = Random.Range(0, inst.aWhile.Length);
					if (oldITheme != iTheme)
					{
						reps = 0;
					}					
				}

                iAudio = Random.Range(0, inst.aWhile[iTheme].aClips.Length);
                inst.bgmASrc.clip = inst.aWhile[iTheme].aClips[iAudio];

                inst.bgmASrc.volume = StaticSettings.volumeBGM;
                inst.bgmASrc.Play();
            }

			// randomly play theme of instruments in party
			if ((int)Time.time % 8 == 0)
			{
				if (!already)
				{
					inst.playInstTheme();
					already = true;
				}				
			}
			else
			{
				already = false;
			}
        }
    }

	/* --- Inspector */

	/* Chance of playing instruments theme. */
	public int instThemeChance;

	/* Array of audio clips to cycle through. */
	public AudioClip[] aStart;

    /* Array of audio clips to cycle through. */
    public AudioList[] aWhile;

	/* 0: drums, 1: guitar. */
	public AudioClip[] aFoundInst;

	/* 0: drums, 1: guitar. */
	public AudioList[] aInstTheme;

	/* Clip for when finding sheets. */
	public AudioClip aFoundSheet;


	/* --- Attributes --- */

	/* Audio Source that plays the bgm. */
	private AudioSource bgmASrc;

	/* Audio Source that plays when something is found. */
	private AudioSource foundASrc;

    /* Audio Source that plays drums theme. */
    private AudioSource drumsASrc;

	/* Audio Source that plays guitar theme. */
	private AudioSource guitarASrc;

	/* Self instance. */
	private static BGM self;
	
	/* Current state. */
	private State state;

	/* --- Methods --- */

	// Use this for initialization
	void Start () {
		self = this;
		AudioSource[] aSrcs = GetComponents<AudioSource>();
        bgmASrc = aSrcs[0];
		foundASrc = aSrcs[1];
		drumsASrc = aSrcs[2];
        guitarASrc = aSrcs[3];
        state = new StartState(this);
	}
	
	// Update is called once per frame
	void Update () {
		state.update();
	}

    public void updateBGMVolume(float volume)
    {
        state.updateVolume(volume);
    }

	private void playInstTheme()
	{
		// make sure playing this wont interfere with gameplay
		if (foundASrc.isPlaying)
		{
			return;
		}
		else
		{
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            GameObject[] spotlights = GameObject.FindGameObjectsWithTag("Spotlight");

            GameObject[] triggerables = new GameObject[enemies.Length + spotlights.Length];
            System.Array.Copy(enemies, triggerables, enemies.Length);
            System.Array.Copy(spotlights, 0, triggerables, enemies.Length, spotlights.Length);

            foreach (GameObject obj in triggerables)
            {
				if (!obj.GetComponent<Melody>().IsStopped())
				{
					return;
				}
				
			}
		}

		bool drums = false;
		bool guitar = false;
		
		foreach (GameObject inst in Movement.party)
		{
			if (inst.name == "PartyTambor")
			{
				drums = true;
			} else if (inst.name == "PartyGuitar")
			{
				guitar = true;
			}
		}

		if (!drumsASrc.isPlaying && drums && Random.Range(0, 100) >= (100 - instThemeChance))
		{
			drumsASrc.clip = aInstTheme[0].aClips[Random.Range(0, aInstTheme[0].aClips.Length)];
			drumsASrc.Play();
		}
        
		if (!guitarASrc.isPlaying && guitar && Random.Range(0, 100) >= (100 - instThemeChance))
        {
            guitarASrc.clip = aInstTheme[1].aClips[Random.Range(0, aInstTheme[1].aClips.Length)];
			guitarASrc.Play();
        }
	}

	// Play the instrument sound when found
	public static void FoundInst(string name)
	{
		AudioClip clip = null;

		switch (name)
		{
			case "PartyTambor":
				clip = self.aFoundInst[0];
				break;
            case "PartyGuitar":
                clip = self.aFoundInst[1];
                break;

			default:
				break;
		}

		self.foundASrc.clip = clip;
		self.foundASrc.Play();
	}

    // Play the sheet sound when found
    public static void FoundSheet()
    {
        self.foundASrc.clip = self.aFoundSheet;
        self.foundASrc.Play();
    }
}
