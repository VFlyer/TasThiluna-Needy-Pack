using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class rotatingSquares : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
	public KMNeedyModule module;

	public KMSelectable[] buttons;
	public Renderer[] buttonRenders;
	public Transform platform;
	public Color defaultColor;
	public Color resetColor;

	private bool active;
	private bool[] pressed = new bool[16];
	private Vector3[] rotations = new Vector3[4] { new Vector3(0f, 0f, 0f), new Vector3(0f, 90f, 0), new Vector3(0f, 180f, 0f), new Vector3(0f, 270f, 0f) };
	private float[] lengths = new float[5] { 1f, 1.5f, 2f, 2.5f, 3f };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    void Awake()
    {
    	moduleId = moduleIdCounter++;
		module.OnNeedyActivation += OnNeedyActivation;
		module.OnNeedyDeactivation += OnNeedyDeactivation;
		module.OnTimerExpired += OnTimerExpired;
		foreach (KMSelectable button in buttons)
			button.OnInteract += delegate () { ButtonPress(button); return false; };
    }

    void Start()
    {
		Debug.LogFormat("[Rotating Squares #{0}] Needy initiated.", moduleId);
    }

	protected void OnNeedyActivation()
	{
		active = true;
	}

	protected void OnNeedyDeactivation()
	{
		active = false;
	}

	protected void OnTimerExpired()
	{
		if (active)
		{
			module.OnStrike();
			OnNeedyDeactivation();
		}
	}

	void ButtonPress(KMSelectable button)
	{
		if (!active)
			return;
		button.AddInteractionPunch(.5f);
		audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
		if (!pressed[Array.IndexOf(buttons, button)])
		{
			pressed[Array.IndexOf(buttons, button)] = true;
			OnNeedyDeactivation();
			module.OnPass();
			StartCoroutine(Rotate());
		}
		else
			module.OnStrike();
		if (!pressed.Any(b => !b))
		{
			for (int i = 0; i < 16; i++)
				pressed[i] = false;
			audio.PlaySoundAtTransform("jingle", platform);
			foreach (Renderer buttonR in buttonRenders)
				StartCoroutine(Reset(buttonR));
		}
	}

	IEnumerator Reset(Renderer button)
	{
		var elapsed = 0f;
		var halfWay = .5f;
		var duration = 1f;
		while (elapsed <= halfWay)
		{
			button.material.color = new Color(
				Mathf.Lerp(defaultColor.r, resetColor.r, elapsed / duration),
				Mathf.Lerp(defaultColor.g, resetColor.g, elapsed / duration),
				Mathf.Lerp(defaultColor.b, resetColor.b, elapsed / duration));
			yield return null;
			elapsed += Time.deltaTime;
		}
		yield return new WaitForSeconds(1.5f);
		while (elapsed < duration)
		{
			button.material.color = new Color(
				Mathf.Lerp(resetColor.r, defaultColor.r, elapsed / duration),
				Mathf.Lerp(resetColor.g, defaultColor.g, elapsed / duration),
				Mathf.Lerp(resetColor.b, defaultColor.b, elapsed / duration));
			yield return null;
			elapsed += Time.deltaTime;
		}
	}

	IEnumerator Rotate()
	{
		var currentRotation = platform.localEulerAngles;
		var nextRotation = rotations.Where(r => r != currentRotation).PickRandom();
		var elapsed = 0f;
		var duration = lengths.PickRandom();
		while (elapsed < duration)
		{
			platform.localEulerAngles = new Vector3(
				Easing.InOutSine(elapsed, currentRotation.x, nextRotation.x, duration),
				Easing.InOutSine(elapsed, currentRotation.y, nextRotation.y, duration),
				Easing.InOutSine(elapsed, currentRotation.z, nextRotation.z, duration));
			yield return null;
			elapsed += Time.deltaTime;
		}

	}
}
