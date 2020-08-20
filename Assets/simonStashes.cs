using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class simonStashes : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMNeedyModule module;

    public KMSelectable centerButton;
    public KMSelectable[] buttons;
    public Renderer[] buttonRenders;
    public Color[] colors;
    public Color[] litColors;
    public Color gray;
    public Color litGray;
    public Light centerLight;
    public Light[] buttonLights;

    private int color1;
    private int color2;
    private int[] colorsPresent;
    private int[] base36Values = new int[6];
    private int result;
    private string binary;
    private bool[] selected = new bool[4];

    private static readonly string base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static readonly string[] colorNames = { "red", "green", "blue", "cyan", "magenta", "yellow" };
    private int pressedCount;
    private bool centerPressed;
    private bool active;
    private bool animating;
    private Coroutine flashing;

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;
    private bool bombSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        module.OnNeedyActivation += OnNeedyActivation;
        module.OnNeedyDeactivation += OnNeedyDeactivation;
        module.OnTimerExpired += OnTimerExpired;
        bomb.OnBombExploded += delegate () { bombSolved = true; };
        bomb.OnBombSolved += delegate () { bombSolved = true; };
        centerButton.OnInteract += delegate () { PressButton(); return false; };
        foreach (KMSelectable button in buttons)
            button.OnInteract += delegate () { PressButton(button); return false; };
    }

    void Start()
    {
        Debug.LogFormat("[Simon Stashes #{0}] Needy initiated.", moduleId);
        module.SetResetDelayTime(60f, 90f);
        for (int i = 0; i < 6; i++)
            base36Values[i] = base36.IndexOf(bomb.GetSerialNumber()[i]);
        float scalar = transform.lossyScale.x;
        foreach (Light l in buttonLights)
        {
            l.enabled = false;
            l.range *= scalar;
        }
        centerLight.enabled = false;
        centerLight.range *= scalar;
        centerButton.GetComponent<Renderer>().material.color = gray;
    }

    protected void OnNeedyActivation()
    {
        active = true;
        colorsPresent = Enumerable.Range(0, 6).ToList().Shuffle().Take(4).ToArray();
        color1 = rnd.Range(0, 4);
        color2 = rnd.Range(0, 4);
        Debug.LogFormat("[Simon Stashes #{0}] Needy activated!", moduleId);
        Debug.LogFormat("[Simon Stashes #{0}] The sequence is {1}, {2}.", moduleId, colorNames[colorsPresent[color1]], colorNames[colorsPresent[color2]]);
        StartCoroutine(ShowColors());
        switch (colorsPresent[color2])
        {
            case 0:
                result = (base36Values[colorsPresent[color1]] * bomb.GetBatteryCount()) % 16;
                break;
            case 1:
                result = (base36Values[colorsPresent[color1]] * bomb.GetBatteryHolderCount()) % 16;
                break;
            case 2:
                result = (base36Values[colorsPresent[color1]] * bomb.GetIndicators().Count()) % 16;
                break;
            case 3:
                result = (base36Values[colorsPresent[color1]] * bomb.GetPortCount()) % 16;
                break;
            case 4:
                result = (base36Values[colorsPresent[color1]] * bomb.GetPortPlates().Count()) % 16;
                break;
            default:
                result = (base36Values[colorsPresent[color1]] * bomb.GetSerialNumberNumbers().Sum()) % 16;
                break;
        }
        binary = Convert.ToString(result, 2).PadLeft(4, '0');
        Debug.LogFormat("[Simon Stashes #{0}] The result of the math is {1}, and the corresponding binary is {2}.", moduleId, result, binary);
    }

    IEnumerator ShowColors()
    {
        for (int i = 0; i < 4; i++)
        {
            if (active)
                StartCoroutine(Fade(buttonRenders[i], gray, colors[colorsPresent[i]]));
            else
                StartCoroutine(Fade(buttonRenders[i], colors[colorsPresent[i]], gray));
            yield return new WaitForSeconds(.25f);
        }
    }

    IEnumerator Fade(Renderer button, Color start, Color end)
    {
        animating = true;
        var elapsed = 0f;
        var duration = .5f;
        while (elapsed < duration)
        {
            button.material.color = new Color(
                Mathf.Lerp(start.r, end.r, elapsed / duration),
                Mathf.Lerp(start.g, end.g, elapsed / duration),
                Mathf.Lerp(start.b, end.b, elapsed / duration)
            );
            yield return null;
            elapsed += Time.deltaTime;
        }
        button.material.color = end;
        animating = false;
        yield return new WaitForSeconds(.75f);
        if (active && Array.IndexOf(buttonRenders, button) == 3)
            flashing = StartCoroutine(FlashSequence());
    }

    IEnumerator FlashSequence()
    {
        autosolveInteract = true;
        resetSequence:
        centerLight.enabled = true;
        centerButton.GetComponent<Renderer>().material.color = litGray;
        yield return new WaitForSeconds(.4f);
        centerLight.enabled = false;
        centerButton.GetComponent<Renderer>().material.color = gray;
        yield return new WaitForSeconds(.4f);
        buttonLights[color1].enabled = true;
        buttonRenders[color1].material.color = litColors[colorsPresent[color1]];
        yield return new WaitForSeconds(.4f);
        buttonLights[color1].enabled = false;
        buttonRenders[color1].material.color = colors[colorsPresent[color1]];
        yield return new WaitForSeconds(.4f);
        buttonLights[color2].enabled = true;
        buttonRenders[color2].material.color = litColors[colorsPresent[color2]];
        yield return new WaitForSeconds(.4f);
        buttonLights[color2].enabled = false;
        buttonRenders[color2].material.color = colors[colorsPresent[color2]];
        yield return new WaitForSeconds(.4f);
        goto resetSequence;
    }

    protected void OnNeedyDeactivation()
    {
        active = false;
        if (flashing != null)
        {
            StopCoroutine(flashing);
            flashing = null;
        }
        centerLight.enabled = false;
        centerButton.GetComponent<Renderer>().material.color = gray;
        for (int i = 0; i < 4; i++)
        {
            selected[i] = false;
            buttonLights[i].enabled = false;
            buttonRenders[i].material.color = colors[colorsPresent[i]];
        }
        StartCoroutine(ShowColors());
        pressedCount = 0;
        centerPressed = false;
    }

    protected void OnTimerExpired()
    {
        if (active)
        {
            module.OnStrike();
            OnNeedyDeactivation();
        }
    }

    void PressButton()
    {
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, centerButton.transform);
        if (!active || animating)
            return;
        if (!centerPressed)
        {
            centerPressed = true;
            if (flashing != null)
            {
                StopCoroutine(flashing);
                flashing = null;
            }
            for (int i = 0; i < 4; i++)
            {
                buttonLights[i].enabled = false;
                buttonRenders[i].material.color = colors[colorsPresent[i]];
            }
            centerLight.enabled = true;
            centerButton.GetComponent<Renderer>().material.color = litGray;
        }
        else
        {
            centerPressed = false;
            var correct = true;
            for (int i = 0; i < 4; i++)
                if (binary[i] == '1' ? !selected[i] : selected[i])
                    correct = false;
            StartCoroutine(Submit(correct));
        }
    }

    IEnumerator Submit(bool correct)
    {
        audio.PlaySoundAtTransform("InputCheck", centerButton.transform);
        autosolveInteract = false;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                buttonLights[j].enabled = true;
                buttonRenders[j].material.color = litColors[colorsPresent[j]];
                yield return new WaitForSeconds(.125f);
                buttonLights[j].enabled = false;
                buttonRenders[j].material.color = colors[colorsPresent[j]];
            }
        }
        for (int i = 0; i < 4; i++)
        {
            buttonLights[i].enabled = false;
            buttonRenders[i].material.color = colors[colorsPresent[i]];
        }
        if (!correct)
        {
            module.OnStrike();
            module.OnPass();
        }
        else
        {
            module.OnPass();
            yield return new WaitForSeconds(.1f);
            audio.PlaySoundAtTransform("InputCorrect", centerButton.transform);
        }
        OnNeedyDeactivation();
    }

    void PressButton(KMSelectable button)
    {
        if (!active || !centerPressed || animating || selected[Array.IndexOf(buttons, button)])
            return;
        var ix = Array.IndexOf(buttons, button);
        centerLight.enabled = false;
        centerButton.GetComponent<Renderer>().material.color = gray;
        selected[ix] = true;
        buttonLights[ix].enabled = true;
        buttonRenders[ix].material.color = litColors[colorsPresent[ix]];
        pressedCount++;
        audio.PlaySoundAtTransform("ButtonPress" + pressedCount, button.transform);
    }

    // Twitch Plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} <1/2/3/4> [Presses those colored buttons, starting from the top-right and counting clockwise. Can be chained, i.e. ''!{0} press 134'.] !{0} center [Presses the gray button.]";
    private bool autosolveInteract;
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string cmd)
    {
        if (cmd.Trim().All(x => "1234".Contains(x)))
        {
            yield return null;
            for (int i = 0; i < cmd.Length; i++)
            {
                buttons[Int32.Parse(cmd[i].ToString()) - 1].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
        else if (new string[] { "c", "gray", "grey", "center", "centre", "middle", "submit", "enter" }.Contains(cmd.Trim().ToLowerInvariant()))
        {
            yield return null;
            centerButton.OnInteract();
        }
        else
            yield break;
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
            while (!autosolveInteract)
                yield return new WaitForSeconds(.1f);
            centerButton.OnInteract();
            yield return new WaitForSeconds(.1f);
            for (int i = 0; i < 4; i++)
            {
                if ((binary[i] == '1' && !selected[i]) || (binary[i] != '1' && selected[i]))
                {
                    buttons[i].OnInteract();
                    yield return new WaitForSeconds(.1f);
                }
            }
            yield return new WaitForSeconds(.1f);
            centerButton.OnInteract();
        }
    }
}
