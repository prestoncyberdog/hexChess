using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class battleManager : MonoBehaviour
{
    public gameManager gm;

    public tile root;
    public tile[] allTiles;
    public float spacingFactor;
    public float tileScale;
    public int mapRadius;

    public piece selectedPiece;
    public bool holdingPiece;
    public bool justClicked;

    void Start()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        gm.bm = this;

        spacingFactor = 1.1f;
        mapRadius = 7;
        tileScale = 0.15f;
        spawnMap();

        //for now, manually force some pieces onto the board
        for (int i = 0;i<40;i++)
        {
            tile place = allTiles[Random.Range(0, allTiles.Length)];
            if (place.thisPiece == null)
            {
                piece newPiece = Instantiate(gm.Pieces[0], place.transform.position, Quaternion.identity).GetComponent<piece>();
                newPiece.thisTile = place;
                place.thisPiece = newPiece;
                newPiece.init();
            }
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !justClicked)
        {
            selectedPiece = null;
            resetTiles();
        }
        else
        {
            justClicked = false;//if a tile is clicked, it sets this to true
            //this means we can select/deselect pieces regardless of update order
        }

        if (holdingPiece)
        {
            selectedPiece.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0,0,10);
            if (Input.GetMouseButtonUp(0))
            {
                holdingPiece = false;
                selectedPiece.transform.position = selectedPiece.thisTile.transform.position;
            }
        }
    }

    //reset distance measures, availability, and highlighting for all tiles
    public void resetTiles()
    {
        for (int i = 0; i < allTiles.Length; i++)
        {
            tile temp = allTiles[i];
            temp.targeted = new int[2];
            temp.distance = 1000;
            temp.gameObject.GetComponent<SpriteRenderer>().color = temp.defaultColor;
        }
    }

    public tile findNearestTileToMouse()
    {
        tile nearest = allTiles[0];
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float minDist = (mousePos - nearest.transform.position).magnitude;
        for (int i = 0;i < allTiles.Length;i++)
        {
            if ((mousePos - allTiles[i].transform.position).magnitude < minDist)
            {
                nearest = allTiles[i];
                minDist = (mousePos - nearest.transform.position).magnitude;
            }
        }
        return nearest;
    }

    public void spawnMap()
    {
        root = Instantiate(gm.Tile, new Vector3(0, 0, 0), Quaternion.identity).GetComponent<tile>();
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
        allTiles = new tile[tileGameObjects.Length];
        for (int i = 0;i<tileGameObjects.Length;i++)
        {
            allTiles[i] = tileGameObjects[i].GetComponent<tile>();
        }
    }
}
