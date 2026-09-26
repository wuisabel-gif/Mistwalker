using UnityEngine;
using UnityEngine.Serialization;

public class PathTileCycler : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Tooltip("Terrains in travel order.")]
    [FormerlySerializedAs("terrains")]
    public Terrain[] tiles;

    [Header("Terrain Settings")]
    [FormerlySerializedAs("terrainLength")]
    public float tileLength = 100f;
    [FormerlySerializedAs("safeBackTerrains")]
    public int bufferTiles = 2;

    private int frontIndex;
    private int backIndex;

    void Start()
    {
        backIndex = 0;
        frontIndex = tiles.Length - 1;
    }

    void Update()
    {
        if (player == null || tiles == null || tiles.Length == 0)
            return;

        CycleAheadIfNeeded();
        CycleBehindIfNeeded();
    }

    void CycleAheadIfNeeded()
    {
        float backTileLimit = tiles[backIndex].transform.position.z + tileLength * bufferTiles;

        if (player.position.z > backTileLimit)
            MoveBackTileToFront();
    }

    void CycleBehindIfNeeded()
    {
        float frontTileLimit = tiles[frontIndex].transform.position.z - tileLength * bufferTiles;

        if (player.position.z < frontTileLimit)
            MoveFrontTileToBack();
    }

    void MoveBackTileToFront()
    {
        Terrain recycledTile = tiles[backIndex];
        recycledTile.transform.position = tiles[frontIndex].transform.position + Vector3.forward * tileLength;

        frontIndex = backIndex;
        backIndex = (backIndex + 1) % tiles.Length;
    }

    void MoveFrontTileToBack()
    {
        Terrain recycledTile = tiles[frontIndex];
        recycledTile.transform.position = tiles[backIndex].transform.position - Vector3.forward * tileLength;

        backIndex = frontIndex;
        frontIndex = (frontIndex - 1 + tiles.Length) % tiles.Length;
    }
}
