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

    public void init()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;

        spacingFactor = 1.1f;
        mapRadius = 7;
        tileScale = 0.15f;
        spawnMap();//spawns tiles, not obstacles
        spawnObjectives(5);
        linkTilesToObjectives();//to be done only after map fully determined
    }

    void Update()
    {
        
    }

    public void spawnObjectives(int numObjectives)
    {
        bm.allObjectives = new objective[numObjectives];
        for (int i = 0;i< numObjectives; i++)
        {
            objective newObjective = Instantiate(gm.Objective, gm.AWAY, Quaternion.identity).GetComponent<objective>();
            bm.allObjectives[i] = newObjective;
            if (i == 0 || i == 1)
            {
                newObjective.team = i;
            }
            else
            {
                newObjective.team = -1;
            }
            newObjective.init();
        }
        chooseRandomObjectiveLocations(numObjectives);
    }

    public void chooseRandomObjectiveLocations(int numObjectives)
    {
        objective newObjective;
        int tries = 0;
        do
        {
            resetMap();
            objectiveLocations = new tile[numObjectives];
            for (int i = 0; i < numObjectives; i++)
            {
                tile place = bm.allTiles[Random.Range(0, bm.allTiles.Length)];
                if (isValidObjectiveLocation(place))
                {
                    newObjective = bm.allObjectives[i];
                    newObjective.thisTile = place;
                    newObjective.transform.position = place.transform.position;
                    place.thisObjective = newObjective;
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
    }

    public bool isValidMap()
    {
        return findDistFromPlayerToEnemy() >= (mapRadius - 3) * 2;
    }

    //manually reset valiues in tile grid to allow rerandomization
    public void resetMap()
    {
        for (int i = 0;i<bm.allTiles.Length;i++)
        {
            bm.allTiles[i].thisObjective = null;
        }
    }

    public bool isValidObjectiveLocation(tile potentialTile)
    {
        if (potentialTile.thisObjective != null)
        {
            return false;
        }
        for (int i = 0;i<potentialTile.neighbors.Length;i++)
        {
            if (potentialTile.neighbors[i] == null || potentialTile.neighbors[i].thisObjective != null)
            {
                return false;
            }
        }
        return true;
    }

    public int findDistFromPlayerToEnemy()
    {
        tile activeTile;
        tile otherTile;
        Queue q = new Queue();
        bm.resetTiles();
        activeTile = bm.allObjectives[0].thisTile;
        q.Enqueue(activeTile);
        activeTile.distance = 0;
        while (q.Count > 0)
        {
            activeTile = (tile)q.Dequeue();
            if (activeTile == bm.allObjectives[1].thisTile)
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

    public void linkTilesToObjectives()
    {
        for (int i = 0;i<bm.allTiles.Length;i++)
        {
            bm.allTiles[i].objectives = new objective[bm.allObjectives.Length];
            bm.allTiles[i].objectiveDists = new int[bm.allObjectives.Length];
        }

        int objectiveIndex = 0;
        tile activeTile;
        tile otherTile;
        Queue q = new Queue();
        for (int i = 0; i < bm.allObjectives.Length; i++)
        {
            bm.resetTiles();
            activeTile = bm.allObjectives[i].thisTile;
            q.Enqueue(activeTile);
            activeTile.distance = 0;
            while (q.Count > 0 && objectiveIndex < bm.allObjectives.Length)
            {
                activeTile = (tile)q.Dequeue();
                activeTile.objectives[objectiveIndex] = bm.allObjectives[i];
                activeTile.objectiveDists[objectiveIndex] = activeTile.distance;
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
            objectiveIndex++;
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
