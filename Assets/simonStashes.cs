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
    public Material[] usedMats;
    public Color[] colors;
    public Color[] litColors;
    public Color gray;
    public Color litGray;
    public Light centerLight;
    public Light[] buttonLights;
    public TextMesh colorblindText;

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

    private void Awake()
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
        colorblindText.gameObject.SetActive(GetComponent<KMColorblindMode>().ColorblindModeActive);
    }

    private void Start()
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
        var colorString = "RGBCMY";
        colorblindText.text = string.Format("{0}{1}\n{2}{3}", colorString[colorsPresent[3]], colorString[colorsPresent[0]], colorString[colorsPresent[1]], colorString[colorsPresent[2]]);
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

    private IEnumerator ShowColors()
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

    private IEnumerator Fade(Renderer button, Color start, Color end)
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

    private IEnumerator FlashSequence()
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
        buttonRenders[color1].material = usedMats[1];
        buttonRenders[color1].material.color = litColors[colorsPresent[color1]];
        yield return new WaitForSeconds(.4f);
        buttonLights[color1].enabled = false;
        buttonRenders[color1].material = usedMats[0];
        buttonRenders[color1].material.color = colors[colorsPresent[color1]];
        yield return new WaitForSeconds(.4f);
        buttonLights[color2].enabled = true;
        buttonRenders[color2].material = usedMats[1];
        buttonRenders[color2].material.color = litColors[colorsPresent[color2]];
        yield return new WaitForSeconds(.4f);
        buttonLights[color2].enabled = false;
        buttonRenders[color2].material = usedMats[0];
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
            buttonRenders[color1].material = usedMats[0];
            buttonRenders[color2].material = usedMats[0];
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
        colorblindText.text = "";
        pressedCount = 0;
        centerPressed = false;
    }

    protected void OnTimerExpired()
    {
        if (active)
        {
            module.HandleStrike();
            OnNeedyDeactivation();
        }
    }

    private void PressButton()
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
                buttonRenders[color1].material = usedMats[0];
                buttonRenders[color2].material = usedMats[0];
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

    private IEnumerator Submit(bool correct)
    {
        active = false;
        audio.PlaySoundAtTransform("InputCheck", centerButton.transform);
        autosolveInteract = false;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                buttonLights[j].enabled = true;
                buttonRenders[j].material = usedMats[1];
                buttonRenders[j].material.color = litColors[colorsPresent[j]];
                yield return new WaitForSeconds(.125f);
                buttonRenders[j].material = usedMats[0];
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
            Debug.LogFormat("[Simon Stashes #{0}] Submitted incorrect binary: {1}", moduleId, selected.Select(a => a ? '1' : '0').Join(""));
            module.HandleStrike();
            module.HandlePass();
        }
        else
        {
            module.HandlePass();
            yield return new WaitForSeconds(.1f);
            audio.PlaySoundAtTransform("InputCorrect", centerButton.transform);
        }
        OnNeedyDeactivation();
    }

    private void PressButton(KMSelectable button)
    {
        if (!active || !centerPressed || animating || selected[Array.IndexOf(buttons, button)])
            return;
        var ix = Array.IndexOf(buttons, button);
        centerLight.enabled = false;
        centerButton.GetComponent<Renderer>().material.color = gray;
        selected[ix] = true;
        buttonLights[ix].enabled = true;
        buttonRenders[ix].material = usedMats[1];
        buttonRenders[ix].material.color = litColors[colorsPresent[ix]];
        pressedCount++;
        audio.PlaySoundAtTransform("ButtonPress" + pressedCount, button.transform);
    }

    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = "\"!{0} <1/2/3/4>\" [Presses those colored buttons, starting from the top-right and counting clockwise. Multiples can be chained, i.e. \"!{0} 134\".] | \"!{0} center/submit/gray/grey/a\" [Presses the gray button.] | Mentioned commands can be chained by spacing out the presses, i.e. \"!{0} center 134 a\"";
    private bool autosolveInteract;
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string cmd)
    {
        var selectablesAll = new List<KMSelectable>();
        var centerPressOptions = new string[] { "c", "gray", "grey", "center", "centre", "middle", "submit", "enter", "a" };
        var intCmd = cmd.Trim();
        foreach (var cmdPortion in intCmd.Split())
        {
            if (cmdPortion.All(x => "1234".Contains(x)))
            {
                yield return null;
                for (int i = 0; i < cmdPortion.Length; i++)
                    selectablesAll.Add(buttons[int.Parse(cmdPortion[i].ToString()) - 1]);
            }
            else if (centerPressOptions.Contains(cmdPortion.ToLowerInvariant()))
            {
                selectablesAll.Add(centerButton);
            }
            else
                yield break;
        }
        if (selectablesAll.Any())
            yield return null;
        foreach (var selectable in selectablesAll)
        {
            selectable.OnInteract();
            yield return new WaitForSeconds(.1f);
        }
    }

    private void TwitchHandleForcedSolve()
    {
        //The code is done in a coroutine instead of here so that if the solvebomb command was executed this will just input the number right when it activates and it wont wait for its turn in the queue
        StartCoroutine(HandleSolve());
    }

    private IEnumerator HandleSolve()
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
