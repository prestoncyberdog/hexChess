using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class battleManager : MonoBehaviour
{
    public gameManager gm;
    public enemyManager em;
    public uiManager um;
    public mapGenerator generator;

    public tile[] allTiles;
    public List<piece> alivePieces;

    public piece selectedPiece;
    public bool holdingPiece;
    public bool justClicked;
    public bool playersTurn;
    public int playsRemaining;
    public int playerEnergy;
    public int enemyEnergy;
    public piece movingPiece;

    void Start()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        gm.bm = this;
        em = Instantiate(gm.EnemyManager, transform.position, Quaternion.identity).GetComponent<enemyManager>();
        um = Instantiate(gm.UIManager, transform.position, Quaternion.identity).GetComponent<uiManager>();
        generator = Instantiate(gm.MapGenerator, transform.position, Quaternion.identity).GetComponent<mapGenerator>();
        
        generator.setTileScale();
        alivePieces = new List<piece>();

        gm.createInitialTeam();//for testing only, team will exist once we have other scenes
        em.init();
        generator.init();
        um.init();
        changeTurn(0);

        resetTargets(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && playersTurn)
        {
            playersTurn = false;
            em.takeTurn();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            gm.loadMap();
        }

        if (Input.GetMouseButtonDown(0) && !justClicked)
        {
            selectedPiece = null;
            resetTiles();
            resetHighlighting();
        }
        else
        {
            justClicked = false;//if a tile is clicked, it sets this to true
            //this means we can select/deselect pieces regardless of update order
        }

        if (holdingPiece)
        {
            selectedPiece.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0,0,10);
            if (Input.GetMouseButtonUp(0))
            {
                holdingPiece = false;
                if (selectedPiece.alive)
                {
                    selectedPiece.transform.position = selectedPiece.thisTile.transform.position;
                }
                else
                {
                    selectedPiece.transform.position = selectedPiece.thisSlot.transform.position;
                }
            }
        }
    }

    public void changeTurn(int whosTurn)
    {
        em.unexhaustPieces();
        playersTurn = (whosTurn == 0);
        giveTurnBonuses(whosTurn);
        resetHighlighting();
    }

    //awards plays and energy each turn
    public void giveTurnBonuses(int currentPlayer)
    {
        playsRemaining = 1;
        if (playersTurn)
        {
            playerEnergy += 10;
        }
        else if (!playersTurn)
        {
            enemyEnergy += 10;
        }
    }

    //places new piece, but does not create it, piece must already exist and be on the correct team
    public void placeNewPiece(piece newPiece, tile newTile)
    {
        newPiece.alive = true;
        newPiece.exhausted = true;
        newPiece.placePiece(newTile, true);
        newPiece.transform.position = newTile.transform.position;
        playsRemaining--;
        if (newPiece.thisSlot != null)
        {
            newPiece.thisSlot.thisPiece = null;
            newPiece.thisSlot.setColor();
            newPiece.thisSlot = null;
        }
        if (!alivePieces.Contains(newPiece))
        {
            alivePieces.Add(newPiece);
        }
        resetHighlighting();
    }

    //reset distance measures for all tiles
    public void resetTiles()
    {
        for (int i = 0; i < allTiles.Length; i++)
        {
            allTiles[i].distance = 1000;
        }
    }

    public void resetTargets(bool real)
    {
        for(int i = 0;i< alivePieces.Count;i++)
        {
            alivePieces[i].updateTargeting(real);
        }
    }

    public void resetHighlighting ()
    {
        for (int i = 0; i<allTiles.Length; i++)
        {
            allTiles[i].setColor();
        }
        if (selectedPiece != null && selectedPiece.alive)
        {
            selectedPiece.highlightCandidates();
        }
        else if (selectedPiece != null && !selectedPiece.alive)
        {
            for (int i = 0; i < allTiles.Length; i++)
            {
                if (allTiles[i].isValidPlacement(0, true))
                {
                    allTiles[i].gameObject.GetComponent<SpriteRenderer>().color = allTiles[i].candidateColor;
                }
            }
        }
        um.resetHighlighting();
    }

    public tile findNearestTileToMouse()
    {
        tile nearest = allTiles[0];
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float minDist = (mousePos - nearest.transform.position).magnitude;
        for (int i = 0;i < allTiles.Length;i++)
        {
            if ((mousePos - allTiles[i].transform.position).magnitude < minDist)
            {
                nearest = allTiles[i];
                minDist = (mousePos - nearest.transform.position).magnitude;
            }
        }
        return nearest;
    }

    
}
