using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tile : MonoBehaviour
{
    public gameManager gm;
    public battleManager bm;

    public Color defaultColor;
    public Color candidateColor;
    public Color selectedColor;

    public tile[] neighbors;
    public int distance;
    public int[] targeted;//stores weakest piece targeting tile for player in [0] and enemy in [1], gets reset during turn planning

    public float scale;

    public piece thisPiece;
    public int obstacle;//may be object instead later

    public void init(float tileScale)
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;

        scale = tileScale;
        neighbors = new tile[6];
        targeted = new int[2];
        transform.localScale = new Vector3(scale, scale, 1);
        defaultColor = new Color(0.8f, 0.8f, 0.8f);
        candidateColor = new Color(0.8f, 0.8f, 0.6f);
        selectedColor = new Color(0.8f, 0.8f, 0.4f);
        this.GetComponent<SpriteRenderer>().color = defaultColor;
        transform.Rotate(new Vector3(0, 0, 30));
        distance = 1000;
        targeted = new int[2];

        obstacle = 0;//for now

        findNeighbors();
    }

    void Update()
    {
        
    }

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (bm.selectedPiece != null && !bm.selectedPiece.exhausted && targeted[0] == bm.selectedPiece.value && bm.selectedPiece.team == 0 && bm.playersTurn)
            {
                if (thisPiece != null)
                {
                    bm.selectedPiece.capturing = thisPiece;
                }
                thisPiece = bm.selectedPiece;
                thisPiece.exhausted = true;
                thisPiece.newTile = this;
                thisPiece.thisTile.thisPiece = null;
                thisPiece.thisTile = this;
                bm.resetTiles(true);
            }
            if (thisPiece != null)
            {
                bm.selectedPiece = thisPiece;
                bm.justClicked = true;
                thisPiece.findAllCandidates(true);
                if (!thisPiece.exhausted && thisPiece.team == 0 && bm.playersTurn)
                {
                    bm.holdingPiece = true;
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (bm.selectedPiece != null && bm.holdingPiece && !bm.selectedPiece.exhausted && targeted[0] == bm.selectedPiece.value)
            {
                if (thisPiece != null)
                {
                    thisPiece.getCaptured();
                }
                bm.holdingPiece = false;
                thisPiece = bm.selectedPiece;
                thisPiece.transform.position = transform.position;
                thisPiece.exhausted = true;
                thisPiece.setColor();
                thisPiece.thisTile.thisPiece = null;
                thisPiece.thisTile = this;
                thisPiece.findAllCandidates(true);//resets highlighting and selects piece again
            }
        }
    }

    public void findNeighbors()
    {
        neighbors = new tile[6];
        float neighborRadius = 5 * scale;//should be times (root 3)/2 but this gives margin for error
        //wrong answers will be 1.5X farther
        GameObject[] tileGameObjects = GameObject.FindGameObjectsWithTag("tile");
        tile currentTile;
        for (int i = 0; i < tileGameObjects.Length; i++)//this finds neighbor tiles
        {
            currentTile = tileGameObjects[i].GetComponent<tile>();
            if ((transform.position - currentTile.transform.position).magnitude < neighborRadius && currentTile != this)
            {
                //here, we have a tile which is close by
                float x1 = transform.position.x;
                float y1 = transform.position.y;
                float x2 = currentTile.transform.position.x;
                float y2 = currentTile.transform.position.y;
                int direction = 0;
                if (x2 > x1 && y2 - y1 > scale / 5)//use scale/5 as margin for error
                {
                    //tile is up and right
                    direction = 0;
                }
                else if (x2 > x1 && y2 - y1 < -scale / 5)
                {
                    //tile is down and right
                    direction = 2;
                }
                else if (x2 > x1)
                {
                    //tile is straight right
                    direction = 1;
                }
                else if (x2 < x1 && y2 - y1 > scale / 5)
                {
                    //tile is up and left
                    direction = 5;
                }
                else if (x2 < x1 && y2 - y1 < -scale / 5)
                {
                    //tile is down and left
                    direction = 3;
                }
                else if (x2 < x1)
                {
                    //tile is straight left
                    direction = 4;
                }
                else
                {
                    Debug.Log("Tile tried to connect to neighbor with same X coordinate");
                }
                if (neighbors[direction] == null)
                {
                    neighbors[direction] = currentTile;
                    currentTile.neighbors[(direction + 3) % 6] = this;
                }
                else if (neighbors[direction] != currentTile)
                {
                    Debug.Log("Error in tile linking, extra neighbor detected");
                }
                //if these both fail, we are already linked correctly and do nothing
                //actually shouldn't happen, tiles only do this when first created
                //new tiles wil make links to pre-existing tiles so they won't find existing links
            }
        }
    }
}
