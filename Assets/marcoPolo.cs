using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class marcoPolo : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMNeedyModule module;

    public KMSelectable soundButton;
    public KMSelectable[] buttons;
    public TextMesh[] buttonTexts;
    public Color[] textColors;
    public Transform[] positions;

    private bool active;
    private bool isBlue;
    private bool swapped;
    private int directionIndex;
    private int solution;
    private static readonly string[] labels = new string[2] { "L", "R" };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        module.OnNeedyActivation += OnNeedyActivation;
        module.OnNeedyDeactivation += OnNeedyDeactivation;
        module.OnTimerExpired += OnTimerExpired;
        soundButton.OnInteract += delegate () { PressSoundButton(); return false; };
        foreach (KMSelectable button in buttons)
            button.OnInteract += delegate () { PressButton(button); return false; };
    }

    void Start()
    {
        Debug.LogFormat("[Marco Polo #{0}] Needy initiated.", moduleId);
        foreach (TextMesh t in buttonTexts)
            t.text = "";
    }

    protected void OnNeedyActivation()
    {
        active = true;
        isBlue = rnd.Range(0, 2) == 0;
        swapped = rnd.Range(0, 2) == 0;
        directionIndex = rnd.Range(0, 2);
        if (directionIndex == 0)
        {
            if (isBlue)
            {
                if (swapped)
                    solution = 1;
                else
                    solution = 0;
            }
            else
            {
                solution = 0;
            }
        }
        else
        {
            if (isBlue)
            {
                if (swapped)
                    solution = 0;
                else
                    solution = 1;
            }
            else
            {
                solution = 1;
            }
        }
        foreach (TextMesh t in buttonTexts)
        {
            t.color = !isBlue ? textColors[0] : textColors[1];
            t.text = !swapped ? labels[Array.IndexOf(buttonTexts, t)] : labels[Enumerable.Range(0, 2).Where(x => x != Array.IndexOf(buttonTexts, t)).First()];
        }
    }

    protected void OnNeedyDeactivation()
    {
        active = false;
        foreach (TextMesh t in buttonTexts)
            t.text = "";
    }

    protected void OnTimerExpired()
    {
        if (active)
        {
            module.OnStrike();
            OnNeedyDeactivation();
        }
    }

    void PressSoundButton()
    {
        if (!active)
            return;
        soundButton.AddInteractionPunch(.5f);
        audio.PlaySoundAtTransform("beep", positions[directionIndex]);
    }

    void PressButton(KMSelectable button)
    {
        if (!active)
            return;
        button.AddInteractionPunch(.5f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        if (Array.IndexOf(buttons, button) != solution)
            module.OnStrike();
        else
        {
            OnNeedyDeactivation();
            module.OnPass();
        }
    }
}
