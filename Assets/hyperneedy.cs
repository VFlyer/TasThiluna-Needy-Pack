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
	private static readonly string[] rotationNames = new string[12] { "XY", "YX", "XZ", "ZX", "XW", "WX", "YZ", "ZY", "YW", "WY", "ZW", "WZ" };

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
		//var usedColors = discColors.ToList().Shuffle();
		for (int i = 0; i < 16; i++)
		{
			//allDiscRenders[i].material.color = usedColors[i];
			defaultPositions[i] = allDiscs[i].localPosition;
			allDiscs[i].localPosition = new Vector3(0f, 0f, 0f);
		}
    }

	protected void OnNeedyActivation()
	{
		active = true;
		rotationIndex = rnd.Range(0,12);
		var vertices = GetUnrotatedVertices();
		for (int i = 0; i < 16; i++)
			StartCoroutine(Move(allDiscs[i], new Vector3(0f, 0f, 0f), vertices[i].Project(), true));
	}

	protected void OnNeedyDeactivation()
	{
		active = false;
		var vertices = GetUnrotatedVertices();
		for (int i = 0; i < 16; i++)
			StartCoroutine(Move(allDiscs[i], vertices[i].Project(), new Vector3(0f, 0f, 0f), false));
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
		if (animating || !discsOut)
			return;
		StartCoroutine(Rotation());
	}

	IEnumerator Move(Transform disc, Vector3 startPosition, Vector3 endPosition, bool next)
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

	/*IEnumerator Rotation(Transform disc, int ix, int rotationIx)
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
	}*/

	IEnumerator Rotation()
	{
		var unrotatedVertices = GetUnrotatedVertices();
        SetHypercube(unrotatedVertices.Select(v => v.Project()).ToArray());
        var axis1 = "XYZW".IndexOf(rotationNames[rotationIndex][0]);
        var axis2 = "XYZW".IndexOf(rotationNames[rotationIndex][1]);
        var duration = 2f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            var angle = Easing.InOutQuad(elapsed, 0, Mathf.PI / 2, duration);
            var matrix = new double[16];
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    matrix[i + 4 * j] =
                        i == axis1 && j == axis1 ? Mathf.Cos(angle) :
                        i == axis1 && j == axis2 ? Mathf.Sin(angle) :
                        i == axis2 && j == axis1 ? -Mathf.Sin(angle) :
                        i == axis2 && j == axis2 ? Mathf.Cos(angle) :
                        i == j ? 1 : 0;

            SetHypercube(unrotatedVertices.Select(v => (v * matrix).Project()).ToArray());

            yield return null;
            elapsed += Time.deltaTime;
        }
        SetHypercube(unrotatedVertices.Select(v => v.Project()).ToArray());
	}

	void SetHypercube(Vector3[] vertices)
	{
		for (int i = 0; i < 16; i++)
			allDiscs[i].localPosition = vertices[i];
	}

	private Point4D[] GetUnrotatedVertices()
   	{
		return Enumerable.Range(0, 1 << 4).Select(i => new Point4D((i & 1) != 0 ? 1 : -1, (i & 2) != 0 ? 1 : -1, (i & 4) != 0 ? 1 : -1, (i & 8) != 0 ? 1 : -1)).ToArray();
   	}

}
