using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class boozleglyphIdentification : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMNeedyModule module;

    public KMSelectable submit;
    public KMSelectable[] arrowButtons;
    public GameObject screenObj;
    private Vector3[] screenPos = new Vector3[] { new Vector3(0f, 0.8999966f, 0.02000031f), new Vector3(0f, 0.8999966f, 0.08f) };
    public TextMesh screenText;
    public Renderer screen;
    public Texture none;
    public Texture[] set1;
    public Texture[] set2;
    public Texture[] set3;
    private static List<Texture[]> allTextures = new List<Texture[]>();

    private bool active;
    private bool bombSolved;
    private int stage;
    private int letterIndex;
    private int setIndex;
    private int chosenLetter;
    private int chosenSet;
    private static readonly string[] alphabet = new string[26] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
    private static readonly string[] setNumbers = new string[3] { "1", "2", "3" };

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
        submit.OnInteract += delegate () { Submit(); return false; };
        foreach (KMSelectable button in arrowButtons)
            button.OnInteract += delegate () { PressArrowButton(button); return false; };
    }

    void Start()
    {
        Debug.LogFormat("[Boozleglyph Identification #{0}] Needy initiated.", moduleId);
        screenText.text = "";
        allTextures = new List<Texture[]> { set1, set2, set3 };
    }

    protected void OnNeedyActivation()
    {
        active = true;
        screenText.text = "A";
        letterIndex = rnd.Range(0, 26);
        setIndex = rnd.Range(0, 3);
        Debug.LogFormat("[Boozleglyph Identification #{0}] The displayed boozleglyph is {1} from set {2}.", moduleId, alphabet[letterIndex], setNumbers[setIndex]);
        screen.material.mainTexture = allTextures[setIndex][letterIndex];
    }

    protected void OnNeedyDeactivation()
    {
        active = false;
        screen.material.mainTexture = none;
        screenText.text = "";
        chosenLetter = 0;
        chosenSet = 0;
        stage = 0;
    }

    protected void OnTimerExpired()
    {
        if (active)
        {
            module.OnStrike();
            OnNeedyDeactivation();
        }
    }

    void Submit()
    {
        if (!active)
            return;
        if (stage == 0)
        {
            if (chosenLetter != letterIndex)
            {
                Debug.LogFormat("[Boozleglyph Identification #{0}] Submitted letter {1}, that is incorrect. Strike!", moduleId, alphabet[chosenLetter]);
                module.OnStrike();
            }
            else
            {
                Debug.LogFormat("[Boozleglyph Identification #{0}] Submitted letter {1}, that is correct.", moduleId, alphabet[chosenLetter]);
                stage++;
                UpdateScreen();
            }
        }
        else if (stage == 1)
        {
            if (chosenSet != setIndex)
            {
                Debug.LogFormat("[Boozleglyph Identification #{0}] Submitted set {1}, that is incorrect. Strike!", moduleId, setNumbers[chosenSet]);
                module.OnStrike();
            }
            else
            {
                Debug.LogFormat("[Boozleglyph Identification #{0}] Submitted set {1}, that is correct. Module temporarily neutralized!", moduleId, setNumbers[chosenSet]);
                module.OnPass();
                OnNeedyDeactivation();
            }
        }
    }

    void PressArrowButton(KMSelectable button)
    {
        if (!active)
            return;
        button.AddInteractionPunch(.5f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        int ix = Array.IndexOf(arrowButtons, button);
        int[] offsets = new int[2] { -1, 1 };
        chosenLetter += offsets[ix];
        chosenSet += offsets[ix];
        if (chosenLetter == -1)
            chosenLetter = 25;
        if (chosenSet == -1)
            chosenSet = 2;
        chosenLetter %= 26;
        if (chosenLetter == 16)
        {
            screenObj.transform.localPosition = screenPos[1];
        }
        else
        {
            screenObj.transform.localPosition = screenPos[0];
        }
        chosenSet %= 3;
        UpdateScreen();
    }

    void UpdateScreen()
    {
        if (stage == 0)
            screenText.text = alphabet[chosenLetter];
        else
            screenText.text = setNumbers[chosenSet];
    }

    // Twitch Plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} submit <boz> <set> [Submits the boozleglyph 'boz' from set 'set'] | Valid boozleglyphs are A-Z | Valid sets are 1-3";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length > 3)
            {
                yield return "sendtochaterror Too many parameters!";
            }
            else if (parameters.Length == 3)
            {
                parameters[1] = parameters[1].ToUpper();
                if (!alphabet.Contains(parameters[1]))
                {
                    yield return "sendtochaterror The specified boozleglyph '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                int temp = 0;
                if (int.TryParse(parameters[2], out temp))
                {
                    if (temp < 1 || temp > 3)
                    {
                        yield return "sendtochaterror The specified set '" + parameters[2] + "' is out of range 1-3!";
                        yield break;
                    }
                    if (stage == 0)
                    {
                        int front = 0;
                        int back = 0;
                        string current = alphabet[chosenLetter];
                        int offset = 0;
                        while (current != parameters[1])
                        {
                            front++;
                            if (chosenLetter + front > 25)
                            {
                                offset = -26;
                            }
                            current = alphabet[(chosenLetter + front) + offset];
                        }
                        current = alphabet[chosenLetter];
                        offset = 0;
                        while (current != parameters[1])
                        {
                            back++;
                            if (chosenLetter - back < 0)
                            {
                                offset = 26;
                            }
                            current = alphabet[(chosenLetter - back) + offset];
                        }
                        if (front > back)
                        {
                            while (parameters[1] != screenText.text)
                            {
                                arrowButtons[0].OnInteract();
                                yield return new WaitForSeconds(0.1f);
                            }
                        }
                        else if (front < back)
                        {
                            while (parameters[1] != screenText.text)
                            {
                                arrowButtons[1].OnInteract();
                                yield return new WaitForSeconds(0.1f);
                            }
                        }
                        else
                        {
                            int rando = rnd.Range(0, 2);
                            while (parameters[1] != screenText.text)
                            {
                                arrowButtons[rando].OnInteract();
                                yield return new WaitForSeconds(0.1f);
                            }
                        }
                        submit.OnInteract();
                        yield return new WaitForSeconds(0.1f);
                    }
                    else if (stage == 1 && parameters[1] != alphabet[letterIndex])
                    {
                        yield return "sendtochaterror The specified letter '" + parameters[1] + "' was not correct in the first stage!";
                        yield break;
                    }
                    if ((chosenSet == 2 && temp == 1) || (chosenSet == 0 && temp == 2) || (chosenSet == 1 && temp == 3))
                    {
                        arrowButtons[1].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                    }
                    else if ((chosenSet == 2 && temp == 2) || (chosenSet == 1 && temp == 1) || (chosenSet == 0 && temp == 3))
                    {
                        arrowButtons[0].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                    }
                    submit.OnInteract();
                }
                else
                {
                    yield return "sendtochaterror The specified set '" + parameters[2] + "' is invalid!";
                }
            }
            else if (parameters.Length == 2)
            {
                yield return "sendtochaterror Please specify the set to submit!";
            }
            else if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify the boozleglyph and set to submit!";
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
            if (stage == 1)
                stage = 0;
            yield return ProcessTwitchCommand("submit " + alphabet[letterIndex] + " " + setNumbers[setIndex]);
        }
    }
}
