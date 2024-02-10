﻿using System.Collections;
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
    public float value;
    public int cost;
    public float qualityBonus;
    public int team;
    public bool champion;
    public tile intention;//used by AI

    public tile thisTile;
    public bool exhausted;
    public bool alive;
    public List<tile> targets;
    public tile hypoTile;
    public bool hypoExhausted;
    public bool hypoAlive;
    public List<tile> hypoTargets;

    public int health;
    public int damage;
    public int hypoHealth;
    public healthBar thisHealthBar;

    public teamSlot thisSlot;

    public tile newTile;
    public tile oldTile;
    public tile turnStartTile;
    public float moveRate;
    public piece capturing;
    public piece attacking;
    public bool notMoving;
    public List<tile> stepPath;

    public void init()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;
        moveRate = 5 * (2f - team);
        playerColor = new Color(0.2f, 0.2f, 1f);
        exhaustedPlayerColor = playerColor;// new Color(0.4f, 0.4f, 1f);
        enemyColor = new Color(1f, 0.2f, 0.2f);
        exhaustedEnemyColor = enemyColor;// new Color(1f, 0.4f, 0.4f);

        cost = 1;
        health = 5;
        damage = 3;
        champion = false;
        exhausted = false;
        if (thisTile != null)
        {
            alive = true;
            bm.alivePieces.Add(this);
        }
        specificInit();
        value = cost + qualityBonus;
        transform.localScale = new Vector3(transform.localScale.x * bm.generator.tileScale, transform.localScale.y * bm.generator.tileScale, 1);

        updateTargeting(true);
        setColor();
    }

    void Update()
    {

    }

    public virtual void specificInit()
    {
        //do nothing by default
    }

    public virtual void specificUpdate()
    {
        //do nothing by default
    }

    //does not include exhausting new pieces
    public void placePiece(tile targetTile, bool real)
    {
        if (real)
        {
            alive = true;
            newTile = targetTile;
            thisTile = targetTile;
            thisTile.thisPiece = this;
            if (thisSlot != null)
            {
                thisSlot.thisPiece = null;
                thisSlot = null;
            }
            if (thisHealthBar == null)
            {
                thisHealthBar = Instantiate(gm.HealthBar, gm.AWAY, Quaternion.identity).GetComponent<healthBar>();
                thisHealthBar.owner = this;
            }
        }
        else //here its a move on the hypo board
        {
            hypoAlive = true;
            hypoTile = targetTile;
            hypoTile.hypoPiece = this;
        }
        updateTargeting(real);
        List<piece> retargeted = new List<piece>();
        retargeted.Add(this);
        targetTile.updateTargeting(real, ref retargeted);
        bm.resetHighlighting();
    }

    //begins moving to new tile, updates location and targeting info
    public void moveToTile(tile targetTile, bool real)
    {
        tile possibleOldTile;
        if (real)
        {
            if (targetTile.thisPiece != null && targetTile.thisPiece != this && !targetTile.thisPiece.willGetCaptured(this, real))
            {
                //here, we will deal damage but not capture, so our resulting tile will not be the original target tile
                attacking = targetTile.thisPiece;
                targetTile = findEndingTile(attacking, real);//thisTile;//to be replaced
            }
            else if (targetTile.thisPiece != null && targetTile.thisPiece != this)
            {
                capturing = targetTile.thisPiece;
            }
            possibleOldTile = thisTile;
            oldTile = possibleOldTile;
            exhausted = true;
            possibleOldTile.thisPiece = null;
            possibleOldTile.setColor();
            newTile = targetTile;
            thisTile = targetTile;
            thisTile.thisPiece = this;
            thisTile.setColor();
        }
        else //here its a move on the hypo board
        {
            if (targetTile.hypoPiece != null && targetTile.hypoPiece != this && !targetTile.hypoPiece.willGetCaptured(this, real))
            {
                //here, we will deal damage but not capture, so our resulting tile will not be the original target tile
                //attacking = targetTile.hypoPiece;
                targetTile = findEndingTile(targetTile.hypoPiece, real);//hypoTile;//to be replaced
                //enemy manager should handle the hypothetical damage and capturing
            }
            else if (targetTile.hypoPiece != null && targetTile.hypoPiece != this)
            {
                //capturing = targetTile.hypoPiece;
            }
            possibleOldTile = hypoTile;
            hypoExhausted = true;
            possibleOldTile.hypoPiece = null;
            hypoTile = targetTile;
            hypoTile.hypoPiece = this;
        }
        if (champion)
        {
            bm.generator.findDistsToChampions(real);
        }
        updateTargeting(real);
        List<piece> retargeted = new List<piece>();
        retargeted.Add(this);
        possibleOldTile.updateTargeting(real, ref retargeted);
        targetTile.updateTargeting(real, ref retargeted);
    }

    //allows us to delay coloring changes, captures, and sound effects, our final location is already changed in moveToTile
    public IEnumerator moveTowardsNewTile()
    {
        bm.movingPiece = this;
        stepPath = new List<tile>();
        if (attacking != null)
        {
            stepPath.Add(attacking.thisTile);
        }
        if (moveType == JUMP || moveType == LINE)
        {
            stepPath.Add(newTile);
        }
        else
        {
            findStepPath(newTile);
        }
        while (stepPath.Count > 0)
        {
            Vector3 toNextTile = stepPath[0].transform.position - transform.position;
            if (moveRate * Time.deltaTime > toNextTile.magnitude)//here, we've arrived
            {
                if (attacking != null && stepPath[0] == attacking.thisTile)
                {
                    dealDamage(attacking, true);
                    attacking = null;
                }
                stepPath.RemoveAt(0);
            }
            else
            {
                transform.position = transform.position + toNextTile.normalized * moveRate * Time.deltaTime;
                thisHealthBar.setPositions();
            }
            yield return null;
        }
        arriveOnTile();
        bm.movingPiece = null;
    }

    //take care of delayed effects of move like coloring, capturing, and sound effects. Damage/capturing happens here
    public void arriveOnTile()
    {
        transform.position = newTile.transform.position;
        newTile = null;
        oldTile = null;
        setColor();
        if (capturing != null)
        {
            capturing.getCaptured(true);
            capturing = null;
        }
        bm.resetHighlighting();
        thisHealthBar.setPositions();
    }

    //finds the tile we'd end up on if we attacked target
    public tile findEndingTile(piece target, bool real)
    {
        tile startTile = thisTile;
        if (!real)
        {
            startTile = hypoTile;
        }
        if (moveType != LINE)
        {
            return startTile;
        }
        for (int i = 0;i < 6;i++)
        {
            if (target.isInDirection(this, i, real))
            {
                if (real)
                {
                    return target.thisTile.neighbors[i];
                }
                else
                {
                    return target.hypoTile.neighbors[i];
                }
            }
        }
        return startTile;
    }

    public void dealDamage(piece target, bool real)
    {
        target.takeDamage(damage, real);
    }

    public void takeDamage(int amount, bool real)
    {
        if (real)
        {
            health -= amount;
            thisHealthBar.setColors();
        }
        else
        {
            hypoHealth -= amount;
        }
    }

    //this reverses our hypothetical attack
    public void unDealDamage(piece target)
    {
        target.hypoHealth += damage;
    }

    public bool isValidCandidate(tile target, bool real)
    {
        if (real)
        {
            return ((target.thisPiece == null || target.thisPiece.team != team) &&
                    target.obstacle == 0 &&
                    exhausted == false);
        }
        else
        {
            return ((target.hypoPiece == null || target.hypoPiece.team != team) &&
                    target.hypoObstacle == 0 &&
                    hypoExhausted == false);
        }
    }

    //returns whether an attack will capture this piece
    public bool willGetCaptured(piece attacker, bool real)
    {
        return (( real && attacker.damage >= health) ||
                (!real && attacker.damage >= hypoHealth));
    }

    //starts from this piece and looks in a direction for another piece, only looking along a straight line
    public bool isInDirection(piece otherPiece, int dir, bool real)
    {
        tile currTile = thisTile;
        if (!real)
        {
            currTile = hypoTile;
        }
        while (currTile.neighbors[dir] != null)
        {
            currTile = currTile.neighbors[dir];
            if ((real && otherPiece.thisTile == currTile) ||
                (!real && otherPiece.hypoTile == currTile))
            {
                return true;
            }
        }
        return false;
    }

    public bool canAfford()
    {
        return ((team == 0 && bm.playerEnergy >= cost) || (team == 1 && bm.enemyEnergy >= cost));
    }

    public void payEnergyCost()
    {
        if (team == 0)
        {
            bm.playerEnergy -= cost;
        }
        else if (team == 1)
        {
            bm.enemyEnergy -= cost;
        }
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

    public void getCaptured(bool real)
    {
        if (real)
        {
            alive = false;
            updateTargeting(real);//removes all targeting
            bm.alivePieces.Remove(this);
            thisHealthBar.destroyAll();
            if (team == 0)
            {
                bm.em.playerPieces.Remove(this);
                cost = Mathf.Min(cost * 2, 999);
                moveToSlot(bm.um.findOpenSlot());
            }
            if (team == 1)
            {
                if (champion)//shouldnt matter once battle end is implemented
                {
                    thisTile.thisPiece = null;
                    thisTile = null;
                    bm.generator.findDistsToChampions(real);
                }
                bm.em.enemyPieces.Remove(this);
                Destroy(gameObject);
            }
        }
        else
        {
            hypoAlive = false;
            updateTargeting(real);//removes all hypoTargeting
        }
    }

    //moves piece to other slot, swapping if it was occupied
    public void moveToSlot(teamSlot newSlot)
    {
        if (thisSlot != null)
        {
            teamSlot oldSlot = thisSlot;
            oldSlot.thisPiece = newSlot.thisPiece;
            if (newSlot.thisPiece != null)
            {
                newSlot.thisPiece.thisSlot = oldSlot;
                newSlot.thisPiece.transform.position = oldSlot.transform.position;
            }
        }
        thisSlot = newSlot;
        newSlot.thisPiece = this;
        transform.position = newSlot.transform.position;
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
        if (!alive)
        {
            return;
        }
        thisTile.gameObject.GetComponent<SpriteRenderer>().color = thisTile.selectedColor;
        for (int i = 0;i< targets.Count;i++)
        {
            if (isValidCandidate(targets[i], true))
            {
                targets[i].gameObject.GetComponent<SpriteRenderer>().color = targets[i].candidateColor;
            }
        }
    }

    private void findAllCandidates(bool real)
    {
        if ((real && !alive) || (!real && !hypoAlive))
        {
            return;
        }
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
                                                               (!real && !hypoTargets.Contains(otherTile))))  // here, otherTile is a target we can maybe move to
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

    //breadth first search of tiles not recursing on unavailable tiles
    private void findStepPath(tile targetTile)
    {
        for (int i = 0; i < bm.allTiles.Length; i++)
        {
            bm.allTiles[i].previousTile = null;
            bm.allTiles[i].distance = 1000;
        }
        Queue q = new Queue();
        tile activeTile = oldTile;
        tile otherTile;
        q.Enqueue(activeTile);
        activeTile.distance = 0;
        while (q.Count > 0)
        {
            activeTile = (tile)q.Dequeue();
            if (activeTile == targetTile)  // here, we have found the path to our target
            {
                while (activeTile.previousTile != null)
                {
                    stepPath.Insert(0,activeTile);
                    activeTile = activeTile.previousTile;
                }
                return;
            }
            for (int i = 0; i < activeTile.neighbors.Length; i++)
            {
                if (activeTile.neighbors[i] != null)
                {
                    otherTile = activeTile.neighbors[i];
                    if (activeTile.distance < moveRange &&
                        otherTile.distance > activeTile.distance + 1 &&
                        (activeTile == oldTile || activeTile.thisPiece == null))
                    {
                        q.Enqueue(otherTile);
                        otherTile.distance = activeTile.distance + 1;
                        otherTile.previousTile = activeTile;
                    }
                }
            }
        }
    }
}
