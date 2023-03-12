using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;

[CreateAssetMenu (menuName ="World/Tile", fileName="Tile")]
public class Tile_Profile : Tile
{

    public int layer;
    public Gradient colorOverElevation;
    public List<Sprite> sprites;

    public override void RefreshTile(Vector3Int location, ITilemap tilemap)
    {
        for (int tx = -1; tx <= 1; tx++) {
            for (int ty = -1; ty <= 1; ty++) {
                Vector3Int position = new Vector3Int(location.x + tx, location.y + ty, location.z);
                tilemap.RefreshTile(position);
            }
        }
    }

    public override void GetTileData(Vector3Int p, ITilemap tilemap, ref TileData tileData)
    {
        if (World.current == null) return;
        int index = 0;
        index += tilemap.GetTile<Tile_Profile>(new Vector3Int(p.x + 0, p.y + 1, 0)) == this ? 1 : 0;
        index += tilemap.GetTile<Tile_Profile>(new Vector3Int(p.x + 1, p.y + 0, 0)) == this ? 2 : 0;
        index += tilemap.GetTile<Tile_Profile>(new Vector3Int(p.x + 0, p.y - 1, 0)) == this ? 4 : 0;
        index += tilemap.GetTile<Tile_Profile>(new Vector3Int(p.x - 1, p.y + 0, 0)) == this ? 8 : 0;
        tileData.color = colorOverElevation.Evaluate(World.current.relativeElevation[p.x, p.y, layer]);
        tileData.sprite = sprites[index];
    }
}