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
    public List<piece> allPieces;
    public objective[] allObjectives;


    public piece selectedPiece;
    public bool holdingPiece;
    public bool justClicked;
    public bool playersTurn;
    public int playsRemaining;

    void Start()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        gm.bm = this;
        em = Instantiate(gm.EnemyManager, transform.position, Quaternion.identity).GetComponent<enemyManager>();
        um = Instantiate(gm.UIManager, transform.position, Quaternion.identity).GetComponent<uiManager>();
        generator = Instantiate(gm.MapGenerator, transform.position, Quaternion.identity).GetComponent<mapGenerator>();

        allPieces = new List<piece>();
        generator.init();
        gm.createInitialTeam();//for testing only, team will exist once we have other scenes
        em.init();
        um.init();
        changeTurn(0);

        //for now, manually force some pieces onto the board
        /*for (int i = 0;i<40;i++)
        {
            tile place = allTiles[Random.Range(0, allTiles.Length)];
            if (place.thisPiece == null)
            {
                piece newPiece = Instantiate(gm.Pieces[0], place.transform.position, Quaternion.identity).GetComponent<piece>();
                newPiece.thisTile = place;
                place.thisPiece = newPiece;
                newPiece.team = 0;
                newPiece.init();
            }
        }*/

        resetTargets(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && playersTurn)
        {
            playersTurn = false;
            em.takeTurn();
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
        giveObjectiveBonuses(whosTurn);
        resetHighlighting();
    }

    //awards plays and energy for each objective controlled
    public void giveObjectiveBonuses(int currentPlayer)
    {
        playsRemaining = 0;
        for (int i = 0;i<allObjectives.Length;i++)
        {
            if (allObjectives[i].team == currentPlayer)
            {
                playsRemaining++;
            }
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
        if (!allPieces.Contains(newPiece))
        {
            allPieces.Add(newPiece);
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
        for(int i = 0;i<allPieces.Count;i++)
        {
            allPieces[i].updateTargeting(real);
        }
    }

    public void resetHighlighting ()
    {
        for (int i = 0; i<allTiles.Length; i++)
        {
            allTiles[i].gameObject.GetComponent<SpriteRenderer>().color = allTiles[i].defaultColor;
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
