﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyManager : MonoBehaviour
{
    public gameManager gm;
    public battleManager bm;

    public List<piece> playersPieces;
    public List<piece> enemyPieces;
    public List<piece> moveOrder;
    public int moveIndex;

    public void init()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;

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
        moveOrder = new List<piece>();
        for (int i = 0;i<enemyPieces.Count;i++)
        {
            moveOrder.Add(enemyPieces[i]);
            //enemyPieces[i].findAllCandidates();
            enemyPieces[i].intention = enemyPieces[i].thisTile;
        }
    }

    public void endTurn()
    {
        unexhaustPieces();
        bm.playersTurn = true;
        bm.selectedPiece = null;
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
        currentPiece.moveToTile(currentPiece.intention);
    }

    public void findAllPieces()
    {
        GameObject[] pieceGameObjects = GameObject.FindGameObjectsWithTag("piece");
        piece currentPiece;
        playersPieces = new List<piece>();
        enemyPieces = new List<piece>();
        for (int i = 0;i < pieceGameObjects.Length;i++)
        {
            currentPiece = pieceGameObjects[i].GetComponent<piece>();
            if (currentPiece.alive)
            { 
                bm.allPieces.Add(currentPiece);
                if (currentPiece.team == 0)
                {
                    playersPieces.Add(currentPiece);
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
        for (int i = 0;i< playersPieces.Count;i++)
        {
            playersPieces[i].exhausted = false;
            playersPieces[i].setColor();
        }
        for (int i = 0; i < enemyPieces.Count; i++)
        {
            enemyPieces[i].exhausted = false;
            enemyPieces[i].setColor();
        }
    }
}
