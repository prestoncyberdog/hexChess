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
    public List<piece> moveOrder;
    public List<piece> decideOrder; 
    public List<recursiveActionItem> stack;

    public int groupSize;//the number of units that can be considered together
    public float spawnDelay;
    public float spawnDelayMax;
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
        endTurnDelayMax = 0.5f;//delay after last piece placement
        frameMax = 0.1f;//longest frame allowed for thinking, is seconds, meant as a last resort
        maxTurnStallTime = 2f;//max allowed total stalling time, after which group size will be reduced (repeatable within a turn)

        moveOrder = new List<piece>();

        spawnPlan = new List<piece>();
        prepareSpawns();
    }

    void Update()
    {

    }

    public void takeTurn()
    {
        findAllPieces(true);
        bm.changeTurn(1);
        copyHypoBoard();
        StartCoroutine(decideActions());
        StartCoroutine(moveAllPieces());
    }

    //evaluates the hypothetical position
    //should also consider end of game?
    public float evaluatePosition()
    {
        //these values can balance how much the ai values different measures
        float objectiveWeight = 10;
        float pieceWeight = 1;
        float targetingWeight = 0.8f;
        float positionWeight = 0.2f;
        float notMovingPenalty = 0.05f;
        float randomizationWeight = 0.01f;
        findAllPieces(false);
        float value = calculateObjectiveScore(objectiveWeight) +
                      calculatePieceScore(pieceWeight) +
                      calculateTargetingScore(targetingWeight) +
                      calculatePositionScore(positionWeight, notMovingPenalty) +
                      (Random.Range(0, 10000) / 10000f) * randomizationWeight;
        return value;
    }

    //adds up number of objectives for each player and considers the difference
    public float calculateObjectiveScore(float objectiveWeight)
    {
        float objectiveScore = 0;
        int playerObjectives = 0;
        int enemyObjectives = 0;
        for (int i = 0; i < bm.allObjectives.Length; i++)
        {
            if (bm.allObjectives[i].hypoTeam == 0)
            {
                playerObjectives++;
            }
            else if (bm.allObjectives[i].hypoTeam == 1)
            {
                enemyObjectives++;
            }
        }
        //check for player defeat
        if (playerObjectives == 0 && playerPieces.Count == 0)
        {
            objectiveScore = 10000;//here, the ai has won
        }
        objectiveScore += (enemyObjectives - playerObjectives) * objectiveWeight;
        return objectiveScore;
    }

    //adds up all living piece values for each player and considers the difference
    //assumes findAllPieces has already been called
    public float calculatePieceScore(float pieceWeight)
    {
        float pieceScore = 0;
        for (int i = 0; i < playerPieces.Count; i++)
        {
            pieceScore -= playerPieces[i].value;
        }
        for (int i = 0; i < enemyPieces.Count; i++)
        {
            pieceScore += enemyPieces[i].value;
        }
        return pieceScore * pieceWeight;
    }

    //awards negative points to each enemy piece that is under attack, based on how strong it, its attacker, and its defender are
    //assumes findAllPieces has already been called
    public float calculateTargetingScore(float targetingWeight)
    {
        //our expectations are less reliable further in the future so these get progressiveley lower weights
        float attackerWeight = 0.8f;
        float defenderWeight = 0.6f;

        float targetingScore = 0;
        tile currentTile;
        piece otherPiece;
        float weakestAttacker;
        float weakestDefender;
        for (int i = 0; i < enemyPieces.Count; i++)
        {
            weakestAttacker = 1000;
            weakestDefender = 1000;
            currentTile = enemyPieces[i].hypoTile;
            for (int j = 0; j < currentTile.hypoTargetedBy.Count; j++)
            {
                otherPiece = currentTile.hypoTargetedBy[j];
                if (otherPiece.team == 0 && otherPiece.value < weakestAttacker)
                {
                    weakestAttacker = otherPiece.value;
                }
                else if (otherPiece.team == 1 && otherPiece.value < weakestDefender)
                {
                    weakestDefender = otherPiece.value;
                }
            }
            //if weaket attacker == 1000 then the piece is under attack so we add 0
            if (weakestAttacker < 1000 && weakestDefender == 1000)
            {
                targetingScore += -enemyPieces[i].value;//piece attacked and not defended
            }
            else if (weakestAttacker < 1000 && weakestDefender < 1000)
            {
                targetingScore += Mathf.Min((-enemyPieces[i].value + weakestAttacker * attackerWeight - weakestDefender * defenderWeight), 0);//cannot be positive result
            }
        }
        return targetingScore * targetingWeight;
    }

    //awards points to each enemy piece based on its proximity to any objective the enemy doesnt already control
    //penalizes pieces slightly for not moving
    //assumes findAllPieces has already been called
    public float calculatePositionScore(float positionWeight, float notMovingPenalty)
    {
        float positionScore = 0;
        float minDist = 1000;
        tile currentTile;
        for (int i = 0; i < enemyPieces.Count; i++)
        {
            currentTile = enemyPieces[i].hypoTile;
            minDist = 1000;
            for (int j = 0; j < currentTile.objectiveDists.Length; j++)
            {
                if (currentTile.objectives[j].hypoTeam != 1 && currentTile.objectiveDists[j] < minDist)
                {
                    minDist = currentTile.objectiveDists[j];
                }
            }
            positionScore += (1 / (1 + minDist));
            if (enemyPieces[i].turnStartTile == enemyPieces[i].hypoTile)
            {
                positionScore -= notMovingPenalty;
            }
        }
        return positionScore * positionWeight;
    }

    //decide which pieces to place and setting their intentions, using hypo board after other moves are done
    public void decidePlacements()
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
                currentPiece.hypoAlive = true;
                currentPiece.placePiece(spawnTiles[i], false);
                //evaluate
                currentVal = evaluatePosition();
                if (currentVal > bestVal)
                {
                    bestVal = currentVal;
                    bestTile = spawnTiles[i];
                }
                //remove piece from hypo board
                currentPiece.hypoAlive = false;
                currentPiece.updateTargeting(false);
                spawnTiles[i].updateTargeting(false);
            }
            //place piece on hypo board
            currentPiece.hypoAlive = true;
            currentPiece.placePiece(bestTile, false);
            currentPiece.payEnergyCost();
            //plan to place piece in move order
            moveOrder.Add(currentPiece);
            currentPiece.intention = bestTile;
            spawnTiles.Remove(bestTile);
            bm.playsRemaining--;
        }
        readyToEnd = true; //allows piece moving coroutine to end turn when ready
    }

    //determines the order in which the ai will consider its pieces
    //we will select the closest piece to an uncontrolled objective, then order other pieces by distance from the first
    public void orderPieces()
    {
        decideOrder = new List<piece>();
        float minDist = 1000;
        piece firstPiece = null;
        tile currentTile;
        //find piece that can move which is closest to useful objective
        for (int i = 0; i < enemyPieces.Count; i++)
        {
            if (enemyPieces[i].hypoExhausted == false && enemyPieces[i].notMoving == false)
            {
                if (firstPiece == null)
                {
                    firstPiece = enemyPieces[i];//will only be relevant if there are no useful objectives, in which case any piece is fine
                }
                currentTile = enemyPieces[i].hypoTile;
                for (int j = 0; j < currentTile.objectiveDists.Length; j++)
                {
                    if (currentTile.objectives[j].hypoTeam != 1 && currentTile.objectiveDists[j] < minDist)
                    {
                        minDist = currentTile.objectiveDists[j];
                        firstPiece = enemyPieces[i];
                    }
                }
            }
        }
        if (firstPiece == null)//all pieces have made decisions
        {
            return;
        }
        decideOrder.Add(firstPiece);

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
                activeTile.hypoPiece.exhausted == false && 
                activeTile.hypoPiece.notMoving == false && 
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

    public IEnumerator moveAllPieces()
    {
        lastFrameTime = Time.realtimeSinceStartup;
        currentTurnStallTime = 0;
        endTurnDelay = endTurnDelayMax;
        while (!bm.playersTurn)
        {
            if (bm.movingPiece == null)
            {
                if (spawnDelay > 0)
                {
                    spawnDelay -= Time.deltaTime;
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
            if (currentPiece.intention != currentPiece.thisTile)
            {
                currentPiece.moveToTile(currentPiece.intention, true);
                StartCoroutine(currentPiece.moveTowardsNewTile());
            }
        }
        else //placing new piece
        {
            if (moveOrder.Count >  1 || !readyToEnd)//its not our final placement
            {
                spawnDelay = spawnDelayMax;
            }
            bm.placeNewPiece(currentPiece, currentPiece.intention);
        }
    }

    public void copyHypoBoard()
    {
        for (int i = 0; i < bm.alivePieces.Count; i++)
        {
            bm.alivePieces[i].hypoAlive = bm.alivePieces[i].alive;
            bm.alivePieces[i].hypoExhausted = false;
            bm.alivePieces[i].hypoTile = bm.alivePieces[i].thisTile;
            bm.alivePieces[i].turnStartTile = bm.alivePieces[i].thisTile;
            //copy targets
            bm.alivePieces[i].hypoTargets = new List<tile>();
            copyTileList(bm.alivePieces[i].targets, bm.alivePieces[i].hypoTargets);
        }
        for (int i = 0; i < bm.allTiles.Length; i++)
        {
            bm.allTiles[i].hypoPiece = bm.allTiles[i].thisPiece;
            bm.allTiles[i].hypoObstacle = bm.allTiles[i].obstacle;
            //copy targeted by
            bm.allTiles[i].hypoTargetedBy = new List<piece>();
            copyPieceList(bm.allTiles[i].targetedBy, bm.allTiles[i].hypoTargetedBy);
        }
        for (int i = 0; i < bm.allObjectives.Length; i++)
        {
            bm.allObjectives[i].hypoTeam = bm.allObjectives[i].team;
        }
    }

    public void storeObjectiveHypoStatuses(recursiveActionItem currentMove)
    {
        currentMove.objectiveStatuses = new int[bm.allObjectives.Length];
        for (int i = 0; i< currentMove.objectiveStatuses.Length;i++)
        {
            currentMove.objectiveStatuses[i] = bm.allObjectives[i].hypoTeam;
        }
    }

    public void restoreObjectiveHypoStatuses(recursiveActionItem currentMove)
    {
        for (int i = 0; i < currentMove.objectiveStatuses.Length; i++)
        {
            bm.allObjectives[i].hypoTeam = currentMove.objectiveStatuses[i];
        }
    }

    //finds all living pieces on real board or hypo board
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
        }
        for (int i = 0;i < pieceGameObjects.Length;i++)
        {
            currentPiece = pieceGameObjects[i].GetComponent<piece>();
            if ((real && currentPiece.alive) || (!real && currentPiece.hypoAlive))
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
    public void unexhaustPieces()
    {
        for (int i = 0;i< bm.alivePieces.Count;i++)
        {
            bm.alivePieces[i].exhausted = false;
            bm.alivePieces[i].notMoving = false;
            bm.alivePieces[i].setColor();
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

    public void prepareSpawns()
    {
        if (spawnPlan.Count > bm.playsRemaining)
        {
            return;
        }
        for (int i = 0; i < 10; i++)
        {
            piece newPiece = Instantiate(gm.Pieces[Random.Range(0, gm.Pieces.Length)], new Vector3(1000, 1000, 0), Quaternion.identity).GetComponent<piece>();
            newPiece.team = 1;
            newPiece.init();
            spawnPlan.Add(newPiece);
        }
    }

    /*
    //each piece must recieve an intention tile and the pieces will be ordered in moveOrder
    public void decideActions()
    {
        moveOrder = new List<piece>();
        orderPieces();
        while (decideOrder.Count > 0)
        {
            //decideActionRecur(true);
            decideActionsNonRecursive();
            orderPieces();
        }
        decidePlacements();
    }
    
    //chooses and adds 1 action to the moveOrder list, removes relevant piece from decideOrder
    public float decideActionRecur(bool makeDecision)
    {
        float bestVal = -10000;
        float newVal;
        piece bestPiece = decideOrder[0];
        tile bestTile = bestPiece.hypoTile;
        piece capturedPiece = null;
        tile previousTile = null;
        tile[] hypoTargetCopy;

        for (int i = 0; i < decideOrder.Count && i < groupSize; i++)
        {
            if (!decideOrder[i].hypoExhausted)
            {
                //first, consider not moving
                decideOrder[i].hypoExhausted = true;
                newVal = decideActionRecur(false);//recurse, but don't allow recursions to make decisions
                if (newVal > bestVal)
                {
                    bestVal = newVal;
                    bestPiece = decideOrder[i];
                    bestTile = decideOrder[i].hypoTile;
                }
                decideOrder[i].hypoExhausted = false;
                //now, consider all possible moves
                hypoTargetCopy = new tile[decideOrder[i].hypoTargets.Count];
                decideOrder[i].hypoTargets.CopyTo(hypoTargetCopy);//use copy of this list so it won't change as we loop through it
                for (int j = 0; j < hypoTargetCopy.Length; j++)
                {
                    if (decideOrder[i].isValidCandidate(hypoTargetCopy[j], false))
                    {
                        //make hypo move
                        previousTile = decideOrder[i].hypoTile;
                        storeObjectiveHypoStatuses();
                        if (hypoTargetCopy[j].hypoPiece != null)
                        {
                            capturedPiece = hypoTargetCopy[j].hypoPiece;
                            capturedPiece.hypoAlive = false;
                        }
                        decideOrder[i].moveToTile(hypoTargetCopy[j], false);

                        //evaluate result of move
                        newVal = decideActionRecur(false);//recurse, but don't allow recursions to make decisions
                        if (newVal > bestVal)
                        {
                            bestVal = newVal;
                            bestPiece = decideOrder[i];
                            bestTile = hypoTargetCopy[j];
                        }

                        //undo hypo move
                        decideOrder[i].moveToTile(previousTile, false);
                        decideOrder[i].hypoExhausted = false;
                        decideOrder[i].capturing = null;
                        if (capturedPiece != null)
                        {
                            capturedPiece.placePiece(hypoTargetCopy[j], false);
                            capturedPiece.hypoAlive = true;
                        }
                        capturedPiece = null;
                        restoreObjectiveHypoStatuses();
                    }
                }
            }
        }

        if (makeDecision)//only the top level of the recursion can make actual move plans
        {

            //update hypo board to include new move
            if (bestTile.hypoPiece != null)
            {
                bestTile.hypoPiece.hypoAlive = false;
            }
            bestPiece.moveToTile(bestTile, false);

            moveOrder.Add(bestPiece);
            bestPiece.intention = bestTile;
            decideOrder.Remove(bestPiece);
        }

        if (bestVal == -10000)//we did not manage to recurse on any other pieces
        {
            bestVal = evaluatePosition();
        }
        return bestVal;
    }*/

    //each piece must recieve an intention tile and the pieces will be ordered in moveOrder
    //this function is reworked to be not recursive so that it can be a coroutine and yield each frame
    public IEnumerator decideActions()
    {
        readyToEnd = false;
        turnGroupSize = groupSize - 1;//set lower so first decision can be made quickly
        lastYieldTime = Time.realtimeSinceStartup;
        moveOrder = new List<piece>();
        orderPieces();
        turnGroupSize++;//set to full value because we can think while pieces move now
        while (decideOrder.Count > 0)//choose a single piece action each loop
        {
            //create stack 
            stack = new List<recursiveActionItem>();
            recursiveActionItem current = new recursiveActionItem();
            current.pieceIndex = -1;
            current.bestVal = -10000;
            stack.Insert(0, current);

            while (stack.Count > 0)
            {
                current = stack[0];
                //update indices
                current.tileIndex++;//advance inner (tile) loop
                if (current.targetListCopy == null || current.tileIndex >= current.targetListCopy.Length)
                {
                    do
                    {
                        current.pieceIndex += 1;//advance outer (piece) loop
                    } while (current.pieceIndex < decideOrder.Count && current.pieceIndex < turnGroupSize && decideOrder[current.pieceIndex].hypoExhausted);//find a piece that isnt exhausted
                    if (current.pieceIndex < decideOrder.Count && current.pieceIndex < turnGroupSize)
                    {
                        current.tileIndex = -1;//start at -1 to consider not moving first
                        current.targetListCopy = new tile[decideOrder[current.pieceIndex].hypoTargets.Count];
                        decideOrder[current.pieceIndex].hypoTargets.CopyTo(current.targetListCopy);
                    }
                }
                //consider going up
                if (!(current.pieceIndex < decideOrder.Count && current.pieceIndex < turnGroupSize))
                {
                    if (current.bestVal == -10000)//we did not manage to recurse on any other pieces
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
                if (Time.realtimeSinceStartup - lastYieldTime > Time.smoothDeltaTime || Time.realtimeSinceStartup - lastYieldTime > frameMax)
                {
                    lastYieldTime = Time.realtimeSinceStartup;
                    yield return null;
                }
            }
            orderPieces();
        }
        decidePlacements();
    }

    //moves up one level in position evaluation, updating stack item above and reverting hypo move
    public void goUpLevel(recursiveActionItem current)
    {
        if (stack.Count == 1)//here we can't go up so we return our result
        {
            //update hypo board to include new move
            if (current.bestTile.hypoPiece != null && current.bestTile.hypoPiece != current.bestPiece)
            {
                current.bestTile.hypoPiece.getCaptured(false);
            }
            else if(current.bestTile == current.bestPiece.hypoTile)
            {
                current.bestPiece.notMoving = true;
            }
            current.bestPiece.moveToTile(current.bestTile, false);

            moveOrder.Add(current.bestPiece);
            current.bestPiece.intention = current.bestTile;
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
            if (above.tileIndex == -1)//piece did not move
            {
                above.bestTile = decideOrder[above.pieceIndex].hypoTile;
            }
            else
            {
                above.bestTile = above.targetListCopy[above.tileIndex];
            }
        }

        //undo hypo move
        decideOrder[above.pieceIndex].moveToTile(above.previousTile, false);
        decideOrder[above.pieceIndex].hypoExhausted = false;
        decideOrder[above.pieceIndex].capturing = null;
        if (above.capturedPiece != null)
        {
            above.capturedPiece.hypoAlive = true;
            above.capturedPiece.placePiece(above.targetListCopy[above.tileIndex], false);
        }
        above.capturedPiece = null;
        restoreObjectiveHypoStatuses(above);

        //remove level from stack
        stack.RemoveAt(0);
    }

    public void goDownLevel(recursiveActionItem current)
    {
        if (current.tileIndex == -1)//piece not moving
        {
            //make hypo move
            storeObjectiveHypoStatuses(current);
            decideOrder[current.pieceIndex].hypoExhausted = true;
            current.previousTile = decideOrder[current.pieceIndex].hypoTile;
        }
        else
        {
            if (!(decideOrder[current.pieceIndex].isValidCandidate(current.targetListCopy[current.tileIndex], false)))
            {
                return;//not a valid move, do nothing
            }
            //make hypo move
            storeObjectiveHypoStatuses(current);
            current.previousTile = decideOrder[current.pieceIndex].hypoTile;
            if (current.targetListCopy[current.tileIndex].hypoPiece != null)
            {
                current.capturedPiece = current.targetListCopy[current.tileIndex].hypoPiece;
                current.capturedPiece.getCaptured(false);
            }
            decideOrder[current.pieceIndex].moveToTile(current.targetListCopy[current.tileIndex], false);
        }

        //add new level to stack
        recursiveActionItem below = new recursiveActionItem();
        below.pieceIndex = -1;
        below.bestVal = -10000;
        stack.Insert(0, below);
    }
}

public class recursiveActionItem
{
    public float bestVal;
    public piece bestPiece;
    public tile bestTile;
    public int pieceIndex;
    public int tileIndex;
    public tile[] targetListCopy;
    public piece capturedPiece;
    public tile previousTile;
    public int[] objectiveStatuses;
}
