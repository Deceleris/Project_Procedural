using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseFilter {

    NoiseType noiseType;
    Noise noise;

    public NoiseFilter(NoiseType noisType) {
        this.noiseType = noisType;
        this.noise = new Noise(0);
    }

    public float Evaluate (Vector2Int position) {
        Vector3 point = new Vector3(position.x, position.y, 0);

        float noiseValue = 0;
        float frequency = noiseType.baseRoughness;
        float amplitude = 1;

        for (int i = 0; i< noiseType.numLayers; i++) {
            float v = noise.Evaluate(point * frequency);
            noiseValue += ( v + 1 ) / 2 * amplitude;
            frequency *= noiseType.roughness;
            amplitude *= noiseType.persistance;
        }

        noiseValue = Mathf.Max(0, noiseValue - noiseType.minValue);    
        return noiseValue * noiseType.strength;
    }

    public float Evaluate(int x, int y)
    {
        Vector3 point = new Vector3(x, y, 0);

        float noiseValue = 0;
        float frequency = noiseType.baseRoughness;
        float amplitude = 1;

        for (int i = 0; i < noiseType.numLayers; i++) {
            float v = noise.Evaluate(point * frequency);
            noiseValue += (v + 1) / 2 * amplitude;
            frequency *= noiseType.roughness;
            amplitude *= noiseType.persistance;
        }

        noiseValue = Mathf.Max(0, noiseValue - noiseType.minValue);
        return noiseValue * noiseType.strength;
    }
}