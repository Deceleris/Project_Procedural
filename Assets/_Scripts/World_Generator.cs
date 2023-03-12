using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

public class World_Generator : MonoBehaviour
{

    [Title ("REFERENCES")]
    [SerializeField] World_Displayer worldDisplayer;
    [SerializeField] World_Profile profile;

    [Title ("Debug")]
    public bool autoUpdate = true;
    [SerializeField] InputAction generateInput;

    World_Datas world;

    public int size => profile.size;

    void Awake ()
    {
        Generate();
        generateInput.performed += (v) => Generate();
    }
 
    [Button("GENERATE")]
    void Generate ()
    {
        StartCoroutine(GenerationProcess()); IEnumerator GenerationProcess () {

            ClearAndInitMap();

            // Génère un carte de hauteur selon une noise
            yield return GenerateHeightMap();

            worldDisplayer.Display(world);
        }
    }

    void ClearAndInitMap ()
    {
        world = new (size);
    }

    IEnumerator GenerateHeightMap()
    {
        NoiseFilter noiseFilter = new NoiseFilter (profile.noise);
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int x = 0; x < world.size; x++) {
            for (int y = 0; y < world.size; y++) {

                // Elevation obtenue grâce au calculs de la noise
                Vector2Int position = new Vector2Int(x, y);
                float elevation = noiseFilter.Evaluate(position);
                if (elevation < min) min = elevation;
                if (elevation > max) max = elevation;
                world.heightMap[x, y] = elevation;
            }
        }

        for (int x = 0; x < world.size; x++) {
            for (int y = 0; y < world.size; y++) {

                // Le terrain est transformé en île et la transition des côtes est contrôlée
                float elevation = (world.heightMap [x, y] - min) / (max - min);
                float _x = Mathf.Abs(x / ((float)world.size / 2) - 1);
                float _y = Mathf.Abs(y / ((float)world.size / 2) - 1);
                elevation *= 1 - Mathf.Max(_x, _y);
                float a = profile.fallOffParamA;
                float b = profile.fallOffParamB;
                elevation = Mathf.Pow(elevation, a) / (Mathf.Pow(elevation, a) + Mathf.Pow(b - b * elevation, a));
                world.heightMap[x, y] = elevation;
            }
        }

        yield return null;
    }

    private void OnDrawGizmos()
    {
        if (autoUpdate) Generate();
    }
}