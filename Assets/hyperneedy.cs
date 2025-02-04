using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class hyperneedy : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMNeedyModule module;

    public KMSelectable button;
    public KMSelectable[] axisButtons;
    public Transform[] allDiscs;
    public Renderer[] allDiscRenders;
    public Color[] discColors;
    private Color[] usedColors = new Color[16];

    private bool active;
    private bool bombSolved;
    private bool animating;
    private bool discsOut;
    private int rotationIndex;
    private int enteringStage;
    private Vector3[] defaultPositions = new Vector3[16];
    private static readonly string[] rotationNames = new string[12] { "XY", "YX", "XZ", "ZX", "XW", "WX", "YZ", "ZY", "YW", "WY", "ZW", "WZ" };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    private void Awake()
    {
        moduleId = moduleIdCounter++;
        module.OnNeedyActivation += OnNeedyActivation;
        module.OnNeedyDeactivation += OnNeedyDeactivation;
        module.OnTimerExpired += OnTimerExpired;
        bomb.OnBombExploded += delegate () { bombSolved = true; };
        bomb.OnBombSolved += delegate () { bombSolved = true; };
        button.OnInteract += delegate () { ButtonPress(); return false; };
        foreach (KMSelectable axisButton in axisButtons)
            axisButton.OnInteract += delegate () { AxisButtonPress(axisButton); return false; };
    }

    private void Start()
    {
        Debug.LogFormat("[Hyperneedy #{0}] Needy initiated.", moduleId);
        module.SetResetDelayTime(45f, 60f);
        usedColors = discColors.ToList().Shuffle().ToArray();
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
        rotationIndex = rnd.Range(0, 12);
        Debug.LogFormat("[Hyperneedy #{0}] The rotation is {1}.", moduleId, rotationNames[rotationIndex]);
        var vertices = GetUnrotatedVertices();
        for (int i = 0; i < 16; i++)
            StartCoroutine(Move(allDiscs[i], new Vector3(0f, 0f, 0f), vertices[i].Project(), true));
    }

    protected void OnNeedyDeactivation()
    {
        active = false;
        enteringStage = 0;
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

    private void ButtonPress()
    {
        if (animating || !discsOut || !active)
            return;
        StartCoroutine(Rotation());
    }

    private void AxisButtonPress(KMSelectable axisButton)
    {
        if (!active)
            return;
        var currentRotation = rotationNames[rotationIndex].ToCharArray();
        axisButton.AddInteractionPunch(.5f);
        if (axisButton.GetComponentInChildren<TextMesh>().text != currentRotation[enteringStage].ToString())
        {
            Debug.LogFormat("[Hyperneedy #{0}] Pressed the button {1}, that is incorrect. Strike!", moduleId, axisButton.GetComponentInChildren<TextMesh>().text);
            module.OnStrike();
            enteringStage = 0;
        }
        else
        {
            Debug.LogFormat("[Hyperneedy #{0}] Pressed the button {1}, that is correct.", moduleId, axisButton.GetComponentInChildren<TextMesh>().text);
            enteringStage++;
            audio.PlaySoundAtTransform("Bleep" + rnd.Range(1, 11).ToString(), axisButton.transform);
        }
        if (enteringStage == 2)
        {
            Debug.LogFormat("[Hyperneedy #{0}] Module temporarily neutralized!", moduleId);
            module.OnPass();
            OnNeedyDeactivation();
        }
    }

    private IEnumerator Move(Transform disc, Vector3 startPosition, Vector3 endPosition, bool next)
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

    private IEnumerator Rotation()
    {
        animating = true;
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
        var axis12 = 1 << "XYZW".IndexOf(rotationNames[rotationIndex][0]);
        var axis22 = 1 << "XYZW".IndexOf(rotationNames[rotationIndex][1]);
        var newColors = new Color[16];
        for (int i = 0; i < 16; i++)
            newColors[((i & axis12) != 0) ^ ((i & axis22) != 0) ? (i ^ axis22) : (i ^ axis12)] = usedColors[i];
        usedColors = newColors;
        for (int i = 0; i < 16; i++)
            allDiscRenders[i].material.color = usedColors[i];
        SetHypercube(unrotatedVertices.Select(v => v.Project()).ToArray());
        animating = false;
    }

    private void SetHypercube(Vector3[] vertices)
    {
        for (int i = 0; i < 16; i++)
            allDiscs[i].localPosition = vertices[i];
    }

    private Point4D[] GetUnrotatedVertices()
    {
        return Enumerable.Range(0, 1 << 4).Select(i => new Point4D((i & 1) != 0 ? 1 : -1, (i & 2) != 0 ? 1 : -1, (i & 4) != 0 ? 1 : -1, (i & 8) != 0 ? 1 : -1)).ToArray();
    }

    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} rotate (#) [Starts the rotation of the floating discs (Optionally '#' times)] | !{0} press <rot> [Presses the buttons corresponding to the rotation 'rot'] | Valid rotations are XY, YX, XZ, ZX, XW, WX, YZ, ZY, YW, WY, ZW, and WZ";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*rotate\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
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
                    if (temp < 1 || temp > 5)
                    {
                        yield return "sendtochaterror The specified number of times to rotate '" + parameters[1] + "' is out of range 1-5!";
                        yield break;
                    }
                    for (int i = 0; i < temp; i++)
                    {
                        button.OnInteract();
                        while (animating) { yield return "trycancel Halted rotating due to a request to cancel!"; yield return new WaitForSeconds(0.1f); }
                    }
                }
                else
                {
                    yield return "sendtochaterror The specified number of times to rotate '" + parameters[1] + "' is invalid!";
                }
            }
            else if (parameters.Length == 1)
            {
                button.OnInteract();
                while (animating) { yield return new WaitForSeconds(0.1f); }
            }
            yield break;
        }
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length > 2)
            {
                yield return "sendtochaterror Too many parameters!";
            }
            else if (parameters.Length == 2)
            {
                parameters[1] = parameters[1].ToUpper();
                if (rotationNames.Contains(parameters[1]))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (axisButtons[i].GetComponentInChildren<TextMesh>().text[0].Equals(parameters[1][0]))
                        {
                            axisButtons[i].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                            break;
                        }
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        if (axisButtons[i].GetComponentInChildren<TextMesh>().text[0].Equals(parameters[1][1]))
                        {
                            axisButtons[i].OnInteract();
                            break;
                        }
                    }
                }
                else
                {
                    yield return "sendtochaterror The specified rotation '" + parameters[1] + "' is invalid!";
                }
            }
            else if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify the rotation!";
            }
            yield break;
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
            while (!active) { yield return new WaitForSeconds(0.1f); }
            while (animating && !discsOut) { yield return new WaitForSeconds(0.1f); }
            if (enteringStage == 1)
                enteringStage = 0;
            yield return ProcessTwitchCommand("press " + rotationNames[rotationIndex]);
        }
    }
}
