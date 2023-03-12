using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class World
{

    public int size;
    public int layers;
    public int half;

    public float [,] elevation;
    public float [,,] relativeElevation;
    public int [,,] occlusion;

    public static World current;

    public World (World_Profile profile)
    {
        size = profile.size;
        layers = profile.layers;
        half = size / 2;
        elevation = new float[size, size];
        relativeElevation = new float[size, size, layers];
        occlusion = new int[size, size, layers];
    }
}