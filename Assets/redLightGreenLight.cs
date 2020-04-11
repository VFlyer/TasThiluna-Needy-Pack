using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class redLightGreenLight : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMNeedyModule module;

    public KMSelectable button;
    public Light[] lights;
    public TextMesh buttonText;

    private bool active;
    private bool bombSolved;
    private static readonly string[] yellowTimes = new string[5] { "5", "4", "3", "2", "1" };

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
            Debug.LogFormat("[Red Light Green Light #{0}] The button was not pressed in time, strike!", moduleId);
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
        {
            Debug.LogFormat("[Red Light Green Light #{0}] Pressed the button with {1} seconds remaining, that is incorrect. Strike!", moduleId, timeNow);
            module.OnStrike();
        }
        else
        {
            Debug.LogFormat("[Red Light Green Light #{0}] Pressed the button with 0 seconds remaining, that is correct. Module temporarily neutralized!", moduleId);
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

    // Twitch Plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press at <num> [Presses the button when the number of seconds remaining on the needy's timer is 'num']";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLower();
        string[] parameters = command.Split(' ');
        if (command.StartsWith("press at "))
        {
            yield return null;
            if (parameters.Length > 3)
            {
                yield return "sendtochaterror Too many parameters!";
            }
            else if (parameters.Length == 3)
            {
                int temp = 0;
                if (int.TryParse(parameters[2], out temp))
                {
                    if (temp < 0 || temp > 40)
                    {
                        yield return "sendtochaterror The specified time to press the button at '" + parameters[2] + "' is out of range 0-40!";
                        yield break;
                    }
                    int temp2 = int.Parse(Mathf.Round(module.GetNeedyTimeRemaining()).ToString());
                    if (temp2 < temp)
                    {
                        yield return "sendtochaterror The specified time to press the button at '" + parameters[2] + "' has already passed!";
                        yield break;
                    }
                    while (int.Parse(Mathf.Round(module.GetNeedyTimeRemaining()).ToString()) != temp) { yield return "trycancel Halted waiting for the button press due to a request to cancel!"; yield return new WaitForSeconds(0.1f); }
                    button.OnInteract();
                }
                else
                {
                    yield return "sendtochaterror The specified time to press the button at '" + parameters[2] + "' is invalid!";
                }
            }
            else if (parameters.Length == 2)
            {
                yield return "sendtochaterror Please specify the time to press the button at!";
            }
            else if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify 'at' and the time to press the button at!";
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
            while (int.Parse(Mathf.Round(module.GetNeedyTimeRemaining()).ToString()) != 0) { yield return new WaitForSeconds(0.1f); }
            button.OnInteract();
        }
    }
}
