using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

public class World_Generator : MonoBehaviour
{

    [Title ("PROFILES")]
    public World_Profile profile;

    [Title ("LAYERS")]
    public Tilemap layerPrefab;

    [Title ("WATER")]
    public Gradient worldColorByElevation;
    public Material waterMaterial;
    public LayerMask waterLayer;
    public string waterSortinLayer;
    public Gradient waterColor;
    public int waterShoreSize = 1;

    [Title ("BLEND TEST")]
    public float dirtLevel = 0.015f;
    public Material dirtBlendMaterial;

    [Title ("Debug")]
    [SerializeField] InputAction generateInput;

    World world;

    Tilemap [] layers;

    SpriteRenderer worldRenderer;
    SpriteRenderer waterRenderer;
    SpriteRenderer dirtBlendRenderer;

    Sprite fullMaskSprite;
    Sprite worldSprite;
    Sprite waterSprite;
    Sprite dirtBlendSprite;

    Texture2D fullMaskTexture;
    Texture2D worldTexture;
    Texture2D shoreMask;
    Texture2D groundMask;
    Texture2D waterMask;
    Texture2D waterColorTexture;
    Texture2D dirtBlendTexture;

    public int size => profile.size;

    void Awake ()
    {
        Generate();
        generateInput.performed += (v) => Generate();
    }

    [Button("GENERATE")]
    void Generate()
    {
        ClearAndInitMap();
        GenerateHeightMap();
        Test();
        Display();
    }

    void ClearAndInitMap ()
    {
        for (int i = transform.childCount - 1; i >= 0; i--) {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        world = new (profile);
        layers = new Tilemap[world.layers];
        World.current = world;

        for (int i = 0; i < world.layers; i++) {
            layers[i] = Instantiate<Tilemap>(layerPrefab, transform);
            layers[i].name = "Layer " + i;
            layers[i].gameObject.SetActive(true);
            TilemapRenderer rend = layers[i].gameObject.GetComponent<TilemapRenderer> ();
            rend.sortingLayerName = i > 0 ? "World" : "Default";
        }
    }

    void GenerateHeightMap()
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
                world.elevation[x, y] = elevation;
            }
        }

        for (int x = 0; x < world.size; x++) {
            for (int y = 0; y < world.size; y++) {

                // Le terrain est transformé en île et la transition des côtes est contrôlée
                float elevation = (world.elevation [x, y] - min) / (max - min);
                float _x = Mathf.Abs(x / ((float)world.size / 2) - 1);
                float _y = Mathf.Abs(y / ((float)world.size / 2) - 1);
                elevation *= 1 - Mathf.Max(_x, _y);
                float a = profile.fallOffParamA;
                float b = profile.fallOffParamB;
                elevation = Mathf.Pow(elevation, a) / (Mathf.Pow(elevation, a) + Mathf.Pow(b - b * elevation, a));
                world.elevation[x, y] = elevation;
            }
        }
    }

    void Test ()
    {
        for (int x = 0; x < world.size; x++) {
            for (int y = 0; y < world.size; y++) {

                Vector3Int pos = new Vector3Int(x, y, 0);
                float elevation = world.elevation[x, y];

                for (int i = 0; i < profile.tiles.Count; i++) {
                    World_Profile.MinMaxTile t = profile.tiles[i];
                    if (elevation < t.startElevation) continue;
                    layers[t.tile.layer].SetTile(pos, t.tile);
                }
            }
        }

        for (int i = 0; i < world.layers; i++) {
            layers[i].RefreshAllTiles();
        }
    }

    void Display()
    {

        // =========================================== Init    

        worldRenderer = new GameObject("Ground").AddComponent<SpriteRenderer>();
        waterRenderer = new GameObject("Water").AddComponent<SpriteRenderer>();
        dirtBlendRenderer = new GameObject("DirtBlend").AddComponent<SpriteRenderer>();
        worldRenderer.transform.parent = transform;
        waterRenderer.transform.parent = transform;
        dirtBlendRenderer.transform.parent = transform;

        fullMaskTexture = new Texture2D(world.size, world.size);
        worldTexture = new Texture2D(world.size, world.size);
        shoreMask = new Texture2D(world.size, world.size);
        groundMask = new Texture2D(world.size, world.size);
        waterMask = new Texture2D(world.size, world.size);
        waterColorTexture = new Texture2D(world.size, world.size);
        dirtBlendTexture = new Texture2D(world.size, world.size);

        // =========================================== Generate world base, ground and water masks

        for (int x = 0; x < world.size; x++) {
            for (int y = 0; y < world.size; y++) {
                float elevation = world.elevation[x, y];
                worldTexture.SetPixel(x, y, worldColorByElevation.Evaluate(elevation));
                float gm = elevation > profile.waterLevel ? 1 : 0;
                float wm = elevation <= profile.waterLevel ? 1 : 0;
                float wd = Mathf.Clamp01 (elevation / profile.waterLevel);
                groundMask.SetPixel(x, y, new Color(gm, gm, gm, gm));
                waterMask.SetPixel(x, y, new Color(wm, wm, wm, wm));
                waterColorTexture.SetPixel(x, y, waterColor.Evaluate (wd));
            }
        }

        // =========================================== Generate shore mask

        List<(int, int, float)> edgeCoords = new List<(int, int, float)>();
        for (int x = 0; x < world.size; x++) {
            for (int y = 0; y < world.size; y++) {

                // Si au moin un voisin est de l'eau, alors je suis une côte
                bool water = GetElevation(x, y) < profile.waterLevel;
                bool waterUp = GetElevation (x, y + 1) < profile.waterLevel;
                bool waterLeft = GetElevation (x - 1, y) < profile.waterLevel;
                bool waterDown = GetElevation (x, y - 1) < profile.waterLevel;
                bool waterRight = GetElevation (x + 1, y) < profile.waterLevel;
                bool edge = !water && (waterUp || waterLeft || waterDown || waterRight);

                // Epaisseur du mask
                if (edge) {
                    if (waterShoreSize == 0) {
                        edgeCoords.Add((x, y, 1));
                    }
                    else {
                        for (int _x = x - waterShoreSize; _x <= x + waterShoreSize; _x++) {
                            for (int _y = y - waterShoreSize; _y <= y + waterShoreSize; _y++) {
                                if (IsInMap(_x, _y)) edgeCoords.Add((_x, _y, (_x == x && _x == y) ? 1 : 0.5f));
                            }
                        }
                    }
                }

                shoreMask.SetPixel(x, y, new Color(0, 0, 0, 0));
            }
        }

        // Add shores
        foreach ((int, int, float) coord in edgeCoords) {
            float t = shoreMask.GetPixel(coord.Item1, coord.Item2).a;
            float c = t > coord.Item3 ? 1 : coord.Item3;
            shoreMask.SetPixel(coord.Item1, coord.Item2, new Color(c, c, c, c));
        }

        // =========================================== Generate Dirt Blend

        for (int x = 0; x < world.size; x++) {
            for (int y = 0; y < world.size; y++) {

                // Si au moin un voisin n'est pas de la terre, alors je suis un rebord
                bool dirt = GetElevation(x, y) >= dirtLevel;
                bool dirtUp = GetElevation (x, y + 1) >= dirtLevel;
                bool dirtLeft = GetElevation (x - 1, y) >= dirtLevel;
                bool dirtDown = GetElevation (x, y - 1) >= dirtLevel;
                bool dirtRight = GetElevation (x + 1, y) >= dirtLevel;
                bool edge = dirt && !(dirtUp && dirtLeft && dirtDown && dirtRight);
                float d = edge ? 1 : 0;
                dirtBlendTexture.SetPixel(x, y, new Color (d, d, d, d));
            }
        }

        // =========================================== Finalise

        fullMaskTexture.Apply();
        worldTexture.Apply();
        shoreMask.Apply();
        groundMask.Apply();
        waterMask.Apply();
        waterColorTexture.Apply();
        dirtBlendTexture.Apply();

        fullMaskTexture.filterMode = FilterMode.Bilinear;
        worldTexture.filterMode = FilterMode.Bilinear;
        groundMask.filterMode = FilterMode.Point;
        shoreMask.filterMode = FilterMode.Bilinear;
        waterMask.filterMode = FilterMode.Bilinear;
        waterColorTexture.filterMode = FilterMode.Bilinear;
        dirtBlendTexture.filterMode = FilterMode.Point;

        waterMaterial.SetTexture("_ShoreMask", shoreMask);
        waterMaterial.SetTexture("_GroundMask", groundMask);
        waterMaterial.SetTexture("_WaterMask", waterMask);
        waterMaterial.SetTexture("_WaterColor", waterColorTexture);
        dirtBlendMaterial.SetTexture("_DirtMask", dirtBlendTexture);

        fullMaskSprite = Sprite.Create(fullMaskTexture, new(0, 0, world.size, world.size), Vector2.one / 2);
        worldSprite = Sprite.Create(worldTexture, new(0, 0, world.size, world.size), Vector2.one / 2);
        waterSprite = Sprite.Create(shoreMask, new(0, 0, world.size, world.size), Vector2.one / 2);
        dirtBlendSprite = Sprite.Create(dirtBlendTexture, new(0, 0, world.size, world.size), Vector2.one / 2);

        worldRenderer.sprite = worldSprite;
        waterRenderer.sprite = fullMaskSprite;
        waterRenderer.material = waterMaterial;
        waterRenderer.sortingOrder = 1;
        waterRenderer.sortingLayerName = waterSortinLayer;
        dirtBlendRenderer.sprite = dirtBlendSprite;
        dirtBlendRenderer.material = dirtBlendMaterial;

        waterRenderer.transform.localScale = Vector3.one * 100;
        dirtBlendRenderer.transform.localScale = Vector3.one * 100;

        Vector3 position = Vector3.one * world.size;
        waterRenderer.transform.position = position;
        worldRenderer.transform.position = position;
        dirtBlendRenderer.transform.position = position;

        // =========================================== Tools
    }

    float GetElevation(int x, int y)
    {
        if (!IsInMap(x, y)) return 0;
        else return world.elevation[x, y];
    }

    bool IsInMap(int x, int y)
    {
        return x > -1 && x < world.size && y > -1 && y < world.size;
    }
}