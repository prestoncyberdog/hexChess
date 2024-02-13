using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mapGenerator : MonoBehaviour
{
    public gameManager gm;
    public battleManager bm;

    public float spacingFactor;
    public float tileScale;
    public int mapRadius;

    public tile[] objectiveLocations;
    public piece[] championLocations;

    public void init()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;

        spacingFactor = 1.1f;
        mapRadius = 3;
        setTileScale();
        spawnMap();//spawns tiles, not obstacles
        placeChampions();
    }

    public void setTileScale()
    {
        tileScale = 0.25f;
    }

    void Update()
    {
        
    }

    //randomly assigns tiles to each champion, making sure they are far enough apart
    public void placeChampions()
    {
        int tries = 0;
        do
        {
            resetMap();
            for (int i = 0; i < 2; i++)
            {
                tile place = bm.allTiles[Random.Range(0, bm.allTiles.Length)];
                if (place.thisPiece == null)
                {
                    gm.champions[i].thisTile = place;
                }
                else
                {
                    i--;
                }
            }
            tries++;
        } while (!isValidMap() && tries < 100);
        if (!isValidMap())
        {
            gm.loadMap();
        }
        for (int i = 0;i<2;i++)
        {
            bm.placeNewPiece(gm.champions[i],gm.champions[i].thisTile);
        }
        bm.em.copyHypoBoard();
        findDistsToChampions(true);
        findDistsToChampions(false);
        spawnChampionMarkers();
    }

    public bool isValidMap()
    {
        return findDistFromPlayerToEnemy() >= 3;//(mapRadius - 3) * 2;
    }

    //manually reset valiues in tile grid to allow rerandomization
    public void resetMap()
    {
        for (int i = 0;i<bm.allTiles.Length;i++)
        {
            bm.allTiles[i].thisPiece = null;
        }
    }

    public int findDistFromPlayerToEnemy()
    {
        tile activeTile;
        tile otherTile;
        Queue q = new Queue();
        bm.resetTiles();
        activeTile = gm.champions[0].thisTile;
        q.Enqueue(activeTile);
        activeTile.distance = 0;
        while (q.Count > 0)
        {
            activeTile = (tile)q.Dequeue();
            if (activeTile == gm.champions[1].thisTile)
            {
                return activeTile.distance;
            }
            for (int j = 0; j < activeTile.neighbors.Length; j++)
            {
                if (activeTile.neighbors[j] != null && activeTile.neighbors[j].distance > activeTile.distance + 1)
                {
                    otherTile = activeTile.neighbors[j];
                    q.Enqueue(otherTile);
                    otherTile.distance = activeTile.distance + 1;
                }
            }
        }
        return -1;
    }

    //finds the distances to each champion for each tile
    public void findDistsToChampions(bool real)
    {
        for (int i = 0;i<bm.allTiles.Length;i++)
        {
            if (real)
            {
                bm.allTiles[i].championDists = new int[2];
            }
            else
            {
                 bm.allTiles[i].hypoChampionDists = new int[2];
            } 
        }

        tile activeTile;
        tile otherTile;
        Queue q = new Queue();
        for (int i = 0; i < 2; i++)
        {
            bm.resetTiles();
            activeTile = gm.champions[i].thisTile;
            if (!real)
            {
                activeTile = gm.champions[i].hypoTile;
            }
            if (gm.champions[i].alive)
            {
                q.Enqueue(activeTile);
                activeTile.distance = 0;
            }
            while (q.Count > 0)
            {
                activeTile = (tile)q.Dequeue();
                if (real)
                {
                    activeTile.championDists[i] = activeTile.distance;
                }
                else
                {
                    activeTile.hypoChampionDists[i] = activeTile.distance;
                }
                for (int j = 0; j < activeTile.neighbors.Length; j++)
                {
                    if (activeTile.neighbors[j] != null && activeTile.neighbors[j].distance > activeTile.distance + 1)
                    {
                        otherTile = activeTile.neighbors[j];
                        q.Enqueue(otherTile);
                        otherTile.distance = activeTile.distance + 1;
                    }
                }
            }
        }
    }

    public void spawnChampionMarkers()
    {
        for (int i = 0;i<2;i++)
        {
            championMarker marker = Instantiate(gm.ChampionMarker, gm.AWAY, Quaternion.identity).GetComponent<championMarker>();
            marker.champ = gm.champions[i];
        }
    }

    public void spawnMap()
    {
        tile root = Instantiate(gm.Tile, new Vector3(3, 0, 0), Quaternion.identity).GetComponent<tile>();
        root.init(tileScale);
        root.distance = 0;
        Queue q = new Queue();
        q.Enqueue(root);
        tile activeTile;
        while (q.Count > 0)
        {
            activeTile = (tile)q.Dequeue();
            for (int i = 0; i < activeTile.neighbors.Length; i++)
            {
                //if neighbor exists already, do nothing
                //this depends on invariant that existing tiles will already be linked
                if (activeTile.neighbors[i] == null)
                {
                    float angle = (-60 * i + 60) * Mathf.Deg2Rad;//angle of next neighbor
                    float dist = 5 * Mathf.Pow(3, 0.5f) * 0.5f * tileScale * spacingFactor;//5 * (root 3)/2 * scale
                    Vector3 newPos = new Vector3(
                        activeTile.transform.position.x + Mathf.Cos(angle) * dist,
                        activeTile.transform.position.y + Mathf.Sin(angle) * dist,
                        activeTile.transform.position.z);
                    tile temp = Instantiate(gm.Tile, newPos, Quaternion.identity).GetComponent<tile>();
                    temp.init(tileScale);
                    temp.distance = activeTile.distance + 1;
                    if (temp.distance < mapRadius)
                    {
                        q.Enqueue(temp);
                    }
                }
            }
        }
        GameObject[] tileGameObjects = GameObject.FindGameObjectsWithTag("tile");
        bm.allTiles = new tile[tileGameObjects.Length];
        for (int i = 0; i < tileGameObjects.Length; i++)
        {
            bm.allTiles[i] = tileGameObjects[i].GetComponent<tile>();
        }
    }
}
