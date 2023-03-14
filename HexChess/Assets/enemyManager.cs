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
    public bool readyToEnd;

    public void init()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;

        groupSize = 3;
        spawnDelayMax = 1f;

        moveOrder = new List<piece>();

        spawnPlan = new List<piece>();
        prepareSpawns();
    }

    void Update()
    {

    }

    public void takeTurn()
    {
        readyToEnd = false;
        findAllPieces();
        bm.changeTurn(1);
        copyHypoBoard();
        decideActions();
        StartCoroutine(moveAllPieces());
    }

    //each piece must recieve an intention tile and the pieces will be ordered in moveOrder
    public void decideActions()
    {
        orderPieces();
        moveOrder = new List<piece>();
        while (decideOrder.Count > 0)
        {
            //decideActionRecur(true);
            decideActionsNonRecursive();
        }
        decidePlacements();
    }

    //chooses and adds 1 action to the moveOrder list, removes relevant piece from decideOrder
    public float decideActionRecur(bool makeDecision)
    {
        float bestVal = -1;
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

        if (bestVal == -1)//we did not manage to recurse on any other pieces
        {
            bestVal = evaluatePosition();
        }
        return bestVal;
    }

    public void decideActionsNonRecursive()
    {
        //create stack 
        stack = new List<recursiveActionItem>();
        recursiveActionItem current = new recursiveActionItem();
        current.pieceIndex = -1;
        current.bestVal = -1;
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
                } while (current.pieceIndex < decideOrder.Count && current.pieceIndex < groupSize && decideOrder[current.pieceIndex].hypoExhausted);//find a piece that isnt exhausted
                if (current.pieceIndex < decideOrder.Count && current.pieceIndex < groupSize)
                {
                    current.tileIndex = -1;//start at -1 to consider not moving first
                    current.targetListCopy = new tile[decideOrder[current.pieceIndex].hypoTargets.Count];
                    decideOrder[current.pieceIndex].hypoTargets.CopyTo(current.targetListCopy);
                }
            }
            //consider going up
            if (!(current.pieceIndex < decideOrder.Count && current.pieceIndex < groupSize))
            {
                if (current.bestVal == -1)//we did not manage to recurse on any other pieces
                {
                    current.bestVal = evaluatePosition();
                }
                goUpLevel(current);
            }
            else
            {
                goDownLevel(current);
            }
        }
    }

    //moves up one level in position evaluation, updating stack item above and reverting hypo move
    public void goUpLevel(recursiveActionItem current)
    {
        if (stack.Count == 1)//here we can't go up so we return our result
        {
            //update hypo board to include new move
            if (current.bestTile.hypoPiece != null)
            {
                current.bestTile.hypoPiece.hypoAlive = false;
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
            above.capturedPiece.placePiece(above.targetListCopy[above.tileIndex], false);
            above.capturedPiece.hypoAlive = true;
        }
        above.capturedPiece = null;

        //remove level from stack
        stack.RemoveAt(0);
    }

    public void goDownLevel(recursiveActionItem current)
    {
        if (current.tileIndex == -1)//piece not moving
        {
            //make hypo move
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
            current.previousTile = decideOrder[current.pieceIndex].hypoTile;
            if (current.targetListCopy[current.tileIndex].hypoPiece != null)
            {
                current.capturedPiece = current.targetListCopy[current.tileIndex].hypoPiece;
                current.capturedPiece.hypoAlive = false;
            }
            decideOrder[current.pieceIndex].moveToTile(current.targetListCopy[current.tileIndex], false);
        }

        //add new level to stack
        recursiveActionItem below = new recursiveActionItem();
        below.pieceIndex = -1;
        below.bestVal = -1;
        stack.Insert(0,below);
    }

    //evaluates the hypothetical position
    //should also consider end of game?
    public float evaluatePosition()
    {
        return Random.Range(0, 1000);
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
            bestVal = -1;
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
    public void orderPieces()
    {
        //for now, whatever order enemyPieces is in is fine
        decideOrder = new List<piece>();
        for (int i = 0;i<enemyPieces.Count;i++)
        {
            decideOrder.Add(enemyPieces[i]);
        }
    }

    public IEnumerator moveAllPieces()
    {
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
                else if (readyToEnd)
                {
                    bm.changeTurn(0);
                }
            }
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
        for (int i = 0; i < bm.allPieces.Count; i++)
        {
            bm.allPieces[i].hypoAlive = bm.allPieces[i].alive;
            bm.allPieces[i].hypoExhausted = false;
            bm.allPieces[i].hypoTile = bm.allPieces[i].thisTile;
            //copy targets
            bm.allPieces[i].hypoTargets = new List<tile>();
            copyTileList(bm.allPieces[i].targets, bm.allPieces[i].hypoTargets);
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

    public void findAllPieces()
    {
        GameObject[] pieceGameObjects = GameObject.FindGameObjectsWithTag("piece");
        piece currentPiece;
        playerPieces = new List<piece>();
        enemyPieces = new List<piece>();
        for (int i = 0;i < pieceGameObjects.Length;i++)
        {
            currentPiece = pieceGameObjects[i].GetComponent<piece>();
            if (currentPiece.alive)
            { 
                bm.allPieces.Add(currentPiece);
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
        for (int i = 0;i< playerPieces.Count;i++)
        {
            playerPieces[i].exhausted = false;
            playerPieces[i].setColor();
        }
        for (int i = 0; i < enemyPieces.Count; i++)
        {
            enemyPieces[i].exhausted = false;
            enemyPieces[i].setColor();
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
}
