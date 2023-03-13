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

    public void init()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;

        spacingFactor = 1.1f;
        mapRadius = 7;
        tileScale = 0.15f;
        spawnMap();//spawns tiles, not obstacles
        spawnObjectives();
        linkTilesToObjectives();//to be done only after map fully determined
    }

    void Update()
    {
        
    }

    public void spawnObjectives()
    {
        bm.allObjectives = new objective[5];
        for (int i = 0;i<bm.allObjectives.Length;i++)
        {
            tile place = bm.allTiles[Random.Range(0, bm.allTiles.Length)];
            if (place.thisObjective == null)
            {
                objective newObjective = Instantiate(gm.Objective, place.transform.position, Quaternion.identity).GetComponent<objective>();
                newObjective.thisTile = place;
                place.thisObjective = newObjective;
                if (i == 0 || i == 1)
                {
                    newObjective.team = i;
                }
                else
                {
                    newObjective.team = -1;
                }
                newObjective.init();
                bm.allObjectives[i] = newObjective;
            }
        }
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
