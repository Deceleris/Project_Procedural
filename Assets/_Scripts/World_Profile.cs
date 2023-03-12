using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu (menuName = "World/World", fileName = "World")]
public class World_Profile : ScriptableObject
{

    [Title("METRICS")]
    public int size = 500;

    [Title("NOISE")]
    public NoiseType noise;

    [Title("FALL OFF")]
    [Range(0, 5f)] public float fallOffParamA = 1;
    [Range(0, 5f)] public float fallOffParamB = 2;

    private void OnValidate()
    {
        size = Mathf.RoundToInt((float)size / 2) * 2;
    }
}