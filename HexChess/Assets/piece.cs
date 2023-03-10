using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class piece : MonoBehaviour
{
    public gameManager gm;
    public battleManager bm;

    public const int STEP = 0;
    public const int LINE = 1;
    public const int JUMP = 2;

    public Color playerColor;
    public Color exhaustedPlayerColor;
    public Color enemyColor;
    public Color exhaustedEnemyColor;

    public int moveType;
    public int moveRange;
    public int value;
    public int team;
    public bool alive;
    public tile intention;//used by AI

    public tile thisTile;
    public tile newTile;
    public float moveRate;
    public bool exhausted;
    public piece capturing;
    public List<tile> targets;

    public void init()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;
        moveRate = 5f;
        playerColor = new Color(0.2f, 0.2f, 1f);
        exhaustedPlayerColor = new Color(0.4f, 0.4f, 1f);
        enemyColor = new Color(1f, 0.2f, 0.2f);
        exhaustedEnemyColor = new Color(1f, 0.4f, 0.4f);
        setColor();

        value = 1;
        exhausted = false;
        alive = true;
        bm.allPieces.Add(this);
        specificInit();

        findAllCandidates();
    }

    void Update()
    {
        if (newTile != null)
        {
            moveTowardsNewTile();
        }
    }

    public virtual void specificInit()
    {
        //do nothing by default
    }

    public virtual void specificUpdate()
    {
        //do nothing by default
    }

    //begins moving to new tile, updates location and targeting info
    public void moveToTile(tile targetTile)
    {
        tile oldTile = thisTile;
        exhausted = true;
        oldTile.thisPiece = null;
        newTile = targetTile;
        thisTile = targetTile;
        thisTile.thisPiece = this;
        updateTargeting();
        oldTile.updateTargeting();
        thisTile.updateTargeting();
    }

    //take care of delayed effects of move like coloring, capturing, and sound effects
    public void arriveOnTile()
    {
        transform.position = newTile.transform.position;
        newTile = null;
        setColor();
        if (capturing != null)
        {
            capturing.getCaptured();
            capturing = null;
        }
        bm.resetHighlighting();
        highlightCandidates();
    }

    public void moveTowardsNewTile()
    {
        Vector3 toNextTile = newTile.transform.position - transform.position;
        if (moveRate * Time.deltaTime > toNextTile.magnitude)//here, we've arrived
        {
            arriveOnTile();
        }
        else
        {
            transform.position = transform.position + toNextTile.normalized * moveRate * Time.deltaTime;
        }
    }

    public bool isValidCandidate(tile target)
    {
        return ((target.thisPiece == null || target.thisPiece.team != team) &&
                target.obstacle == 0 &&
                exhausted == false);
    }

    //updates target list and targetedBy list for each affected tile
    public void updateTargeting()
    {
        while(targets.Count > 0)
        {
            targets[0].targetedBy.Remove(this);
            targets.RemoveAt(0);
        }
        findAllCandidates();
    }

    public void getCaptured()
    {
        alive = false;
        while (targets.Count > 0)
        {
            targets[0].targetedBy.Remove(this);
            targets.RemoveAt(0);
        }
        bm.allPieces.Remove(this);
        Destroy(gameObject);
    }

    public void setColor()
    {
        if (team == 0 && !exhausted)
        {
            this.GetComponent<SpriteRenderer>().color = playerColor;
        }
        else if (team == 0 && exhausted)
        {
            this.GetComponent<SpriteRenderer>().color = exhaustedPlayerColor;
        }
        else if (team == 1 && !exhausted)
        {
            this.GetComponent<SpriteRenderer>().color = enemyColor;
        }
        else if (team == 1 && exhausted)
        {
            this.GetComponent<SpriteRenderer>().color = exhaustedEnemyColor;
        }
    }

    public void highlightCandidates()
    {
        bm.resetHighlighting();
        thisTile.gameObject.GetComponent<SpriteRenderer>().color = thisTile.selectedColor;
        for (int i = 0;i< targets.Count;i++)
        {
            if (isValidCandidate(targets[i]))
            {
                targets[i].gameObject.GetComponent<SpriteRenderer>().color = targets[i].candidateColor;
            }
        }
    }

    public void findAllCandidates()
    {
        bm.resetTiles();
        targets = new List<tile>();
        if (moveType == STEP)
        {
            planPathsWithObtacles(); 
        }
        else if (moveType == JUMP)
        {
            planPathsWithoutObtacles();
        }
        else if (moveType == LINE)
        {
            planPathsInALine();
        }
    }
    


    //breadth first search of tiles not recursing on unavailable tiles
    public void planPathsWithObtacles()
    {
        Queue q = new Queue();
        q.Enqueue(thisTile);
        thisTile.distance = 0;
        tile activeTile;
        tile otherTile;
        while (q.Count > 0)
        {
            activeTile = (tile)q.Dequeue();
            for (int i = 0; i < activeTile.neighbors.Length; i++)
            {
                if (activeTile.neighbors[i] != null)
                {
                    otherTile = activeTile.neighbors[i];
                    if (activeTile.distance < moveRange && 
                        otherTile.distance > activeTile.distance + 1 && 
                        (activeTile == thisTile || activeTile.thisPiece == null))
                    {
                        q.Enqueue(otherTile);
                        otherTile.distance = activeTile.distance + 1;
                        if (otherTile.distance <= moveRange && !targets.Contains(otherTile)) // here, otherTile is a target we can maybe move to
                        {
                            otherTile.targetedBy.Add(this);
                            targets.Add(otherTile);
                        }
                    }
                }
            }
        }
    }

    //breadth first search of tiles that will recurse on unavailable tiles
    public void planPathsWithoutObtacles()
    {
        Queue q = new Queue();
        q.Enqueue(thisTile);
        thisTile.distance = 0;
        tile activeTile;
        tile otherTile;
        while (q.Count > 0)
        {
            activeTile = (tile)q.Dequeue();
            for (int i = 0; i < activeTile.neighbors.Length; i++)
            {
                if (activeTile.neighbors[i] != null)
                {
                    otherTile = activeTile.neighbors[i];
                    if (activeTile.distance < moveRange && otherTile.distance > activeTile.distance + 1)
                    {
                        q.Enqueue(otherTile);
                        otherTile.distance = activeTile.distance + 1;
                        if (otherTile.distance == moveRange && !targets.Contains(otherTile)) // here, otherTile is a target we can maybe move to
                        {
                            otherTile.targetedBy.Add(this);
                            targets.Add(otherTile);
                        }
                    }
                }
            }
        }
    }

    //depth first search of tiles that will only recurse in one direction at a time, blocked by unavailable tiles
    public void planPathsInALine()
    {
        thisTile.distance = 0;
        tile activeTile;
        tile otherTile;
        bool continueSearch = true;
        for (int i = 0; i < thisTile.neighbors.Length; i++)
        {
            activeTile = thisTile;
            while (continueSearch)
            {
                continueSearch = false;
                if (activeTile.neighbors[i] != null)
                {
                    otherTile = activeTile.neighbors[i];
                    if (activeTile.distance < moveRange &&
                        otherTile.distance > activeTile.distance + 1 &&
                        (activeTile == thisTile || activeTile.thisPiece == null))
                    {
                        continueSearch = true;
                        otherTile.distance = activeTile.distance + 1;
                        if (otherTile.distance <= moveRange && !targets.Contains(otherTile)) // here, otherTile is a targert we can maybe move to
                        {
                            otherTile.targetedBy.Add(this);
                            targets.Add(otherTile);
                        }
                        activeTile = otherTile;
                    }
                }
            }
            continueSearch = true;
        }
    }
}
