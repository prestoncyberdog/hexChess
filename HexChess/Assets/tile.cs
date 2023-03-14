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
    public Color exhaustedColor;

    public tile[] neighbors;
    public float scale;
    public int distance;

    public List<piece> targetedBy;//stores all pieces targeting tile
    public piece thisPiece;
    public int obstacle;//may be object instead later
    public objective thisObjective;
    public List<piece> hypoTargetedBy;
    public piece hypoPiece;
    public int hypoObstacle;

    public objective[] objectives;
    public int[] objectiveDists;


    public void init(float tileScale)
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;

        scale = tileScale;
        neighbors = new tile[6];
        transform.localScale = new Vector3(scale, scale, 1);
        defaultColor = new Color(0.8f, 0.8f, 0.8f);
        candidateColor = new Color(0.8f, 0.8f, 0.6f);
        selectedColor = new Color(0.8f, 0.8f, 0.4f);
        exhaustedColor = new Color(0.7f, 0.7f, 0.7f);
        this.GetComponent<SpriteRenderer>().color = defaultColor;
        transform.Rotate(new Vector3(0, 0, 30));
        distance = 1000;
        targetedBy = new List<piece>();
        hypoTargetedBy = new List<piece>();

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
            if (bm.selectedPiece != null && bm.selectedPiece.alive && !bm.selectedPiece.exhausted && targetedBy.Contains(bm.selectedPiece) && bm.selectedPiece.team == 0 && bm.playersTurn &&
                bm.selectedPiece.isValidCandidate(this, true))//moving piece
            {
                bm.selectedPiece.moveToTile(this, true);
                StartCoroutine(bm.selectedPiece.moveTowardsNewTile());
                bm.resetTiles();
                bm.resetHighlighting();
            }
            else if(bm.selectedPiece != null && !bm.selectedPiece.alive && bm.playersTurn && isValidPlacement(0, true) && bm.selectedPiece.canAfford())//placing piece
            {
                bm.placeNewPiece(bm.selectedPiece, this);
                bm.selectedPiece.payEnergyCost();
            }
            if (thisPiece != null)
            {
                bm.selectedPiece = thisPiece;
                bm.justClicked = true;
                bm.resetHighlighting();
                if (!thisPiece.exhausted && thisPiece.team == 0 && bm.playersTurn)
                {
                    bm.holdingPiece = true;
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (bm.selectedPiece != null && bm.holdingPiece && bm.selectedPiece.alive && !bm.selectedPiece.exhausted && targetedBy.Contains(bm.selectedPiece) &&
                bm.selectedPiece.isValidCandidate(this, true))//moving piece
            {
                if (thisPiece != null)
                {
                    thisPiece.getCaptured();
                }
                bm.holdingPiece = false;
                bm.selectedPiece.moveToTile(this, true);
                bm.selectedPiece.arriveOnTile();
                bm.resetHighlighting();
            }
            else if (bm.selectedPiece != null && bm.holdingPiece && !bm.selectedPiece.alive && bm.playersTurn && isValidPlacement(0, true) && bm.selectedPiece.canAfford())//placing piece
            {
                bm.placeNewPiece(bm.selectedPiece, this);
                bm.holdingPiece = false;
                bm.selectedPiece.payEnergyCost();
            }
        }
    }

    //updates targeting for each piece in targetedBy list
    public void updateTargeting(bool real)
    {
        piece[] oldPieces; 
        if (real)
        {
            oldPieces = new piece[targetedBy.Count];
            targetedBy.CopyTo(oldPieces);//copy list so it won't change while we are trying to loop through it
        }
        else
        {
            oldPieces = new piece[hypoTargetedBy.Count];
            hypoTargetedBy.CopyTo(oldPieces);
        }

        for (int i = 0;i<oldPieces.Length;i++)
        {
            if (oldPieces[i].moveType != piece.JUMP && oldPieces[i].moveRange > 1)//jump or single move targeting is unaffected
            {
                oldPieces[i].updateTargeting(real);
            }
        }
    }

    public bool isValidPlacement(int team, bool real)//returns whether placing a piece here is possible right now
    {
        if (real)
        {
            return (obstacle == 0 && thisPiece == null && thisObjective == null && bm.playsRemaining > 0 && neighborsObjective(team, real));
        }
        return (hypoObstacle == 0 && hypoPiece == null && thisObjective == null && bm.playsRemaining > 0 && neighborsObjective(team, real));
    }

    public bool neighborsObjective(int team, bool real)//returns whether tile neighbors objective controlled by the given team
    {
        for (int i = 0; i < objectives.Length; i++)
        {
            if (objectiveDists[i] == 1 && ((real && objectives[i].team == team) ||
                                          (!real && objectives[i].hypoTeam == team)))
            {
                return true;
            }
        }
        return false;
    }

    //updates nearby objectives if needed
    public void checkNearbyObjectives(bool real)
    {
        for (int i = 0; i<objectives.Length;i++)
        {
            if (objectiveDists[i] == 1)
            {
                objectives[i].checkStatus(real);
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
