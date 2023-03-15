using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

public class World_Generator : MonoBehaviour
{

    [Title ("MAIN")]
    public World_Profile profile;
    public Tilemap layerPrefab;

    [Title ("WATER")]
    public Gradient worldColorByElevation;
    public Material waterMaterial;
    public LayerMask waterLayer;
    public string waterSortinLayer;
    public Gradient waterColor;
    public int waterShoreSize = 1;

    [Title ("BLEND")]
    public Color dirtBlendColor;
    public float dirtLevel = 0.015f;
    public Material dirtBlendMaterial;
    public string dirtSortingLayer;

    [Title ("GRASS")]
    public Material grassMaterial;

    // ====================================================== VARIABLES

    // Common
    [SerializeField, HideInInspector] World world;
    [SerializeField, HideInInspector] Tilemap [] layers;

    // Heightmap
    [SerializeField, HideInInspector] SpriteRenderer heightRenderer;
    [SerializeField, HideInInspector] Sprite heightSprite;
    [SerializeField, HideInInspector] Texture2D heightTexture;

    // Water
    [SerializeField, HideInInspector] SpriteRenderer waterRenderer;
    [SerializeField, HideInInspector] Sprite waterSprite;
    [SerializeField, HideInInspector] Texture2D waterShoreTexture;
    [SerializeField, HideInInspector] Texture2D waterMaskTexture;
    [SerializeField, HideInInspector] Texture2D waterColorTexture;

    // Dirt
    [SerializeField, HideInInspector] SpriteRenderer dirtBlendRenderer;
    [SerializeField, HideInInspector] Sprite dirtBlendSprite;
    [SerializeField, HideInInspector] Texture2D dirtBlendTexture;

    public int size => profile.size;

    void Awake ()
    {
        GenerateWorld();
    }

    [Button("GENERATE")]
    void GenerateWorld()
    {
        SetupBases();
        GenerateHeightMap();
        SetTileByElevation();
        GenerateWater();
        GenerateBlends();
        GenerateGrass();
    }

    // ======================================================= BASES

    void SetupBases ()
    {
        // Initialisation des variables
        world = new (profile);
        layers = new Tilemap[world.layers];
        World.current = world;

        // Suppression des enfants
        for (int i = transform.childCount - 1; i >= 0; i--) {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // Génération des layers
        for (int i = 0; i < world.layers; i++) {
            layers[i] = Instantiate<Tilemap>(layerPrefab, transform);
            layers[i].name = "Layer " + i;
            layers[i].gameObject.SetActive(true);
            TilemapRenderer rend = layers[i].gameObject.GetComponent<TilemapRenderer> ();
            rend.sortingLayerName = i > 0 ? "World" : "Default";
        }
    }

    // ======================================================= ELEVATION

    void GenerateHeightMap()
    {
        NoiseFilter noiseFilter = new NoiseFilter (profile.noise);
        float min = float.MaxValue;
        float max = float.MinValue;

        // Elevation obtenue grâce au calculs de la noise et stockage du min max
        for (int x = 0; x < world.size; x++) {
            for (int y = 0; y < world.size; y++) {

                Vector2Int position = new Vector2Int(x, y);
                float elevation = noiseFilter.Evaluate(position);
                if (elevation < min) min = elevation;
                if (elevation > max) max = elevation;
                world.elevation[x, y] = elevation;
            }
        }

        // Le terrain est transformé en île et la transition des côtes est contrôlée
        for (int x = 0; x < world.size; x++) {
            for (int y = 0; y < world.size; y++) {

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

    // ======================================================= TILES BY ELEVATION

    void SetTileByElevation ()
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

    // ======================================================= SHADERS AND TEXTURES

    void DisplayHeightMap()
    {
        heightRenderer = new GameObject("Ground").AddComponent<SpriteRenderer>();
        heightRenderer.transform.parent = transform;
        heightTexture = new Texture2D(world.size, world.size);
        Vector3 position = Vector3.one * world.size;
        heightRenderer.transform.position = position;
        heightTexture.Apply();
        heightTexture.filterMode = FilterMode.Bilinear;
        heightSprite = Sprite.Create(heightTexture, new(0, 0, world.size, world.size), Vector2.one / 2);
        heightRenderer.sprite = heightSprite;
    }

    void GenerateWater ()
    {
        // Create textures
        waterShoreTexture = new Texture2D(world.size, world.size);
        waterMaskTexture = new Texture2D(world.size, world.size);
        waterColorTexture = new Texture2D(world.size, world.size);
        waterShoreTexture.filterMode = FilterMode.Bilinear;
        waterMaskTexture.filterMode = FilterMode.Bilinear;
        waterColorTexture.filterMode = FilterMode.Bilinear;

        // =========================================== TEXTURE GENERATION

        // Create mask and water color
        for (int x = 0; x < world.size; x++) {
            for (int y = 0; y < world.size; y++) {
                float elevation = world.elevation[x, y];
                float wm = elevation <= profile.waterLevel ? 1 : 0;
                float wd = Mathf.Clamp01 (elevation / profile.waterLevel);
                waterMaskTexture.SetPixel(x, y, new Color(wm, wm, wm, wm));
                waterColorTexture.SetPixel(x, y, waterColor.Evaluate(wd));
            }
        }

        // Getting shores
        List<(int, int, float)> edgeCoords = new List<(int, int, float)>();
        for (int x = 0; x < world.size; x++) {
            for (int y = 0; y < world.size; y++) {
                bool water = GetElevation(x, y) < profile.waterLevel;
                bool waterUp = GetElevation (x, y + 1) < profile.waterLevel;
                bool waterLeft = GetElevation (x - 1, y) < profile.waterLevel;
                bool waterDown = GetElevation (x, y - 1) < profile.waterLevel;
                bool waterRight = GetElevation (x + 1, y) < profile.waterLevel;
                bool edge = !water && (waterUp || waterLeft || waterDown || waterRight);
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
                waterShoreTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
            }
        }

        // Add shores
        foreach ((int, int, float) coord in edgeCoords) {
            float t = waterShoreTexture.GetPixel(coord.Item1, coord.Item2).a;
            float c = t > coord.Item3 ? 1 : coord.Item3;
            waterShoreTexture.SetPixel(coord.Item1, coord.Item2, new Color(c, c, c, c));
        }

        // ==================================================

        // Apply textures
        waterShoreTexture.Apply();
        waterMaskTexture.Apply();
        waterColorTexture.Apply();
        waterMaterial.SetTexture("_ShoreMask", waterShoreTexture);
        waterMaterial.SetTexture("_WaterMask", waterMaskTexture);
        waterMaterial.SetTexture("_WaterColor", waterColorTexture);
        waterMaterial.SetFloat("_WorldSize", profile.size);

        // Create the renderer
        waterSprite = Sprite.Create(waterMaskTexture, new(0, 0, world.size, world.size), Vector2.one / 2);
        waterRenderer = new GameObject("Water").AddComponent<SpriteRenderer>();
        waterRenderer.sprite = waterSprite;
        waterRenderer.transform.parent = transform;
        waterRenderer.sortingOrder = 1;
        waterRenderer.material = waterMaterial;
        waterRenderer.sortingLayerName = waterSortinLayer;
        waterRenderer.transform.localScale = Vector3.one * 100;
        waterRenderer.transform.position = new Vector3(1, 1, 0) * world.size;
    }

    void GenerateBlends ()
    {
        dirtBlendTexture = new Texture2D(world.size, world.size);
        dirtBlendTexture.filterMode = FilterMode.Point;

        for (int x = 0; x < world.size; x++) {
            for (int y = 0; y < world.size; y++) {
                bool dirt = GetElevation(x, y) >= dirtLevel;
                bool dirtUp = GetElevation (x, y + 1) >= dirtLevel;
                bool dirtLeft = GetElevation (x - 1, y) >= dirtLevel;
                bool dirtDown = GetElevation (x, y - 1) >= dirtLevel;
                bool dirtRight = GetElevation (x + 1, y) >= dirtLevel;
                bool edge = dirt && !(dirtUp && dirtLeft && dirtDown && dirtRight);
                float d = edge ? 1 : 0;
                dirtBlendTexture.SetPixel(x, y, new Color(d, d, d, d));
            }
        }

        dirtBlendTexture.Apply();
        dirtBlendMaterial.SetTexture("_DirtMask", dirtBlendTexture);
        dirtBlendMaterial.SetFloat("_WorldScale", profile.size);

        dirtBlendSprite = Sprite.Create(dirtBlendTexture, new(0, 0, world.size, world.size), Vector2.one / 2);
        dirtBlendRenderer = new GameObject("DirtBlend").AddComponent<SpriteRenderer>();
        dirtBlendRenderer.sprite = dirtBlendSprite;
        dirtBlendRenderer.color = dirtBlendColor;
        dirtBlendRenderer.transform.parent = transform;
        dirtBlendRenderer.material = dirtBlendMaterial;
        dirtBlendRenderer.transform.localScale = Vector3.one * 100;
        dirtBlendRenderer.transform.position = new Vector3 (1, 1, 0) * world.size;
        dirtBlendRenderer.sortingLayerName = dirtSortingLayer;
        dirtBlendRenderer.sortingOrder = 0;
    }

    void GenerateGrass ()
    {
        Tilemap map = layers[5];
        TilemapRenderer renderer = map.GetComponent<TilemapRenderer>();
        renderer.sortingLayerName = dirtSortingLayer;
        renderer.sortingOrder = 2;
        renderer.material = grassMaterial;
        renderer.sortOrder = TilemapRenderer.SortOrder.TopLeft;
    }

    // ================================================ TOOLS

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