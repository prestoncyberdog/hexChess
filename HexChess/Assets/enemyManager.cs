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
        findAllPieces(true);
        copyHypoBoard();
        StartCoroutine(decideActions());
        StartCoroutine(moveAllPieces());
    }

    //evaluates the hypothetical position
    //should also consider end of game?
    public float evaluatePosition()
    {
        //these values can balance how much the ai values different measures
        float championWeight = 2;//bonus multiplier on piece value
        float pieceWeight = 5;
        float targetingWeight = 0.5f;
        float positionWeight = 0.2f;
        float notMovingPenalty = .5f;
        float randomizationWeight = 0;//0.01f;
        findAllPieces(false);
        float value = (calculateChampionScore(championWeight, targetingWeight) +
                       calculatePieceScore(pieceWeight) +
                       calculateTargetingScore(targetingWeight, pieceWeight) +
                       calculatePositionScore(positionWeight, notMovingPenalty)) *
                       (1 + (Random.Range(0, 10000) / 10000f) * randomizationWeight);
        //Debug.Log("position value: " + value);
        return value;
    }

    //considers whether both champions are alive and healthy
    //championWeight multiplies with the health fraction of each champion
    public float calculateChampionScore(float championWeight, float targetingWeight)
    {
        float champScore = 0;
        int potentialDamage = 0;
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
        //consider each attacking piece
        for (int i = 0; i < gm.champions[1].hypoTile.hypoTargetedBy.Count; i++)
        { 
            if (gm.champions[1].hypoTile.hypoTargetedBy[i].team == 0)
            {
                potentialDamage += gm.champions[1].expectedDamage(gm.champions[1].hypoTile.hypoTargetedBy[i].damage);
            }
        } 
        if (potentialDamage >= gm.champions[1].hypoHealth)
        {
            champScore -= 10000;
        }
        else
        {
            champScore -= championWeight * targetingWeight * gm.champions[1].value * potentialDamage * 1f / gm.champions[1].maxHealth;
        }

        //Debug.Log("Champ score: " + champScore);
        return champScore;
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
        float pieceTargetingPenalty;
        bool defended;
        int potentialDamage;
        bool canRetaliate;
        float retaliationModifier = 0.1f;//give less penalty when we hope to retaliate
        piece otherPiece;
        tile currentTile;
        tile landingTile;
        for (int i = 0; i < enemyPieces.Count; i++)
        {
            currentTile = enemyPieces[i].hypoTile;
            defended = false;
            potentialDamage = 0;
            pieceTargetingPenalty = 0;
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

                    if (canRetaliate)
                    {
                        pieceTargetingPenalty += (enemyPieces[i].expectedDamage(otherPiece.damage) * 1f / enemyPieces[i].maxHealth) * .5f * enemyPieces[i].value * pieceWeight * retaliationModifier;
                    }
                    else
                    {
                        pieceTargetingPenalty += (enemyPieces[i].expectedDamage(otherPiece.damage) * 1f / enemyPieces[i].maxHealth) * .5f * enemyPieces[i].value * pieceWeight;
                    }
                    potentialDamage += enemyPieces[i].expectedDamage(otherPiece.damage);
                }
            }
            //check if total is enough to capture
            if (potentialDamage > enemyPieces[i].hypoHealth)
            {
                    if (defended)
                    {
                        pieceTargetingPenalty += enemyPieces[i].value * pieceWeight * retaliationModifier;
                    }
                    else
                    {
                        pieceTargetingPenalty += enemyPieces[i].value * pieceWeight;
                    }
            }
            //update targeting score for this piece
            pieceTargetingPenalty *= targetingWeight;
            pieceTargetingPenalty = Mathf.Min(pieceTargetingPenalty, enemyPieces[i].value * pieceWeight) * -1f;//penalty cannot be worse than the losing the piece
            targetingScore += Mathf.Min(pieceTargetingPenalty, 0);//cannot be positive result
        }
        //Debug.Log("Targeting score: " + targetingScore);
        return targetingScore;
    }

    //awards points to each enemy piece based on its proximity to any objective the enemy doesnt already control
    //penalizes pieces slightly for not moving
    //assumes findAllPieces has already been called
    public float calculatePositionScore(float positionWeight, float notMovingPenalty)
    {
        float positionScore = 0;
        for (int i = 0; i < enemyPieces.Count; i++)
        {
            positionScore -= Mathf.Abs(enemyPieces[i].hypoTile.hypoChampionDists[0] - enemyPieces[i].minAttackRange);//penalize each piece for being farther than min range from the player's champion
            if (enemyPieces[i].notMoving)
            {
                positionScore -= notMovingPenalty;
            }
        }
        //should we also reward the ai for keeping its champion far away from the player's pieces? maybe with lower weight?
        //Debug.Log("Position score: " + positionScore * positionWeight);
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

                /*currentPiece.getCaptured(false);
                List<piece> retargeted = new List<piece>();
                retargeted.Add(currentPiece);
                spawnTiles[i].updateTargeting(false, ref retargeted);*/
            }
            //place piece on hypo board
            currentPiece.hypoAlive = true;
            currentPiece.placePiece(bestTile, false);
            currentPiece.useSummonAbility(false);
            currentPiece.payEnergyCost();
            //plan to place piece in move order
            moveOrder.Add(currentPiece);
            currentPiece.intention = bestTile;
            spawnTiles.Remove(bestTile);
        }
        readyToEnd = true; //allows piece moving coroutine to end turn when ready
    }

    //determines the order in which the ai will consider its pieces
    //we will select the closest piece to an uncontrolled objective, then order other pieces by distance from the first
    public void orderPieces()
    {
        decideOrder = new List<piece>();
        int minDist = 10000;;
        int currDist;
        piece firstPiece = null;
        //find piece that can move which is closest to the players champion
        for (int i = 0; i < enemyPieces.Count; i++)
        {
            if (enemyPieces[i].hypoExhausted == false && enemyPieces[i].notMoving == false && enemyPieces[i].hypoAlive == true)
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
                activeTile.hypoPiece.hypoExhausted == false && 
                activeTile.hypoPiece.notMoving == false && 
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
            if (currentPiece.intention != currentPiece.thisTile)
            {
                currentPiece.moveToTile(currentPiece.intention, true);
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
            bm.placeNewPiece(currentPiece, currentPiece.intention);
        }
    }

    //sets hypo board to match real board
    public void copyHypoBoard()
    {
        for (int i = 0; i < bm.alivePieces.Count; i++)
        {
            bm.alivePieces[i].hypoAlive = bm.alivePieces[i].alive;
            bm.alivePieces[i].hypoHealth = bm.alivePieces[i].health;
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
    public void unexhaustPieces()
    {
        for (int i = 0;i< bm.alivePieces.Count;i++)
        {
            bm.alivePieces[i].exhausted = false;
            bm.alivePieces[i].hypoExhausted = false;
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

    public void createChampion()
    {
        piece newPiece = Instantiate(gm.Pieces[gm.chooseRandomChampion()], new Vector3(1000, 1000, 0), Quaternion.identity).GetComponent<piece>();
        newPiece.team = 1;
        newPiece.init();
        newPiece.champion = true;
        gm.champions[1] = newPiece;
    }

    public void prepareSpawns()
    {
        for (int i = spawnPlan.Count; i < 10; i++)
        {
            piece newPiece = Instantiate(gm.Pieces[Random.Range(0, gm.Pieces.Length)], new Vector3(1000, 1000, 0), Quaternion.identity).GetComponent<piece>();
            newPiece.team = 1;
            newPiece.init();
            spawnPlan.Add(newPiece);
        }
    }

    //each piece must recieve an intention tile and the pieces will be ordered in moveOrder
    //this function is reworked to be not recursive so that it can be a coroutine and yield each frame
    public IEnumerator decideActions()
    {
        readyToEnd = false;
        turnGroupSize = groupSize;// - 1;//set lower so first decision can be made quickly
        lastYieldTime = Time.realtimeSinceStartup;
        moveOrder = new List<piece>();
        orderPieces();
        //turnGroupSize++;//set to full value because we can think while pieces move now
        while (decideOrder.Count > 0)//choose a single piece action each loop
        {
            //create stack 
            stack = new List<recursiveActionItem>();
            recursiveActionItem current = new recursiveActionItem();
            current.pieceIndex = -1;
            current.bestVal = -100000;
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
                    if (current.bestVal == -100000)//we did not manage to recurse on any other pieces
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
        decidePlacements();
    }

    //moves up one level in position evaluation, updating stack item above and reverting hypo move
    public void goUpLevel(recursiveActionItem current)
    {
        if (stack.Count == 1)//here we can't go up so we return our result
        {
            current.attackedPiece = null;
            //update hypo board to include new move
            if (current.bestTile.hypoPiece != null && 
                current.bestTile.hypoPiece != current.bestPiece &&
                current.bestTile.hypoPiece.willGetCaptured(current.bestPiece, false))//here we are capturing a piece
            {
                current.bestTile.hypoPiece.getCaptured(false);
            }
            else if (current.bestTile.hypoPiece != null && 
                     current.bestTile.hypoPiece != current.bestPiece)//here, we are attacking but not capturing another piece
            {
                current.attackedPiece = current.bestTile.hypoPiece;
            }
            else if(current.bestTile == current.bestPiece.hypoTile)//here we aren't moving
            {
                current.bestPiece.notMoving = true;
            }
            current.bestPiece.moveToTile(current.bestTile, false);//we must move after capturing but before dealing damage
            if (current.attackedPiece != null)
            {
                current.bestPiece.dealDamage(current.attackedPiece, false);
                current.attackedPiece = null;
            }
            current.bestPiece.pushedPieces = null;//shouldnt matter

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

        reversableMove thisMove = new reversableMove(decideOrder[above.pieceIndex], above.previousTile, above.attackedPiece, above.capturedPiece);
        bm.undoMove(thisMove, false);
        above.capturedPiece = null;
        above.attackedPiece = null;
        //undo hypo move
        /*decideOrder[above.pieceIndex].undoAttackAbility();
        decideOrder[above.pieceIndex].undoMoveAbility();
        decideOrder[above.pieceIndex].moveToTile(above.previousTile, false);
        decideOrder[above.pieceIndex].hypoExhausted = false;
        decideOrder[above.pieceIndex].capturing = null;
        if (above.capturedPiece != null)
        {
            above.capturedPiece.hypoAlive = true;
            above.capturedPiece.placePiece(above.targetListCopy[above.tileIndex], false);
            above.capturedPiece = null;
        }
        if (above.attackedPiece != null)
        {
            decideOrder[above.pieceIndex].unDealDamage(above.attackedPiece);
            above.attackedPiece = null;
        }*/

        //remove level from stack
        stack.RemoveAt(0);
    }

    public void goDownLevel(recursiveActionItem current)
    {
        if (current.tileIndex == -1)//piece not moving
        {
            //make hypo move
            decideOrder[current.pieceIndex].hypoExhausted = true;
            decideOrder[current.pieceIndex].notMoving = true;
            current.previousTile = decideOrder[current.pieceIndex].hypoTile;
        }
        else
        {
            if (!(decideOrder[current.pieceIndex].isValidCandidate(current.targetListCopy[current.tileIndex], false)))
            {
                return;//not a valid move, do nothing
            }
            //make hypo move
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
                decideOrder[current.pieceIndex].dealDamage(current.attackedPiece, false);
            }
        }

        //add new level to stack
        recursiveActionItem below = new recursiveActionItem();
        below.pieceIndex = -1;
        below.bestVal = -100000;
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
    public piece attackedPiece;
    public tile previousTile;
}