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
    public bool exhausted;
    public List<tile> targets;
    public tile hypoTile;
    public bool hypoExhausted;
    public List<tile> hypoTargets;

    public tile newTile;
    public float moveRate;
    public piece capturing;

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

        updateTargeting(true);
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
    public void moveToTile(tile targetTile, bool real)
    {
        tile oldTile = thisTile;
        if (real)
        {
            exhausted = true;
            oldTile.thisPiece = null;
            newTile = targetTile;
            thisTile = targetTile;
            thisTile.thisPiece = this;
        }
        else //here its a move on the hypo board
        {
            hypoExhausted = true;
            oldTile.hypoPiece = null;
            hypoTile = targetTile;
            hypoTile.hypoPiece = this;
        }
        updateTargeting(real);
        oldTile.updateTargeting(real);
        targetTile.updateTargeting(real);
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
    public void updateTargeting(bool real)
    {
        if (real)
        {
            while (targets.Count > 0)
            {
                targets[0].targetedBy.Remove(this);
                targets.RemoveAt(0);
            }
        }
        else
        {
            while (hypoTargets.Count > 0)
            {
                hypoTargets[0].hypoTargetedBy.Remove(this);
                hypoTargets.RemoveAt(0);
            }
        }
        findAllCandidates(real);
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

    private void findAllCandidates(bool real)
    {
        bm.resetTiles();
        if (real)
        {
            targets = new List<tile>();
        }
        else
        {
            hypoTargets = new List<tile>();
        }

        if (moveType == STEP)
        {
            planPathsWithObtacles(real); 
        }
        else if (moveType == JUMP)
        {
            planPathsWithoutObtacles(real);
        }
        else if (moveType == LINE)
        {
            planPathsInALine(real);
        }
    }



    //breadth first search of tiles not recursing on unavailable tiles
    private void planPathsWithObtacles(bool real)
    {
        Queue q = new Queue();
        tile activeTile = hypoTile;
        tile otherTile;
        if (real)
        {
            activeTile = thisTile;
        }
        q.Enqueue(activeTile);
        activeTile.distance = 0;
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
                            ((real && (activeTile == thisTile || activeTile.thisPiece == null)) ||
                            (!real && (activeTile == hypoTile || activeTile.hypoPiece == null))))
                    {
                        q.Enqueue(otherTile);
                        otherTile.distance = activeTile.distance + 1;
                        if (otherTile.distance <= moveRange && ((real && !targets.Contains(otherTile)) ||
                                                                (!real && !hypoTargets.Contains(otherTile)))) // here, otherTile is a target we can maybe move to
                        {
                            if (real)
                            {
                                otherTile.targetedBy.Add(this);
                                targets.Add(otherTile);
                            }
                            else
                            {
                                otherTile.hypoTargetedBy.Add(this);
                                hypoTargets.Add(otherTile);
                            }
                        }
                    }
                }
            }
        }
    }

    //breadth first search of tiles that will recurse on unavailable tiles
    private void planPathsWithoutObtacles(bool real)
    {
        Queue q = new Queue();
        tile activeTile = hypoTile;
        tile otherTile;
        if (real)
        {
            activeTile = thisTile;
        }
        q.Enqueue(activeTile);
        activeTile.distance = 0;
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
                        if (otherTile.distance == moveRange && ((real && !targets.Contains(otherTile)) ||
                                                                (!real && !hypoTargets.Contains(otherTile)))) // here, otherTile is a target we can maybe move to
                        {
                            if (real)
                            {
                                otherTile.targetedBy.Add(this);
                                targets.Add(otherTile);
                            }
                            else
                            {
                                otherTile.hypoTargetedBy.Add(this);
                                hypoTargets.Add(otherTile);
                            }
                        }
                    }
                }
            }
        }
    }

    //depth first search of tiles that will only recurse in one direction at a time, blocked by unavailable tiles
    private void planPathsInALine(bool real)
    {
        tile startingTile = hypoTile;
        if (real)
        {
            startingTile = thisTile;
        }
        startingTile.distance = 0;
        tile activeTile;
        tile otherTile;
        bool continueSearch = true;
        for (int i = 0; i < startingTile.neighbors.Length; i++)
        {
            activeTile = startingTile;
            while (continueSearch)
            {
                continueSearch = false;
                if (activeTile.neighbors[i] != null)
                {
                    otherTile = activeTile.neighbors[i];
                    if (activeTile.distance < moveRange &&
                        otherTile.distance > activeTile.distance + 1 &&
                            ((real && (activeTile == thisTile || activeTile.thisPiece == null)) ||
                            (!real && (activeTile == hypoTile || activeTile.hypoPiece == null))))
                    {
                        continueSearch = true;
                        otherTile.distance = activeTile.distance + 1;
                        if (otherTile.distance <= moveRange && ((real && !targets.Contains(otherTile)) ||
                                                                (!real && !hypoTargets.Contains(otherTile)))) // here, otherTile is a target we can maybe move to
                        {
                            if (real)
                            {
                                otherTile.targetedBy.Add(this);
                                targets.Add(otherTile);
                            }
                            else
                            {
                                otherTile.hypoTargetedBy.Add(this);
                                hypoTargets.Add(otherTile);
                            }
                        }
                        activeTile = otherTile;
                    }
                }
            }
            continueSearch = true;
        }
    }
}
