using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyManager : MonoBehaviour
{
    public gameManager gm;
    public battleManager bm;

    public List<piece> playerPieces;
    public List<piece> enemyPieces;
    public List<piece> spawnPlan;
    public List<piece> resummons;
    public List<piece> moveOrder;
    public List<piece> decideOrder; 
    public List<recursiveActionItem> stack;

    public int groupSize;//the number of units that can be considered together
    public float spawnDelay;
    public float spawnDelayMax;
    public float moveDelay;
    public float moveDelayMax;
    public float endTurnDelay;
    public float endTurnDelayMax;
    public bool readyToEnd;

    public float lastYieldTime;
    public float frameMax;
    public float maxTurnStallTime;
    public float currentTurnStallTime;
    public float lastFrameTime;
    public int turnGroupSize;

    public void init()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;

        groupSize = 3;//maximum recursive depth / max number of pieces considered at a time
        spawnDelayMax = 1f;//delay after each new piece placement
        moveDelayMax = .7f;//delay after each normal move
        endTurnDelayMax = 0.5f;//delay after last piece placement
        frameMax = 0.02f;//longest frame allowed for thinking, in seconds
        maxTurnStallTime = 2f;//max allowed total stalling time, after which group size will be reduced (repeatable within a turn)

        createChampion();
        moveOrder = new List<piece>();
        spawnPlan = new List<piece>();
        prepareSpawns();
    }

    void Update()
    {

    }

    public void takeTurn()
    {
        bm.changeTurn(1);
        copyHypoBoard();
        findAllPieces(true);
        prepareSpawns();
        StartCoroutine(decideActions());
        StartCoroutine(moveAllPieces());
    }

    //evaluates the hypothetical position
    //should also consider end of game?
    public float evaluatePosition()
    {
        //these values can balance how much the ai values different measures
        float championWeight = 2;//bonus multiplier on piece value
        float pieceWeight = 4;
        float targetingWeight = 0.4f;
        float positionWeight = 0.2f;
        float inactivePenalty = .1f;
        float randomizationWeight = 0.01f;
        findAllPieces(false);
        float value = (calculatePieceScore(pieceWeight) +
                       calculateTargetingScore(targetingWeight, pieceWeight) +
                       calculateChampionScore(championWeight, targetingWeight) +
                       calculatePositionScore(positionWeight, inactivePenalty)) *
                       (1 + (Random.Range(0, 10000) / 10000f) * randomizationWeight);
        //Debug.Log("position value: " + value);
        return value;
    }

    //adds up all living piece values for each player and considers the difference
    //considers piece value to be (1 + health/maxHealth)/2
    //assumes findAllPieces has already been called
    public float calculatePieceScore(float pieceWeight)
    {
        float pieceScore = 0;
        for (int i = 0; i < playerPieces.Count; i++)
        {
            if (!playerPieces[i].hypoAlive)
            {
                Debug.LogError("Evaluated position believing player piece was alive");
            }
            pieceScore -= playerPieces[i].value * (1 + (playerPieces[i].hypoHealth * 1f/ playerPieces[i].maxHealth)) * .5f;
        }
        for (int i = 0; i < enemyPieces.Count; i++)
        {
            if (!enemyPieces[i].hypoAlive)
            {
                Debug.LogError("Evaluated position believing enemy piece was alive");
            }
            pieceScore += enemyPieces[i].value * (1 + (enemyPieces[i].hypoHealth * 1f/ enemyPieces[i].maxHealth)) * .5f;
        }
        //Debug.Log("Piece score: " + pieceScore * pieceWeight);
        return pieceScore * pieceWeight;
    }

    //awards negative points to each enemy piece that is under attack, depending on whether it coud retaliate if attacked
    //may want to add consideration of the power of the attacker and defender
    //assumes findAllPieces has already been called
    public float calculateTargetingScore(float targetingWeight, float pieceWeight)
    {
        float targetingScore = 0;
        bool defended;
        bool canRetaliate;
        float retaliationModifier = 0.1f;//give less penalty when we hope to retaliate
        piece otherPiece;
        tile currentTile;
        tile landingTile;
        for (int i = 0; i < enemyPieces.Count; i++)
        {
            enemyPieces[i].potentialIncomingDamage = 0;
            enemyPieces[i].pieceTargetingPenalty = 0;
        }
        calculateAbilityTargeting(pieceWeight);
        for (int i = 0; i < enemyPieces.Count; i++)
        {
            currentTile = enemyPieces[i].hypoTile;
            defended = false;
            for (int j = 0;j<currentTile.abilityHypoTargetedBy.Count;j++)
            {
                if (currentTile.abilityHypoTargetedBy[j].team == 1 && currentTile.abilityHypoTargetedBy[j].abilityFearScore > 0)
                {
                    defended = true;
                    break;
                }
            }
            //consider each attacking piece
            for (int j = 0; j < currentTile.hypoTargetedBy.Count; j++)
            {
                otherPiece = currentTile.hypoTargetedBy[j];
                if (otherPiece.team == 1)
                {
                    defended = true;
                }
                else
                {
                    canRetaliate = false;
                    //check whether we can retaliate
                    landingTile = otherPiece.findEndingTile(enemyPieces[i], false);
                    for (int k = 0;k<landingTile.hypoTargetedBy.Count;k++)
                    {
                        if (landingTile.hypoTargetedBy[k].team == 1)
                        {
                            canRetaliate = true;
                            break;
                        }
                    }
                    for (int k = 0;k<landingTile.abilityHypoTargetedBy.Count;k++)
                    {
                        if (landingTile.abilityHypoTargetedBy[k].team == 1 && landingTile.abilityHypoTargetedBy[k].abilityFearScore > 0)
                        {
                            canRetaliate = true;
                            break;
                        }
                    }

                    if (canRetaliate)
                    {
                        enemyPieces[i].pieceTargetingPenalty += (enemyPieces[i].expectedDamage(otherPiece.damage) * 1f / enemyPieces[i].maxHealth) * .5f * enemyPieces[i].value * pieceWeight * retaliationModifier;
                    }
                    else
                    {
                        enemyPieces[i].pieceTargetingPenalty += (enemyPieces[i].expectedDamage(otherPiece.damage) * 1f / enemyPieces[i].maxHealth) * .5f * enemyPieces[i].value * pieceWeight;
                    }
                    enemyPieces[i].potentialIncomingDamage += enemyPieces[i].expectedDamage(otherPiece.damage);
                }
            }
            //check if total is enough to capture
            if (enemyPieces[i].potentialIncomingDamage > enemyPieces[i].hypoHealth)
            {
                    if (defended)
                    {
                        enemyPieces[i].pieceTargetingPenalty += enemyPieces[i].value * pieceWeight * retaliationModifier;
                    }
                    else
                    {
                        enemyPieces[i].pieceTargetingPenalty += enemyPieces[i].value * pieceWeight;
                    }
            }
            //update targeting score for this piece
            enemyPieces[i].pieceTargetingPenalty *= targetingWeight;
            enemyPieces[i].pieceTargetingPenalty = Mathf.Min(enemyPieces[i].pieceTargetingPenalty, enemyPieces[i].value * pieceWeight) * -1f;//penalty cannot be worse than the losing the piece
            targetingScore += Mathf.Min(enemyPieces[i].pieceTargetingPenalty, 0);//cannot be positive result
        }
        //Debug.Log("Targeting score: " + targetingScore);
        return targetingScore;
    }

    //checks each player piece for whether it has an activatable ability targeting each enemy piece
    //penalizes the enemy depending on the player's piece abilityFearScore
    //stores info in the pieces that are being targeted
    public void calculateAbilityTargeting(float pieceWeight)
    {
        tile targetedTile;
        for (int i = 0; i < playerPieces.Count; i++)
        {
            for (int j = 0; j < playerPieces[i].hypoAbilityTargets.Count; j++)
            {
                targetedTile = playerPieces[i].hypoAbilityTargets[j];
                if (playerPieces[i].isValidAbilityTarget(targetedTile, false) && targetedTile.hypoPiece != null && targetedTile.hypoPiece.team == 1)
                {
                    //here, the player's piece is targeting an enemy piece with an activatable ability
                    targetedTile.hypoPiece.pieceTargetingPenalty += (targetedTile.hypoPiece.expectedDamage(playerPieces[i].abilityFearScore) * 1f / targetedTile.hypoPiece.maxHealth) *
                                                                    .5f * targetedTile.hypoPiece.value * pieceWeight;
                    targetedTile.hypoPiece.potentialIncomingDamage += targetedTile.hypoPiece.expectedDamage(playerPieces[i].abilityFearScore);
                }
            }
        }
    }

    //considers whether both champions are alive and healthy
    //championWeight multiplies with the health fraction of each champion
    public float calculateChampionScore(float championWeight, float targetingWeight)
    {
        float champScore = 0;
        if (gm.champions[0] != null && gm.champions[0].hypoAlive)
        {
            champScore -= 10000;
            champScore -= championWeight * gm.champions[0].value * gm.champions[0].hypoHealth * 1f / gm.champions[0].maxHealth;
        }
        if (gm.champions[1] != null && gm.champions[1].hypoAlive)
        {
            champScore += 10000;
            champScore += championWeight * gm.champions[0].value * gm.champions[1].hypoHealth * 1f / gm.champions[1].maxHealth;
        }
        //include targeting onto enemy champion
        //potentialIncomingDamage has already been calculated by the calculateTargetingScore function
        if (gm.champions[1].potentialIncomingDamage >= gm.champions[1].hypoHealth)
        {
            champScore -= 8000;//lower because we still want the AI to kill this turn if it can
        }
        else
        {
            champScore -= championWeight * targetingWeight * gm.champions[1].value * gm.champions[1].potentialIncomingDamage * 1f / gm.champions[1].maxHealth;
        }

        //Debug.Log("Champ score: " + champScore);
        return champScore;
    }

    //awards points to each enemy piece based on its proximity to any objective the enemy doesnt already control
    //penalizes pieces slightly for not moving
    //assumes findAllPieces has already been called
    public float calculatePositionScore(float positionWeight, float inactivePenalty)
    {
        float positionScore = 0;
        for (int i = 0; i < enemyPieces.Count; i++)
        {
            positionScore -= Mathf.Abs(enemyPieces[i].hypoTile.hypoChampionDists[0] - enemyPieces[i].getDesiredRange());//penalize each piece for being farther than min range from the player's champion
            if (enemyPieces[i].inactive)
            {
                positionScore -= inactivePenalty;
            }
            if (enemyPieces[i].wastingAttackOnAlly)
            {
                Debug.LogError("Wasting attack on ally, which should not be possible");
                positionScore -= inactivePenalty * 3;//should basically never happen
            }
        }
        //should we also reward the ai for keeping its champion far away from the player's pieces? maybe with lower weight?
        //Debug.Log("Position score: " + positionScore * positionWeight);
        return positionScore * positionWeight;
    }

    //decide which pieces to place and setting their intentions, using hypo board after other moves are done
    /*public void decidePlacements()
    {
        prepareSpawns();
        bool stillPlacing = true;
        List<tile> spawnTiles = new List<tile>();
        for (int i = 0;i<bm.allTiles.Length;i++)
        {
            if (bm.allTiles[i].isValidPlacement(1,false))
            {
                spawnTiles.Add(bm.allTiles[i]);
            }
        }
        if (spawnTiles.Count == 0)
        {
            readyToEnd = true;
            return;
        }
        tile bestTile;
        float bestVal;
        float currentVal;
        while (bm.playsRemaining > 0 && stillPlacing)
        {
            piece currentPiece = spawnPlan[0];
            if (!currentPiece.canAfford())
            {
                stillPlacing = false;
                break;
            }
            spawnPlan.Remove(currentPiece);
            //choose tile
            bestTile = spawnTiles[0];
            bestVal = -10000;
            for (int i = 0;i<spawnTiles.Count;i++)
            {
                //place piece on hypo board
                currentPiece.placePiece(spawnTiles[i], false);
                currentPiece.useSummonAbility(false);
                //evaluate
                currentVal = evaluatePosition();
                if (currentVal > bestVal)
                {
                    bestVal = currentVal;
                    bestTile = spawnTiles[i];
                }
                //remove piece from hypo board (undo placement)
                reversableMove thisPlacement = new reversableMove(currentPiece);
                bm.undoMove(thisPlacement, false);

                //currentPiece.getCaptured(false);
                //List<piece> retargeted = new List<piece>();
                //retargeted.Add(currentPiece);
                //spawnTiles[i].updateTargeting(false, ref retargeted);
            }
            //place piece on hypo board
            //currentPiece.hypoAlive = true;
            currentPiece.placePiece(bestTile, false);
            currentPiece.useSummonAbility(false);
            currentPiece.payEnergyCost();//also pays summons cost
            //plan to place piece in move order
            moveOrder.Add(currentPiece);
            currentPiece.intention = bestTile;
            spawnTiles.Remove(bestTile);
        }
        readyToEnd = true; //allows piece moving coroutine to end turn when ready
    }*/

    public tile[] findSpawnTiles()
    {
        List<tile> spawnTilesList = new List<tile>();
        for (int i = 0;i<bm.allTiles.Length;i++)
        {
            if (bm.allTiles[i].isValidPlacement(1,false))
            {
                spawnTilesList.Add(bm.allTiles[i]);
            }
        }
        tile[] spawnTiles = new tile[spawnTilesList.Count];
        spawnTilesList.CopyTo(spawnTiles);
        return spawnTiles;
    }

    //determines the order in which the ai will consider its pieces
    //we will select the closest piece to an uncontrolled objective, then order other pieces by distance from the first
    public void orderPieces()
    {
        findAllPieces(false);
        decideOrder = new List<piece>();
        int minDist = 10000;;
        int currDist;
        piece firstPiece = null;
        for (int i = 0; i < resummons.Count; i++)//start by looking at resummons as our last choice
        {
            if (resummons[i].readyToSummon && !resummons[i].beingSummoned)
            {
                firstPiece = resummons[i];
                break;
            }
        }
        for (int i = 0; i < spawnPlan.Count; i++)//start by looking at new spawns as our next last choice
        {
            if (spawnPlan[i].readyToSummon && !spawnPlan[i].beingSummoned)
            {
                firstPiece = spawnPlan[i];
                break;
            }
        }
        //find piece that can move which is closest to the players champion
        for (int i = 0; i < enemyPieces.Count; i++)
        {
            if (enemyPieces[i].hypoExhausted == false && enemyPieces[i].inactive == false && enemyPieces[i].hypoAlive == true)
            {
                if (firstPiece == null)
                {
                    firstPiece = enemyPieces[i];
                }
                currDist = enemyPieces[i].hypoTile.hypoChampionDists[0];
                if (currDist < minDist)
                {
                    minDist = currDist;
                    firstPiece = enemyPieces[i];
                }
            }
        }
        if (firstPiece == null)//all existing and new pieces have made decisions
        {
            return;
        }
        decideOrder.Add(firstPiece);

        if (firstPiece.hypoTile != null)//if firstPiece is new placement, no need to search out other pieces to move
        {
            Queue q = new Queue();
            bm.resetTiles();
            tile activeTile = firstPiece.hypoTile;
            tile otherTile;
            q.Enqueue(activeTile);
            activeTile.distance = 0;
            while (q.Count > 0 && decideOrder.Count < turnGroupSize)//no need to prepare more than our group size if we are refilling after each piece anyway
            {
                activeTile = (tile)q.Dequeue();
                if (activeTile.hypoPiece != null && 
                    activeTile.hypoPiece.team == 1 && 
                    activeTile.hypoPiece.hypoExhausted == false && 
                    activeTile.hypoPiece.inactive == false && 
                    activeTile.hypoPiece.hypoAlive == true &&
                    !decideOrder.Contains(activeTile.hypoPiece))
                {
                    //here we have a nearby enemy(ai controlled) piece which has not yet moved/decided
                    decideOrder.Add(activeTile.hypoPiece);
                }

                for (int i = 0; i < activeTile.neighbors.Length; i++)
                {
                    if (activeTile.neighbors[i] != null)
                    {
                        otherTile = activeTile.neighbors[i];
                        if (otherTile.distance > activeTile.distance + 1)
                        {
                            q.Enqueue(otherTile);
                            otherTile.distance = activeTile.distance + 1;
                        }
                    }
                }
            }
        }
        //now, add new spawns to the end of the order if needed
        for (int i = 0; i < spawnPlan.Count; i++)
        {
            if (decideOrder.Count >= turnGroupSize)
            {
                break;
            }
            if (spawnPlan[i].readyToSummon && !spawnPlan[i].beingSummoned)
            {
                decideOrder.Add(spawnPlan[i]);
            }
        }
        //now, add new resummons to the end of the order if needed
        for (int i = 0; i < resummons.Count; i++)
        {
            if (decideOrder.Count >= turnGroupSize)
            {
                break;
            }
            if (resummons[i].readyToSummon && !resummons[i].beingSummoned)
            {
                decideOrder.Add(resummons[i]);
            }
        }
    }

    public IEnumerator moveAllPieces()
    {
        lastFrameTime = Time.realtimeSinceStartup;
        currentTurnStallTime = 0;
        endTurnDelay = endTurnDelayMax;
        while (!bm.playersTurn)
        {
            if (bm.movingPieces == 0)
            {
                if (spawnDelay > 0)
                {
                    spawnDelay -= Time.deltaTime;
                }
                else if (moveDelay > 0)
                {
                    moveDelay -= Time.deltaTime;
                }
                else if (moveOrder.Count > 0)
                {
                    movePiece(moveOrder[0]);
                    moveOrder.RemoveAt(0);
                }
                else if (readyToEnd && endTurnDelay > 0)
                {
                    endTurnDelay -= Time.deltaTime;
                }
                else if (readyToEnd)
                {
                    checkHypoBoard();
                    bm.changeTurn(0);
                }
                else
                {
                    //here we are stalling
                    currentTurnStallTime += (Time.realtimeSinceStartup - lastFrameTime);
                    if (currentTurnStallTime > maxTurnStallTime && turnGroupSize > 1)
                    {
                        //if we've stalled too long, reduce group size for the rest of the turn
                        turnGroupSize--;
                        currentTurnStallTime = 0;
                    }
                }
            }
            lastFrameTime = Time.realtimeSinceStartup;
            yield return null;
        }
    }

    public void movePiece(piece currentPiece)
    {
        if (currentPiece.alive)//moving existing piece
        {
            if (currentPiece.activatingAbility)
            {
                currentPiece.useActivatedAbility(currentPiece.intentions[0], true);
                bm.resetHighlighting();
                if (moveOrder.Count >  1 || !readyToEnd)
                {
                    moveDelay = moveDelayMax;
                }
            }
            else if (currentPiece.intentions[0] != currentPiece.thisTile)
            {
                currentPiece.moveToTile(currentPiece.intentions[0], true);
                StartCoroutine(currentPiece.moveTowardsNewTile());
                if (moveOrder.Count >  1 || !readyToEnd)
                {
                    moveDelay = moveDelayMax;
                }
            }
        }
        else //placing new piece
        {
            if (moveOrder.Count >  1 || !readyToEnd)//its not our final placement
            {
                spawnDelay = spawnDelayMax;
            }
            bm.placeNewPiece(currentPiece, currentPiece.intentions[0]);
            spawnPlan.Remove(currentPiece);
            resummons.Remove(currentPiece);
        }
        currentPiece.intentions.RemoveAt(0);
    }

    //sets hypo board to match real board
    public void copyHypoBoard()
    {
        findAllPieces(true);//fill bm.allPieces first
        for (int i = 0; i < bm.allPieces.Count; i++)
        {
            bm.allPieces[i].hypoAlive = bm.allPieces[i].alive;
            bm.allPieces[i].hypoHealth = bm.allPieces[i].health;
            bm.allPieces[i].hypoTile = bm.allPieces[i].thisTile;
            bm.allPieces[i].turnStartTile = bm.allPieces[i].thisTile;
            //copy targets
            bm.allPieces[i].hypoTargets = new List<tile>();
            bm.allPieces[i].hypoAbilityTargets = new List<tile>();
            copyTileList(bm.allPieces[i].targets, bm.allPieces[i].hypoTargets);
            copyTileList(bm.allPieces[i].abilityTargets, bm.allPieces[i].hypoAbilityTargets);
        }
        for (int i = 0; i < bm.allTiles.Length; i++)
        {
            bm.allTiles[i].hypoPiece = bm.allTiles[i].thisPiece;
            bm.allTiles[i].hypoObstacle = bm.allTiles[i].obstacle;
            //copy targeted by
            bm.allTiles[i].hypoTargetedBy = new List<piece>();
            bm.allTiles[i].abilityHypoTargetedBy = new List<piece>();
            copyPieceList(bm.allTiles[i].targetedBy, bm.allTiles[i].hypoTargetedBy);
            copyPieceList(bm.allTiles[i].abilityTargetedBy, bm.allTiles[i].abilityHypoTargetedBy);
        }
        bm.generator.findDistsToChampions(false);
    }

    //checks if hypo board matches real board and reports if it doesn't
    public void checkHypoBoard()
    {
        for (int i = 0; i < bm.alivePieces.Count; i++)
        {
            if (!(bm.alivePieces[i].hypoAlive == bm.alivePieces[i].alive &&
                  bm.alivePieces[i].hypoHealth == bm.alivePieces[i].health &&
                  bm.alivePieces[i].hypoTile == bm.alivePieces[i].thisTile))
            {
                Debug.LogError("Hypo piece " + i + "  does not match real piece");
            }
        }
        for (int i = 0; i < bm.allTiles.Length; i++)
        {
            if (!(bm.allTiles[i].hypoPiece == bm.allTiles[i].thisPiece &&
                  bm.allTiles[i].hypoObstacle == bm.allTiles[i].obstacle))
            {
                Debug.LogError("Hypo tile " + i + "  does not match real tile");
            }
            for (int j = 0;j<bm.allTiles[i].championDists.Length;j++)
            {
                if (bm.allTiles[i].championDists[j] != bm.allTiles[i].hypoChampionDists[j])
                {
                    Debug.LogError("Hypo champion dists do not match real champion dists");
                }
            }
        }
        checkHypoBoardTargets();
    }

    public void checkHypoBoardTargets()
    {
        for (int i = 0; i < bm.alivePieces.Count; i++)
        {
            for (int j = 0;j< bm.alivePieces[i].targets.Count; j++)
            {
                if (!bm.alivePieces[i].hypoTargets.Contains(bm.alivePieces[i].targets[j]) || 
                    bm.alivePieces[i].targets.Count != bm.alivePieces[i].hypoTargets.Count)
                {
                    Debug.LogError("Hypo piece  " + i + " targeting does not match real piece targeting");
                }
            }

            for (int j = 0;j< bm.alivePieces[i].abilityTargets.Count; j++)
            {
                if (!bm.alivePieces[i].hypoAbilityTargets.Contains(bm.alivePieces[i].abilityTargets[j]) || 
                    bm.alivePieces[i].abilityTargets.Count != bm.alivePieces[i].hypoAbilityTargets.Count)
                {
                    Debug.LogError("Hypo piece  " + i + " ability targeting does not match real piece targeting");
                }
            }
        }
        for (int i = 0; i < bm.allTiles.Length; i++)
        {
            for (int j = 0;j< bm.allTiles[i].targetedBy.Count; j++)
            {
                if (!bm.allTiles[i].hypoTargetedBy.Contains(bm.allTiles[i].targetedBy[j]) ||
                    bm.allTiles[i].targetedBy.Count != bm.allTiles[i].hypoTargetedBy.Count)
                {
                    Debug.LogError("Hypo tile " + i + " targeting does not match real tile targeting");
                }
            }
            for (int j = 0;j< bm.allTiles[i].abilityTargetedBy.Count; j++)
            {
                if (!bm.allTiles[i].abilityHypoTargetedBy.Contains(bm.allTiles[i].abilityTargetedBy[j]) ||
                    bm.allTiles[i].abilityTargetedBy.Count != bm.allTiles[i].abilityHypoTargetedBy.Count)
                {
                    Debug.LogError("Hypo tile " + i + " ablility targeting does not match real tile targeting");
                }
            }
        }
    }

    //finds all pieces on real board or hypo board
    //fills playersPieces and enemyPieces regardless, only fills bm.alivePieces when run on real board
    public void findAllPieces(bool real)
    {
        GameObject[] pieceGameObjects = GameObject.FindGameObjectsWithTag("piece");
        piece currentPiece;
        playerPieces = new List<piece>();
        enemyPieces = new List<piece>();
        if (real)
        {
            bm.alivePieces = new List<piece>();
            bm.allPieces = new List<piece>();
        }
        for (int i = 0;i < pieceGameObjects.Length;i++)
        {
            currentPiece = pieceGameObjects[i].GetComponent<piece>();
            if (real)
            {
                bm.allPieces.Add(currentPiece);
            }
            if (currentPiece.gameObject != null && ((real && currentPiece.alive) || (!real && currentPiece.hypoAlive)))
            {
                if (real)
                {
                    bm.alivePieces.Add(currentPiece);
                }
                if (currentPiece.team == 0)
                {
                    playerPieces.Add(currentPiece);
                }
                else if (currentPiece.team == 1)
                {
                    enemyPieces.Add(currentPiece);
                }
            }
        }
    }

    //unexhaust all pieces 
    public void resetPieces()
    {
        for (int i = 0;i< bm.alivePieces.Count;i++)
        {
            bm.alivePieces[i].exhausted = false;
            bm.alivePieces[i].hypoExhausted = false;
            if (!bm.playersTurn)//only clear the AI variables before using so they can be seen while debugging
            {
                bm.alivePieces[i].inactive = false;
                bm.alivePieces[i].wastingAttackOnAlly = false;
                bm.alivePieces[i].usedActivatedAbility = false;
                bm.alivePieces[i].activatingAbility = false;
                bm.alivePieces[i].readyToSummon = false;
                bm.alivePieces[i].beingSummoned = false;
            }
            bm.alivePieces[i].setColor();
            if (bm.alivePieces[i].intentions.Count != 0)
            {
                Debug.LogError("Piece failed to complete all intended actions.");
                bm.alivePieces[i].intentions = new List<tile>();
            }
        }
        for (int i = 0;i < spawnPlan.Count;i++)
        {
            spawnPlan[i].readyToSummon = false;
            spawnPlan[i].beingSummoned = false;
        }
        for (int i = 0;i < resummons.Count;i++)
        {
            resummons[i].readyToSummon = false;
            resummons[i].beingSummoned = false;
        }
    }

    public void copyTileList(List<tile> original, List<tile> copy)
    {
        for (int i = 0;i<original.Count;i++)
        {
            copy.Add(original[i]);
        }
    }

    public void copyPieceList(List<piece> original, List<piece> copy)
    {
        for (int i = 0; i < original.Count; i++)
        {
            copy.Add(original[i]);
        }
    }

    public void createChampion()
    {
        piece newPiece = Instantiate(gm.Pieces[gm.chooseRandomChampion()], new Vector3(1000, 1000, 0), Quaternion.identity).GetComponent<piece>();
        newPiece.team = 1;
        newPiece.init();
        newPiece.champion = true;
        gm.champions[1] = newPiece;
    }

    //refills list of pieces to spawn soon
    //marks pieces as ready so spawn if we have enough energy and summons to play them this turn
    public void prepareSpawns()
    {
        for (int i = spawnPlan.Count; i < 10; i++)
        {
            piece newPiece = Instantiate(gm.Pieces[Random.Range(0, gm.Pieces.Length)], new Vector3(1000, 1000, 0), Quaternion.identity).GetComponent<piece>();
            newPiece.team = 1;
            newPiece.init();
            spawnPlan.Add(newPiece);
        }

        int j = 0;
        float currEnergy = bm.enemyEnergy;
        int summonsRemaining = bm.playsRemaining; 
        while(j < spawnPlan.Count && summonsRemaining > 0 && currEnergy >= spawnPlan[j].cost)//gives up after first piece which it can't afford, so AI will save up and not skip
        {
            spawnPlan[j].readyToSummon = true;
            spawnPlan[j].beingSummoned = false;
            currEnergy -= spawnPlan[j].cost;
            summonsRemaining--;
            j++;
        }
        j = 0;
        while(j < resummons.Count && summonsRemaining > 0 && currEnergy >= resummons[j].cost)
        {
            resummons[j].readyToSummon = true;
            resummons[j].beingSummoned = false;
            currEnergy -= resummons[j].cost;
            summonsRemaining--;
            j++;
        }
    }

    //each piece must recieve an intention tile and the pieces will be ordered in moveOrder
    //this function is reworked to be not recursive so that it can be a coroutine and yield each frame
    public IEnumerator decideActions()
    {
        readyToEnd = false;
        turnGroupSize = groupSize;
        lastYieldTime = Time.realtimeSinceStartup;
        moveOrder = new List<piece>();
        orderPieces();
        while (decideOrder.Count > 0)//choose a single piece action each loop
        {
            //create stack 
            stack = new List<recursiveActionItem>();
            recursiveActionItem current = new recursiveActionItem();
            current.pieceIndex = -1;
            current.bestVal = -1000000;
            stack.Insert(0, current);

            while (stack.Count > 0)
            {
                current = stack[0];
                //update indices
                current.tileIndex++;//advance inner (tile) loop
                if (current.abilityListCopy != null && current.abilityListCopy.Length > 0 && current.tileIndex >= current.targetListCopy.Length)
                {
                    current.abilityIndex++;
                }

                if (current.targetListCopy == null || (current.tileIndex >= current.targetListCopy.Length && (current.abilityIndex >= current.abilityListCopy.Length ||
                                                                                                              current.abilityListCopy.Length == 0)))//here we dont have a piece to analyze
                {
                    do //while this piece is not suitable, find a piece that isnt exhausted or dead, or is ready to summon and not yet being summoned
                    {
                        current.pieceIndex += 1;//advance outer (piece) loop
                    } while (current.pieceIndex < decideOrder.Count && current.pieceIndex < turnGroupSize && 
                            ((decideOrder[current.pieceIndex].hypoExhausted || !decideOrder[current.pieceIndex].hypoAlive) && 
                             (!decideOrder[current.pieceIndex].readyToSummon || decideOrder[current.pieceIndex].beingSummoned)));
                    if (current.pieceIndex < decideOrder.Count && current.pieceIndex < turnGroupSize)//here we have a new piece to analyze
                    {
                        if (decideOrder[current.pieceIndex].readyToSummon && !decideOrder[current.pieceIndex].beingSummoned)
                        {
                            current.tileIndex = 0;
                            current.targetListCopy = findSpawnTiles();
                            current.abilityListCopy = new tile[0];
                        }
                        else
                        {
                            current.tileIndex = -1;//start at -1 to consider not moving first
                            current.targetListCopy = new tile[decideOrder[current.pieceIndex].hypoTargets.Count];
                            decideOrder[current.pieceIndex].hypoTargets.CopyTo(current.targetListCopy);
                            current.abilityIndex = -1;//start at -1 so we can increment to 0 before using
                            current.abilityListCopy = new tile[decideOrder[current.pieceIndex].hypoAbilityTargets.Count];
                            decideOrder[current.pieceIndex].hypoAbilityTargets.CopyTo(current.abilityListCopy);
                        }
                    }
                }
                //consider going up
                if (!(current.pieceIndex < decideOrder.Count && current.pieceIndex < turnGroupSize) || stack.Count > turnGroupSize)
                {
                    if (current.bestVal == -1000000)//we did not manage to recurse on any other pieces
                    {
                        current.bestVal = evaluatePosition();
                    }
                    goUpLevel(current);
                }
                else
                {
                    goDownLevel(current);
                }
                //yield if we've spent too long thinking
                if (Time.realtimeSinceStartup - lastYieldTime > frameMax)
                {
                    lastYieldTime = Time.realtimeSinceStartup;
                    yield return null;
                }
            }
            orderPieces();
        }
        readyToEnd = true;
        //decidePlacements();
    }

    //moves up one level in position evaluation, updating stack item above and reverting hypo move
    public void goUpLevel(recursiveActionItem current)
    {
        if (stack.Count == 1)//here we can't go up so we return our result
        {
            if (current.bestPiece == null)//we found no legal moves at all, probably no legal tiles to summon
            {
                while (decideOrder.Count > 0)//make sure none of these pieces are considered for actions again
                {
                    decideOrder[0].hypoExhausted = true;
                    decideOrder[0].beingSummoned = true;
                    decideOrder[0].inactive = true;
                    decideOrder.RemoveAt(0);
                }
                stack.RemoveAt(0);
                Debug.Log("No legal actions in decideActions");
                return;
            }

            current.attackedPiece = null;
            //update hypo board to include new move
            makeHypoMove(current);

            moveOrder.Add(current.bestPiece);
            current.bestPiece.intentions.Add(current.bestTile);
            current.bestPiece.activatingAbility = current.usingAbility;
            decideOrder.Remove(current.bestPiece);

            stack.RemoveAt(0);
            return;
        }
        recursiveActionItem above = stack[1];
        //evaluate result of move
        if (current.bestVal > above.bestVal)
        {
            above.bestVal = current.bestVal;
            above.bestPiece = decideOrder[above.pieceIndex];
            if (above.tileIndex == -1)//piece inactive
            {
                above.bestTile = decideOrder[above.pieceIndex].hypoTile;
                above.usingAbility = false;
            }
            else if (above.abilityListCopy != null && above.abilityListCopy.Length > 0 && above.tileIndex >= above.targetListCopy.Length)
            {
                above.bestTile = above.abilityListCopy[above.abilityIndex];
                above.usingAbility = true;
            }
            else
            {
                above.bestTile = above.targetListCopy[above.tileIndex];
                above.usingAbility = false;
            }
        }

        //undo hypo move
        if (decideOrder[above.pieceIndex].readyToSummon)
        {
            above.previousTile = null;//so that undo move knows to undo placement
        }
        reversableMove thisMove = new reversableMove(decideOrder[above.pieceIndex], above.previousTile, above.attackedPiece, above.capturedPiece);
        bm.undoMove(thisMove, false);
        above.capturedPiece = null;
        above.attackedPiece = null;

        //remove level from stack
        stack.RemoveAt(0);
    }

    public void goDownLevel(recursiveActionItem current)
    {
        if (decideOrder[current.pieceIndex].readyToSummon)
        {
            //summon hypo piece
            if (!(current.tileIndex < current.targetListCopy.Length && current.targetListCopy[current.tileIndex].isValidPlacement(1,false)))
            {
                return;
            }
            makeHypoMove(current);
        }
        else if (current.tileIndex == -1)//piece inactive
        {
            //make hypo non-move
            decideOrder[current.pieceIndex].hypoExhausted = true;
            decideOrder[current.pieceIndex].inactive = true;
            current.previousTile = decideOrder[current.pieceIndex].hypoTile;
        }
        else if (current.abilityListCopy != null && current.abilityListCopy.Length > 0 && current.tileIndex >= current.targetListCopy.Length)//here we've already considered all normal moves
        {
            if (!(decideOrder[current.pieceIndex].isValidAbilityTarget(current.abilityListCopy[current.abilityIndex], false)))
            {
                //current.previousTile = decideOrder[current.pieceIndex].hypoTile;
                return;//not a valid ability target, do nothing
            }
            makeHypoMove(current);
        }
        else
        {
            if (!(decideOrder[current.pieceIndex].isValidCandidate(current.targetListCopy[current.tileIndex], false)))
            {
                return;//not a valid move, do nothing
            }
            makeHypoMove(current);
        }

        //add new level to stack
        recursiveActionItem below = new recursiveActionItem();
        below.pieceIndex = -1;
        below.bestVal = -1000000;
        stack.Insert(0, below);
    }


    public void makeHypoMove(recursiveActionItem current)
    {
        if (current.tileIndex >= current.targetListCopy.Length && (current.abilityIndex >= current.abilityListCopy.Length ||
                                                                   current.abilityListCopy.Length == 0))//no more moves to hypo, we need to make the best one for the real plan
        {
            makeBestHypoMove(current);
            return;
        }
        else if (decideOrder[current.pieceIndex].readyToSummon)//summon new piece
        {
            decideOrder[current.pieceIndex].beingSummoned = true;
            decideOrder[current.pieceIndex].placePiece(current.targetListCopy[current.tileIndex], false);
            decideOrder[current.pieceIndex].useSummonAbility(false);
            return;
        }
        else if (current.abilityListCopy != null && current.abilityListCopy.Length > 0 && current.tileIndex >= current.targetListCopy.Length)//use ability
        {
            //current.previousTile = decideOrder[current.pieceIndex].hypoTile;//needed to avoid undo treating this as a placement
            decideOrder[current.pieceIndex].useActivatedAbility(current.abilityListCopy[current.abilityIndex], false);
            decideOrder[current.pieceIndex].usedActivatedAbility = true;
            return;
        }
        else
        {
            current.previousTile = decideOrder[current.pieceIndex].hypoTile;
            if (current.targetListCopy[current.tileIndex].hypoPiece != null && 
                current.targetListCopy[current.tileIndex].hypoPiece.willGetCaptured(decideOrder[current.pieceIndex], false))//here we are capturing a piece
            {
                current.capturedPiece = current.targetListCopy[current.tileIndex].hypoPiece;
                current.capturedPiece.getCaptured(false);
            }
            else if (current.targetListCopy[current.tileIndex].hypoPiece != null)//here, we are attacking but not capturing another piece
            {
                current.attackedPiece = current.targetListCopy[current.tileIndex].hypoPiece;
            }
            decideOrder[current.pieceIndex].moveToTile(current.targetListCopy[current.tileIndex], false);
            if (current.attackedPiece != null)
            {
                decideOrder[current.pieceIndex].dealDamage(current.attackedPiece, decideOrder[current.pieceIndex].damage, false);
                decideOrder[current.pieceIndex].wastingAttackOnAlly = (decideOrder[current.pieceIndex].attackHasNoEffect(current.attackedPiece, decideOrder[current.pieceIndex].damage) &&
                                                                       current.attackedPiece.team == decideOrder[current.pieceIndex].team);
            }
            if (current.capturedPiece != null)//after move so we can unexhaust for the bouncer
            {
                decideOrder[current.pieceIndex].useKillAbility(false);
            }
        }
    }

    public void makeBestHypoMove(recursiveActionItem current)
    {
        if (current.bestPiece.readyToSummon)//summon new piece
        {
            current.bestPiece.readyToSummon = false;
            current.bestPiece.beingSummoned = true;
            current.bestPiece.placePiece(current.bestTile, false);
            current.bestPiece.useSummonAbility(false);
            current.bestPiece.payEnergyCost();//also pays summons cost
            return;
        }
        if (current.usingAbility)//use ability
        {
            current.bestPiece.useActivatedAbility(current.bestTile, false);
            current.bestPiece.usedActivatedAbility = true;
            return;
        }

        bool captured = false;
        if (current.bestTile.hypoPiece != null && 
            current.bestTile.hypoPiece != current.bestPiece &&
            current.bestTile.hypoPiece.willGetCaptured(current.bestPiece, false))//here we are capturing a piece
        {
            captured = true;
            current.bestTile.hypoPiece.getCaptured(false);
        }
        else if (current.bestTile.hypoPiece != null && 
                current.bestTile.hypoPiece != current.bestPiece)//here, we are attacking but not capturing another piece
        {
            current.attackedPiece = current.bestTile.hypoPiece;
        }
        else if(current.bestTile == current.bestPiece.hypoTile)//here we aren't moving
        {
            current.bestPiece.inactive = true;
        }
        current.bestPiece.moveToTile(current.bestTile, false);//we must move after capturing but before dealing damage
        if (current.attackedPiece != null)
        {
            current.bestPiece.dealDamage(current.attackedPiece, current.bestPiece.damage, false);
            current.bestPiece.wastingAttackOnAlly = (current.bestPiece.attackHasNoEffect(current.attackedPiece, current.bestPiece.damage) &&
                                                     current.attackedPiece.team == current.bestPiece.team);
            current.attackedPiece = null;
        }
        if (captured)
        {
            current.bestPiece.useKillAbility(false);
        }
        current.bestPiece.pushedPieces = null;//shouldnt matter
    }
}

public class recursiveActionItem
{
    public float bestVal;
    public piece bestPiece;
    public tile bestTile;
    public int pieceIndex;//index into decideOrder
    public int tileIndex;//index into targetListCopy
    public tile[] targetListCopy;
    public int abilityIndex;//index into abilityListCopy
    public tile[] abilityListCopy;
    public bool usingAbility;
    public piece capturedPiece;
    public piece attackedPiece;
    public tile previousTile;
}