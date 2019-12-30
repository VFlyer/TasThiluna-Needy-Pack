using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class templatescript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo bomb;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
      moduleId = moduleIdCounter++;
    }

    void Start()
    {

    }
}
