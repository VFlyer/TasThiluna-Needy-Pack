using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class redLightGreenLight : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
	public KMNeedyModule module;

	public KMSelectable button;
	public Light[] lights;
	public TextMesh buttonText;

	private bool active;
	private int currentTime;
	private static readonly string[] yellowTimes = new string[5] { "5", "4", "3", "2", "1" };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    void Awake()
    {
    	moduleId = moduleIdCounter++;
		module.OnNeedyActivation += OnNeedyActivation;
		module.OnNeedyDeactivation += OnNeedyDeactivation;
		module.OnTimerExpired += OnTimerExpired;
		button.OnInteract += delegate () { ButtonPress(); return false; };
    }

    void Start()
    {
		Debug.LogFormat("[Red Light Green Light #{0}] Needy initiated.", moduleId);
		foreach (Light l in lights)
			l.gameObject.SetActive(false);
		buttonText.text = "";
    }

	protected void OnNeedyActivation()
	{
		active = true;
		StartCoroutine(PassingTime());
		buttonText.text = "wait...";
	}

	protected void OnNeedyDeactivation()
	{
		active = false;
		buttonText.text = "";
		foreach (Light l in lights)
			l.gameObject.SetActive(false);
	}

	protected void OnTimerExpired()
	{
		if (active)
		{
			module.OnStrike();
			OnNeedyDeactivation();
		}
	}

	void ButtonPress()
	{
		if (!active)
			return;
		button.AddInteractionPunch(.5f);
		audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
		var timeNow = Mathf.Round(module.GetNeedyTimeRemaining()).ToString();
		if (timeNow != "0")
			module.OnStrike();
		else
		{
			OnNeedyDeactivation();
			module.OnPass();
		}
	}

	IEnumerator PassingTime()
	{
		while (active)
		{
			var timeNow = Mathf.Round(module.GetNeedyTimeRemaining()).ToString();
			if (yellowTimes.Contains(timeNow))
			{
				lights[0].gameObject.SetActive(false);
				lights[1].gameObject.SetActive(true);
				lights[2].gameObject.SetActive(false);
			}
			else if (timeNow == "0")
			{
				lights[0].gameObject.SetActive(false);
				lights[1].gameObject.SetActive(false);
				lights[2].gameObject.SetActive(true);
				buttonText.text = "go!";
			}
			else
			{
				lights[0].gameObject.SetActive(true);
				lights[1].gameObject.SetActive(false);
				lights[2].gameObject.SetActive(false);
			}
			yield return null;
		}
	}
}
