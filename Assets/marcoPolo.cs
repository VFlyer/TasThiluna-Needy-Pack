using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

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
    private bool bombSolved;
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
        bomb.OnBombExploded += delegate () { bombSolved = true; };
        bomb.OnBombSolved += delegate () { bombSolved = true; };
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
        Debug.LogFormat("[Marco Polo #{0}] The sound is coming from the {1}.", moduleId, directionIndex == 0 ? "left" : "right");
        Debug.LogFormat("[Marco Polo #{0}] The left button says {1} and the right button says {2}.", moduleId, buttonTexts[0].text, buttonTexts[1].text);
        Debug.LogFormat("[Marco Polo #{0}] The buttons are colored {1}.", moduleId, isBlue ? "blue" : "black");
        Debug.LogFormat("[Marco Polo #{0}] The correct button to press is the {1} button.", moduleId, solution == 0 ? "left" : "right");
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
        {
            Debug.LogFormat("[Marco Polo #{0}] Pressed the {1} button, which was incorrect. Strike!", moduleId, Array.IndexOf(buttons, button) == 0 ? "left" : "right");
            module.OnStrike();
        }
        else
        {
            Debug.LogFormat("[Marco Polo #{0}] Pressed the {1} button, which was correct. Module temporarily neutralized!", moduleId, Array.IndexOf(buttons, button) == 0 ? "left" : "right");
            OnNeedyDeactivation();
            module.OnPass();
        }
    }

    // Twitch Plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <pos> (#) [Presses button in position 'pos' (Optionally '#' times if it is the center button)] | Valid positions are left, right, and center";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length > 3)
            {
                yield return "sendtochaterror Too many parameters!";
            }
            else if (parameters.Length == 3)
            {
                if (parameters[1].EqualsIgnoreCase("center"))
                {
                    int temp = 0;
                    if (int.TryParse(parameters[2], out temp))
                    {
                        if (temp < 1 || temp > 10)
                        {
                            yield return "sendtochaterror The specified number of times to press the center button '" + parameters[2] + "' is out of range 1-10!";
                            yield break;
                        }
                        for (int i = 0; i < temp; i++)
                        {
                            soundButton.OnInteract();
                            yield return new WaitForSeconds(0.5f);
                        }
                    }
                    else
                    {
                        yield return "sendtochaterror The specified number of times to press the center button '" + parameters[2] + "' is invalid!";
                    }
                }
                else if (parameters[1].EqualsIgnoreCase("left") || parameters[1].EqualsIgnoreCase("right"))
                {
                    yield return "sendtochaterror Cannot press the '" + parameters[1] + "' button a certain number of times!";
                }
                else
                {
                    yield return "sendtochaterror Cannot press the '" + parameters[1] + "' button as it is invalid!";
                }
            }
            else if (parameters.Length == 2)
            {
                if (parameters[1].EqualsIgnoreCase("center"))
                {
                    soundButton.OnInteract();
                }
                else if (parameters[1].EqualsIgnoreCase("left"))
                {
                    buttons[0].OnInteract();
                }
                else if (parameters[1].EqualsIgnoreCase("right"))
                {
                    buttons[1].OnInteract();
                }
                else
                {
                    yield return "sendtochaterror Cannot press the '" + parameters[1] + "' button as it is invalid!";
                }
            }
            yield break;
        }
    }

    void TwitchHandleForcedSolve()
    {
        //The code is done in a coroutine instead of here so that if the solvebomb command was executed this will just input the number right when it activates and it wont wait for its turn in the queue
        StartCoroutine(HandleSolve());
    }

    IEnumerator HandleSolve()
    {
        while (!bombSolved)
        {
            while (!active) { yield return new WaitForSeconds(0.1f); }
            buttons[solution].OnInteract();
        }
    }
}
