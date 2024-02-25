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
    public List<piece> allPieces;
    public List<piece> recentlyCaptured;
    public List<reversableMove> undoStack;

    public piece selectedPiece;
    public bool holdingPiece;
    public bool justClicked;
    public bool playersTurn;
    public int playsRemaining;
    public int playerEnergy;
    public float enemyEnergy;
    public int movingPieces;
    public bool undoing;

    void Start()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        gm.bm = this;
        em = Instantiate(gm.EnemyManager, transform.position, Quaternion.identity).GetComponent<enemyManager>();
        um = Instantiate(gm.UIManager, transform.position, Quaternion.identity).GetComponent<uiManager>();
        generator = Instantiate(gm.MapGenerator, transform.position, Quaternion.identity).GetComponent<mapGenerator>();
        
        generator.setTileScale();
        alivePieces = new List<piece>();
        recentlyCaptured = new List<piece>();
        undoStack = new List<reversableMove>();
        movingPieces = 0;

        gm.createInitialTeam();//for testing only, team will exist once we have other scenes
        em.init();
        generator.init();
        um.init();
        changeTurn(0);

        resetTargets(true);
    }

    void Update()
    {
        //normal input upkeep is now handled in the ui manager
    }

    public void changeTurn(int whosTurn)
    {
        clearUndoStorage();
        playersTurn = (whosTurn == 0);
        giveTurnBonuses(whosTurn);
        em.resetPieces();
        resetHighlighting();
    }

    //awards plays and energy each turn
    public void giveTurnBonuses(int currentPlayer)
    {
        playsRemaining = 1;
        if (playersTurn)
        {
            playerEnergy += 2;
        }
        else if (!playersTurn)
        {
            enemyEnergy += 2;
        }

        for (int i = 0; i < alivePieces.Count; i++)
        {
            alivePieces[i].useTurnChangeAbility();
        }
    }

    public void clearUndoStorage()
    {
        for (int i = 0; i < alivePieces.Count; i++)
        {
            alivePieces[i].pushedPieces = null;
            alivePieces[i].healedPieces = null;
            alivePieces[i].damagedPieces = null;
            alivePieces[i].abilityTarget = null;
        }
        while (recentlyCaptured.Count > 0)
        {
            if (recentlyCaptured[0] != null)
            {
                while (alivePieces.Contains(recentlyCaptured[0]))
                {
                    alivePieces.Remove(recentlyCaptured[0]);
                    Debug.LogError("Recently captured piece still listed as being alive");
                }
                if (recentlyCaptured[0].team == 0 && !recentlyCaptured[0].ephemeral)
                {
                    recentlyCaptured[0].cost = Mathf.Max(Mathf.Min(recentlyCaptured[0].cost * 2, 999), recentlyCaptured[0].cost + 1);
                    recentlyCaptured[0].useCommittedDeathAblity();
                    recentlyCaptured[0].deleteInfo();
                    recentlyCaptured[0].moveToSlot(um.findOpenSlot());
                }
                else if (recentlyCaptured[0].team == 1 && recentlyCaptured[0].resummonableByEnemy)
                {
                    recentlyCaptured[0].useCommittedDeathAblity();
                    em.resummons.Add(recentlyCaptured[0]);
                }
                else// if (recentlyCaptured[0].team == 1 || recentlyCaptured[0].ephemeral)
                {
                    recentlyCaptured[0].alive = false;
                    recentlyCaptured[0].hypoAlive = false;//this matters, or else the piece gets added to alivePieces again because unity is dumb
                    recentlyCaptured[0].destroyAll();
                }
            }
            recentlyCaptured.RemoveAt(0);
        }
        undoStack = new List<reversableMove>();
    }

    //places new piece, but does not create it, piece must already exist and be on the correct team
    public void placeNewPiece(piece newPiece, tile newTile)
    {
        newPiece.alive = true;
        newPiece.exhausted = true;
        newPiece.transform.position = newTile.transform.position;
        newPiece.placePiece(newTile, true);
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
        newPiece.useSummonAbility(true);
        resetHighlighting();

        if (true)//newPiece.team == 0 && !newPiece.champion)
        {
            reversableMove thisPlacement = new reversableMove(newPiece);
            undoStack.Insert(0, thisPlacement);
        }
    }

    public void undoMove(reversableMove lastMove, bool real)
    {
        undoing = true;
        if (lastMove.startingTile == null)
        {
            undoPlacement(lastMove, real);
            return;
        }
        if (lastMove.movedPiece.usedActivatedAbility)
        {
            lastMove.movedPiece.undoActivatedAbility(real);
            lastMove.movedPiece.usedActivatedAbility = false;
            if (real)
            {
                lastMove.movedPiece.exhausted = false;
                resetHighlighting();
            }
            else
            {
                lastMove.movedPiece.hypoExhausted = false;
                lastMove.movedPiece.inactive = false;
                lastMove.movedPiece.wastingAttackOnAlly = false;//can include wasting abilities
            }
            undoing = false;
            return;
        }

        tile moveEndTile = lastMove.movedPiece.realOrHypoTile(real);//refers to the end of the move we are undoing
        lastMove.movedPiece.undoPostMoveAbilityTrigger(real);
        lastMove.movedPiece.undoMoveAbility(real);
        lastMove.movedPiece.moveToTile(lastMove.startingTile, real);
        if (lastMove.captured != null)
        {
            lastMove.movedPiece.undoKillAbility(real);
            lastMove.captured.unGetCaptured(moveEndTile, real);
        }
        if (lastMove.attacked != null)
        {
            lastMove.movedPiece.unDealDamage(lastMove.attacked, lastMove.movedPiece.realOrHypoDamage(real), real);
            lastMove.movedPiece.undoAttackBeginAbility(real);
        }
        
        if (real)
        {
            lastMove.movedPiece.arriveOnTile();
            lastMove.movedPiece.exhausted = false;
            resetHighlighting();
        }
        else
        {
            lastMove.movedPiece.hypoExhausted = false;
            lastMove.movedPiece.inactive = false;
            lastMove.movedPiece.wastingAttackOnAlly = false;
        }
        undoing = false;
    }

    public void undoPlacement(reversableMove placement, bool real)
    {
        tile placedTile = placement.movedPiece.realOrHypoTile(real);
        placement.movedPiece.undoSummonAbility(real);
        placement.movedPiece.getCaptured(real);
        List<piece> retargeted = new List<piece>();
        retargeted.Add(placement.movedPiece);
        placedTile.updateTargeting(real, ref retargeted);
        placement.movedPiece.beingSummoned = false;
        if (real)
        {
            placement.movedPiece.exhausted = false;
            recentlyCaptured.Remove(placement.movedPiece);
            if (!placement.movedPiece.ephemeral)
            {
                placement.movedPiece.moveToSlot(um.findOpenSlot());
                placement.movedPiece.refundEnergyCost();
            }
            resetHighlighting();
        }
        else
        {
            placement.movedPiece.hypoExhausted = false;
        }
        undoing = false;
    }

    public void undoPushes(List<pushedPiece> pushedPieces, bool real)
    {
        if (pushedPieces == null)
        {
            return;
        }
        for (int i = 0;i<pushedPieces.Count;i++)
        {
            if (pushedPieces[i] != null)
            {
                if (pushedPieces[i].thisPiece.realOrHypoTile(real) != pushedPieces[i].startingTile)
                {
                    if (real)
                    {
                        pushedPieces[i].thisPiece.pushedTile = pushedPieces[i].startingTile;//to prevent using ability when undoing a push
                        pushedPieces[i].thisPiece.moveToTile(pushedPieces[i].startingTile, real);
                        pushedPieces[i].thisPiece.arriveOnTile();
                        pushedPieces[i].thisPiece.pushedTile = null;
                    }
                    else
                    {
                        pushedPieces[i].thisPiece.hypoPushedTile = pushedPieces[i].startingTile;//to prevent using ability when undoing a push
                        pushedPieces[i].thisPiece.moveToTile(pushedPieces[i].startingTile, real);
                        pushedPieces[i].thisPiece.hypoPushedTile = null;
                    }
                }
                if (pushedPieces[i].pushedInto != null)//undo collision damage
                {
                    pushedPieces[i].thisPiece.unTakeDamage(gm.pushDamage, real);
                    pushedPieces[i].pushedInto.unTakeDamage(gm.pushDamage, real);
                }
            }
        }
    }

    public void undoHeals(List<healedPiece> healedPieces, bool real)
    {
        if (healedPieces == null)
        {
            return;
        }
        for (int i = 0;i<healedPieces.Count;i++)
        {
            healedPieces[i].thisPiece.unGetHeal(healedPieces[i].healAmount, real);
        }
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
        List<piece> retargeted = new List<piece>();
        for(int i = 0;i< alivePieces.Count;i++)
        {
            alivePieces[i].updateTargeting(real, ref retargeted);
        }
    }

    public void resetHighlighting ()
    {
        for (int i = 0; i<allTiles.Length; i++)
        {
            allTiles[i].setColor();
        }
        if (selectedPiece != null && selectedPiece.alive && selectedPiece.activatingAbility)
        {
            //selectedPiece.highlightAbilityCandidates();
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

public class pushedPiece
{
    public tile startingTile;
    public piece thisPiece;
    public piece pushedInto;
}

public class healedPiece
{
    public piece thisPiece;
    public int healAmount;

    public healedPiece(piece target, int amount)
    {
        thisPiece = target;
        healAmount = amount;
    }
}

public class reversableMove
{
    public piece movedPiece;
    public tile startingTile;
    public piece attacked;
    public piece captured;
    //list of pushed pieces will be stored in the movedPiece

    public reversableMove(piece moved, tile start, piece att, piece cap)
    {
        movedPiece = moved;
        startingTile = start;
        attacked = att;
        captured = cap;
    }

    public reversableMove(piece moved)
    {
        movedPiece = moved;
    }
}