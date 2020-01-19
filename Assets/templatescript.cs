using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class templateScript : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
	public KMNeedyModule module;

	private bool active;

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    void Awake()
    {
    	moduleId = moduleIdCounter++;
		module.OnNeedyActivation += OnNeedyActivation;
		module.OnNeedyDeactivation += OnNeedyDeactivation;
		module.OnTimerExpired += OnTimerExpired;
    }

    void Start()
    {
		Debug.LogFormat("[Module name #{0}] Needy initiated.", moduleId);
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
}
