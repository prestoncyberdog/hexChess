using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyManager : MonoBehaviour
{
    public gameManager gm;
    public battleManager bm;

    public List<piece> playerPieces;
    public List<piece> enemyPieces;
    public List<piece> moveOrder;
    public List<piece> decideOrder;
    public int moveIndex;

    public int groupSize;//the number of units that can be considered together

    public void init()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;

        groupSize = 2;

        moveOrder = new List<piece>();
        moveIndex = 0;

        //for now, manually force some pieces onto the board
        for (int i = 0; i < 10; i++)
        {
            tile place = bm.allTiles[Random.Range(0, bm.allTiles.Length)];
            if (place.thisPiece == null)
            {
                piece newPiece = Instantiate(gm.Pieces[0], place.transform.position, Quaternion.identity).GetComponent<piece>();
                newPiece.thisTile = place;
                place.thisPiece = newPiece;
                newPiece.team = 1;
                newPiece.init();
            }
        }
    }

    void Update()
    {
        if (bm.playersTurn)
        {
            return;
        }
        else if (moveOrder[moveIndex].newTile != null)//current piece is still moving
        {
            return;
        }
        else
        {
            moveIndex++;
            if (moveIndex >= moveOrder.Count)
            {
                endTurn();
                return;
            }
            movePiece(moveOrder[moveIndex]);
        }
    }

    public void takeTurn()
    {
        findAllPieces();
        unexhaustPieces();
        copyHypoBoard();
        decideActions();
        //begin moving pieces
        if (moveOrder.Count == 0)
        {
            endTurn();
        }
        else
        {
            movePiece(moveOrder[0]);
        }
    }

    //each piece must recieve an intention tile and the pieces will be ordered in moveOrder
    public void decideActions()
    {
        orderPieces();
        moveOrder = new List<piece>();
        moveIndex = 0;
        while (decideOrder.Count > 0)
        {
            decideActionRecur(true);
        }
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

        for (int i = 0; i < decideOrder.Count && i < groupSize; i++)//todo: let pieces choose to not move
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

    //evaluates the hypothetical position
    //should also consider end of game?
    public float evaluatePosition()
    {
        return Random.Range(0, 1000);
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

    public void copyHypoBoard()
    {
        for (int i = 0;i<bm.allPieces.Count;i++)
        {
            bm.allPieces[i].hypoAlive = bm.allPieces[i].alive;
            bm.allPieces[i].hypoExhausted = false;
            bm.allPieces[i].hypoTile = bm.allPieces[i].thisTile;
            //copy targets
            bm.allPieces[i].hypoTargets = new List<tile>();
            copyTileList(bm.allPieces[i].targets, bm.allPieces[i].hypoTargets);
        }
        for (int i = 0;i<bm.allTiles.Length;i++)
        {
            bm.allTiles[i].hypoPiece = bm.allTiles[i].thisPiece;
            bm.allTiles[i].hypoObstacle = bm.allTiles[i].obstacle;
            //copy targeted by
            bm.allTiles[i].hypoTargetedBy = new List<piece>();
            copyPieceList(bm.allTiles[i].targetedBy, bm.allTiles[i].hypoTargetedBy);
        }
    }

    public void endTurn()
    {
        unexhaustPieces();
        bm.playersTurn = true;
        bm.resetHighlighting();
    }

    public void movePiece(piece currentPiece)
    {
        if (currentPiece.thisTile == currentPiece.intention)//here, we move on to the next piece right away
        {
            moveIndex++;
            if (moveIndex >= moveOrder.Count)
            {
                endTurn();
                return;
            }
            movePiece(moveOrder[moveIndex]);
            return;
        }
        currentPiece.moveToTile(currentPiece.intention, true);
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
}
