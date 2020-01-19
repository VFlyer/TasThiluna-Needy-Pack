using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class boozleglyphIdentification : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
	public KMNeedyModule module;

	public KMSelectable submit;
	public KMSelectable[] arrowButtons;
	public TextMesh screenText;
	public Renderer screen;
	public Texture none;
	public Texture[] set1;
	public Texture[] set2;
	public Texture[] set3;
	private static List<Texture[]> allTextures = new List<Texture[]>();
	private static readonly float[] rotations = new float[4] { 0f, 90f, 180f, 270f };

	private bool active;
	private int stage;
	private int letterIndex;
	private int setIndex;
	private int rotationIndex;
	private int chosenLetter;
	private int chosenSet;
	private int chosenRotation;
	private static readonly string[] alphabet = new string[26] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
	private static readonly string[] setNumbers = new string[3] { "1", "2", "3" };
	private static readonly string[] rotationNames = new string[4] { "0", "1", "2", "3" };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    void Awake()
    {
    	moduleId = moduleIdCounter++;
		module.OnNeedyActivation += OnNeedyActivation;
		module.OnNeedyDeactivation += OnNeedyDeactivation;
		module.OnTimerExpired += OnTimerExpired;
		submit.OnInteract += delegate () { Submit(); return false; };
		foreach (KMSelectable button in arrowButtons)
			button.OnInteract += delegate () { PressArrowButton(button); return false; };
    }

    void Start()
    {
		Debug.LogFormat("[Boozleglyph Identification #{0}] Needy initiated.", moduleId);
		screenText.text = "";
		allTextures = new List<Texture[]> { set1, set2, set3 };
    }

	protected void OnNeedyActivation()
	{
		active = true;
		screenText.text = "A";
		letterIndex = rnd.Range(0,26);
		setIndex = rnd.Range(0,3);
		rotationIndex = rnd.Range(0,4);
		screen.transform.localEulerAngles = new Vector3(90f, rotations[rotationIndex], 0f);
		screen.material.mainTexture = allTextures[setIndex][letterIndex];
	}

	protected void OnNeedyDeactivation()
	{
		active = false;
		screen.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
		screen.material.mainTexture = none;
		screenText.text = "";
		chosenLetter = 0;
		chosenSet = 0;
		chosenRotation = 0;
		stage = 0;
	}

	protected void OnTimerExpired()
	{
		if (active)
		{
			module.OnStrike();
			OnNeedyDeactivation();
		}
	}

	void Submit()
	{
		if (!active)
			return;
		if (stage == 0)
		{
			if (chosenLetter != letterIndex)
				module.OnStrike();
			else
			{
				stage++;
				UpdateScreen();
			}
		}
		else if (stage == 1)
		{
			if (chosenSet != setIndex)
				module.OnStrike();
			else
			{
				stage++;
				UpdateScreen();
			}
		}
		else
		{
			if (chosenRotation != rotationIndex)
				module.OnStrike();
			else
			{
				module.OnPass();
				OnNeedyDeactivation();
			}
		}
	}

	void PressArrowButton(KMSelectable button)
	{
		if (!active)
			return;
		button.AddInteractionPunch(.5f);
		audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
		int ix = Array.IndexOf(arrowButtons, button);
		int[] offsets = new int[2] { -1, 1 };
		chosenLetter += offsets[ix];
		chosenSet += offsets[ix];
		chosenRotation += offsets[ix];
		if (chosenLetter == -1)
			chosenLetter = 25;
		if (chosenSet == -1)
			chosenSet = 2;
		if (chosenRotation == -1)
			chosenRotation = 3;
		chosenLetter %= 26;
		chosenSet %= 3;
		chosenRotation %= 4;
		UpdateScreen();
	}

	void UpdateScreen()
	{
		if (stage == 0)
			screenText.text = alphabet[chosenLetter];
		else if (stage == 1)
			screenText.text = setNumbers[chosenSet];
		else
			screenText.text = rotationNames[chosenRotation];
	}
}
