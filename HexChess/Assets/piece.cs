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

    public tile thisTile;
    public tile newTile;
    public float moveRate;
    public bool exhausted;

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
        team = 0;
        exhausted = false;
        specificInit();
    }

    void Update()
    {
        if (newTile != null)
        {
            moveToNewTile();
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

    public void moveToNewTile()
    {
        Vector3 toNextTile = newTile.transform.position - transform.position;
        if (moveRate * Time.deltaTime > toNextTile.magnitude)//here, we've arrived
        {
            transform.position = newTile.transform.position;
            newTile = null;
            setColor();
        }
        else
        {
            transform.position = transform.position + toNextTile.normalized * moveRate * Time.deltaTime;
        }
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

    public void findAllCandidates()
    {
        bm.resetTiles();
        thisTile.gameObject.GetComponent<SpriteRenderer>().color = thisTile.selectedColor;
        if (exhausted)
        {
            return;
        }
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
                        (otherTile.thisPiece == null || otherTile.thisPiece.team != team) &&
                        otherTile.obstacle == 0)
                    {
                        q.Enqueue(otherTile);
                        otherTile.distance = activeTile.distance + 1;
                        if (otherTile.distance <= moveRange) // here, otherTile is a candidate we can move to
                        {
                            otherTile.targeted[0] = value;
                            otherTile.gameObject.GetComponent<SpriteRenderer>().color = otherTile.candidateColor;
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
                        if (otherTile.distance == moveRange &&
                             (otherTile.thisPiece == null || otherTile.thisPiece.team != team) &&
                             otherTile.obstacle == 0) // here, otherTile is a candidate we can move to
                        {
                            otherTile.targeted[0] = value;
                            otherTile.gameObject.GetComponent<SpriteRenderer>().color = otherTile.candidateColor;
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
                        (otherTile.thisPiece == null || otherTile.thisPiece.team != team) &&
                        otherTile.obstacle == 0)
                    {
                        continueSearch = true;
                        otherTile.distance = activeTile.distance + 1;
                        if (otherTile.distance <= moveRange) // here, otherTile is a candidate we can move to
                        {
                            otherTile.targeted[0] = value;
                            otherTile.gameObject.GetComponent<SpriteRenderer>().color = otherTile.candidateColor;
                        }
                        activeTile = otherTile;
                    }
                }
            }
            continueSearch = true;
        }
    }
}
