using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class World_Displayer : MonoBehaviour
{

    public Gradient worldColorByElevation;
    public Material waterMaterial;
    public float waterLevel = 0.1f;

    public LayerMask waterLayer;
    public string waterSortinLayer;

    SpriteRenderer worldRenderer;
    SpriteRenderer waterRenderer;

    Sprite fullMaskSprite;
    Sprite worldSprite;
    Sprite waterSprite;

    Texture2D fullMaskTexture;
    Texture2D worldTexture;
    Texture2D shoreMask;
    Texture2D groundMask;
    Texture2D waterMask;

    public void Display(World_Datas world)
    {

        // =========================================== Init

        // Childs
        for (int i = transform.childCount - 1; i >= 0; i--) {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        worldRenderer = new GameObject("Ground").AddComponent<SpriteRenderer>();
        waterRenderer = new GameObject("Water").AddComponent<SpriteRenderer>();
        worldRenderer.transform.parent = transform;
        waterRenderer.transform.parent = transform;

        fullMaskTexture = new Texture2D(world.size, world.size);
        worldTexture = new Texture2D(world.size, world.size);
        shoreMask = new Texture2D(world.size, world.size);
        groundMask = new Texture2D(world.size, world.size);
        waterMask = new Texture2D(world.size, world.size);

        // =========================================== Generate world base, ground and water masks

        for (int x = 0; x < world.size; x++) {
            for (int y = 0; y < world.size; y++) {
                float elevation = world.heightMap[x, y];
                worldTexture.SetPixel(x, y, worldColorByElevation.Evaluate(elevation));
                float gm = world.heightMap[x, y] > waterLevel ? 1 : 0;
                float wm = world.heightMap[x, y] < waterLevel ? 1 : 0;
                groundMask.SetPixel(x, y, new Color(gm, gm, gm, gm));
                waterMask.SetPixel(x, y, new Color(wm, wm, wm, wm));
            }
        }

        // =========================================== Generate shore mask

        List<(int, int, float)> edgeCoords = new List<(int, int, float)>();
        for (int x = 0; x < world.size; x++) {
            for (int y = 0; y < world.size; y++) {

                // Si au moin un voisin est de l'eau, alors je suis une côte
                bool water = GetElevation(x, y) < waterLevel;
                bool waterUp = GetElevation (x, y + 1) < waterLevel;
                bool waterLeft = GetElevation (x - 1, y) < waterLevel;
                bool waterDown = GetElevation (x, y - 1) < waterLevel;
                bool waterRight = GetElevation (x + 1, y) < waterLevel;
                bool edge = !water && (waterUp || waterLeft || waterDown || waterRight);

                // Epaisseur du mask
                if (edge) {
                    for (int _x = x - 1; _x <= x + 1; _x++) {
                        for (int _y = y - 1; _y <= y + 1; _y++) {
                            if (IsInMap(_x, _y)) edgeCoords.Add((_x, _y, (_x == x && _x == y) ? 1 : 0.5f));
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

        // =========================================== Finalise

        fullMaskTexture.Apply();
        worldTexture.Apply();
        shoreMask.Apply();
        groundMask.Apply();
        waterMask.Apply();

        fullMaskTexture.filterMode = FilterMode.Bilinear;
        worldTexture.filterMode = FilterMode.Bilinear;
        groundMask.filterMode = FilterMode.Point;
        shoreMask.filterMode = FilterMode.Bilinear;
        waterMask.filterMode = FilterMode.Bilinear;

        waterMaterial.SetTexture("_ShoreMask", shoreMask);
        waterMaterial.SetTexture("_GroundMask", groundMask);
        waterMaterial.SetTexture("_WaterMask", waterMask);

        fullMaskSprite = Sprite.Create(fullMaskTexture, new(0, 0, world.size, world.size), Vector2.one / 2);
        worldSprite = Sprite.Create(worldTexture, new(0, 0, world.size, world.size), Vector2.one / 2);
        waterSprite = Sprite.Create(shoreMask, new(0, 0, world.size, world.size), Vector2.one / 2);

        worldRenderer.sprite = worldSprite;
        waterRenderer.sprite = fullMaskSprite;
        waterRenderer.material = waterMaterial;
        waterRenderer.sortingOrder = 1;
        waterRenderer.sortingLayerName = waterSortinLayer;

        // =========================================== Tools

        float GetElevation(int x, int y)
        {
            if (!IsInMap(x, y)) return 0;
            else return world.heightMap[x, y];
        }

        bool IsInMap(int x, int y)
        {
            return x > -1 && x < world.size && y > -1 && y < world.size;
        }
    }
}