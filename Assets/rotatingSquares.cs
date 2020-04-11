using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

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
    private bool bombSolved;
    private bool[] pressed = new bool[16];
    private Vector3[] rotations = new Vector3[4] { new Vector3(0f, 0f, 0f), new Vector3(0f, 90f, 0), new Vector3(0f, 180f, 0f), new Vector3(0f, 270f, 0f) };
    private Vector3 currentRot = new Vector3(0f, 0f, 0f);
    private float[] lengths = new float[5] { 1f, 1.5f, 2f, 2.5f, 3f };

    private static int moduleIdCounter = 1;
    private int moduleId;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        module.OnNeedyActivation += OnNeedyActivation;
        module.OnNeedyDeactivation += OnNeedyDeactivation;
        module.OnTimerExpired += OnTimerExpired;
        bomb.OnBombExploded += delegate () { bombSolved = true; };
        bomb.OnBombSolved += delegate () { bombSolved = true; };
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
            Debug.LogFormat("[Rotating Squares #{0}] No buttons were pressed in time, strike!", moduleId);
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
            Debug.LogFormat("[Rotating Squares #{0}] Pressed button {1}, which has not been pressed yet. Module temporarily neutralized!", moduleId, Array.IndexOf(buttons, button) + 1);
            pressed[Array.IndexOf(buttons, button)] = true;
            OnNeedyDeactivation();
            module.OnPass();
            StartCoroutine(Rotate());
        }
        else
        {
            Debug.LogFormat("[Rotating Squares #{0}] Pressed button {1}, which has been pressed already. Strike!", moduleId, Array.IndexOf(buttons, button) + 1);
            module.OnStrike();
        }
        if (!pressed.Any(b => !b))
        {
            Debug.LogFormat("[Rotating Squares #{0}] All buttons 16 buttons have been pressed! All buttons returning to unused stage...", moduleId);
            for (int i = 0; i < 16; i++)
                pressed[i] = false;
            audio.PlaySoundAtTransform("jingle", transform);
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
        currentRot = nextRotation;
        Debug.LogFormat("[Rotating Squares #{0}] The platform has been rotated by {1} degrees!", moduleId, Math.Round(nextRotation.y - currentRotation.y));
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

    // Twitch Plays
    private int[][] buttonsinrot = { new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, new int[] { 12, 8, 4, 0, 13, 9, 5, 1, 14, 10, 6, 2, 15, 11, 7, 3 }, new int[] { 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 }, new int[] { 3, 7, 11, 15, 2, 6, 10, 14, 1, 5, 9, 13, 0, 4, 8, 12 } };

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <btn> [Presses button 'btn' in reading order] | Valid buttons are 1-16";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length > 2)
            {
                yield return "sendtochaterror Too many parameters!";
            }
            else if (parameters.Length == 2)
            {
                int temp = 0;
                if (int.TryParse(parameters[1], out temp))
                {
                    if (temp < 1 || temp > 16)
                    {
                        yield return "sendtochaterror The specified button to press '" + parameters[1] + "' is out of range 1-16!";
                        yield break;
                    }
                    buttons[buttonsinrot[Array.IndexOf(rotations, currentRot)][temp - 1]].OnInteract();
                }
                else
                {
                    yield return "sendtochaterror The specified button to press '" + parameters[1] + "' is invalid!";
                }
            }
            else if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify the button to press!";
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
            List<int> unpressed = new List<int>();
            for (int i = 0; i < 16; i++)
            {
                if (!pressed[i])
                {
                    unpressed.Add(i);
                }
            }
            buttons[unpressed.PickRandom()].OnInteract();
        }
    }
}
