using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class World_Datas
{

    public int size;
    public int half;
    public float [,] heightMap;

    public World_Datas (int size)
    {
        this.size = size;
        half = size / 2;
        heightMap = new float[size, size];
    }
}