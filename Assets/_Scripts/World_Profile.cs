using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu (menuName = "World/World", fileName = "World")]
public class World_Profile : ScriptableObject
{

    [Title("METRICS")]
    public int size = 500;
    public int layers = 10;
    public float waterLevel = 0.1f;

    [Title("NOISE")]
    public NoiseType noise;

    [Title("TILES")]
    public List<MinMaxTile> tiles;

    [System.Serializable]
    public class MinMaxTile
    {
        public float startElevation = 0;
        public Tile_Profile tile;
    }

    [Title("FALL OFF")]
    [Range(0, 5f)] public float fallOffParamA = 1;
    [Range(0, 5f)] public float fallOffParamB = 2;

    private void OnValidate()
    {
        size = Mathf.RoundToInt((float)size / 2) * 2;
    }
}