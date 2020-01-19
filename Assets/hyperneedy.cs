using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class hyperneedy : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
	public KMNeedyModule module;

	public KMSelectable button;
	public Transform[] allDiscs;
	public Renderer[] allDiscRenders;
	public Color[] discColors;

	private bool active;
	private bool animating;
	private bool discsOut;
	private int rotationIndex;
	private Vector3[] defaultPositions = new Vector3[16];
	
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
		Debug.LogFormat("[Hyperneedy #{0}] Needy initiated.", moduleId);
		rotationIndex = 0; // TEMPORARY
		var usedColors = discColors.ToList().Shuffle();
		for (int i = 0; i < 16; i++)
		{
			allDiscRenders[i].material.color = usedColors[i];
			defaultPositions[i] = allDiscs[i].localPosition;
			allDiscs[i].localPosition = new Vector3(0f, 0f, 0f);
		}
    }

	protected void OnNeedyActivation()
	{
		active = true;
		//rotationIndex = rnd.Range(0,12);
		rotationIndex = 0; // TEMPORARY
		foreach (Transform disc in allDiscs)
			StartCoroutine(Move(disc, Array.IndexOf(allDiscs, disc), new Vector3 (0f, 0f, 0f), defaultPositions[Array.IndexOf(allDiscs, disc)], true));
	}

	protected void OnNeedyDeactivation()
	{
		active = false;
		foreach (Transform disc in allDiscs)
			StartCoroutine(Move(disc, Array.IndexOf(allDiscs, disc), disc.localPosition, new Vector3 (0f, 0f, 0f), false));
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
		if (animating)
			return;
		if (!discsOut)
			foreach (Transform disc in allDiscs)
				StartCoroutine(Move(disc, Array.IndexOf(allDiscs, disc), new Vector3 (0f, 0f, 0f), defaultPositions[Array.IndexOf(allDiscs, disc)], true));
		else
			foreach (Transform disc in allDiscs)
				StartCoroutine(Rotation(disc, Array.IndexOf(allDiscs, disc), rotationIndex));
	}

	IEnumerator Move(Transform disc, int ix, Vector3 startPosition, Vector3 endPosition, bool next)
	{
		animating = true;
		var elapsed = 0f;
		var duration = 1f;
		while (elapsed < duration)
		{
			disc.localPosition = new Vector3(
				Easing.InOutSine(elapsed, startPosition.x, endPosition.x, duration),
				Easing.InOutSine(elapsed, startPosition.y, endPosition.y, duration),
				Easing.InOutSine(elapsed, startPosition.z, endPosition.z, duration));
			yield return null;
			elapsed += Time.deltaTime;
		}
		discsOut = next;
		animating = false;
	}

	IEnumerator Rotation(Transform disc, int ix, int rotationIx)
	{
		yield return null;
		int currentPosition = Array.IndexOf(defaultPositions, disc);
		bool left = currentPosition % 2 == 0;
		bool bottom = currentPosition / 4 == 0 || currentPosition / 4 == 2;
		bool back = currentPosition % 4 == 0 || currentPosition % 4 == 1;
		bool zig = currentPosition / 4 == 0 || currentPosition / 4 == 1;
		bool inverse = rotationIx % 2 != 0;
		bool involvesW = rotationIx >= 6;
		float startX = disc.localPosition.x;
		float startY = disc.localPosition.y;
		float startZ = disc.localPosition.z;
	}
}
