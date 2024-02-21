using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class uiManager : MonoBehaviour
{
    public gameManager gm;
    public battleManager bm;

    public teamSlot[] teamlist;
    public pieceInfoManager pm;

    public GameObject energy;
    public Text energyText;
    Vector3 energyPos;

    public GameObject plays;
    public Text playsText;
    Vector3 playsPos;

    public void init()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;
        pm = gameObject.GetComponent<pieceInfoManager>();

        createTeamSlots();
        fillTeamSlots();
        createText();
        pm.init();
    }

    void Update()
    {
        checkInputs();
        updateText();
    }

    public void tileMouseOver(tile currTile)
    {
        if (Input.GetMouseButtonDown(0))//use left click to select and place pieces
        {
            if(bm.selectedPiece != null && !bm.selectedPiece.alive && bm.playersTurn && currTile.isValidPlacement(0, true) && bm.selectedPiece.canAfford() && bm.movingPieces == 0)//placing piece
            {
                bm.placeNewPiece(bm.selectedPiece, currTile);
                bm.selectedPiece.payEnergyCost();
            }
            if (currTile.thisPiece != null)//selecting piece
            {
                bm.selectedPiece = currTile.thisPiece;
                bm.justClicked = true;
                bm.selectedPiece.activatingAbility = false;
                bm.resetHighlighting();
                if (!currTile.thisPiece.exhausted && currTile.thisPiece.team == 0 && bm.playersTurn)
                {
                    bm.holdingPiece = true;
                }
                pm.updateText();
            }
        }

        if (Input.GetMouseButtonDown(1))//use right click to make pieces move or place them
        {
            if (bm.selectedPiece != null && bm.selectedPiece.alive && !bm.selectedPiece.exhausted && bm.selectedPiece.hasActivatedAbility && bm.selectedPiece.activatingAbility && 
                bm.selectedPiece.team == 0 && bm.playersTurn  && bm.movingPieces == 0 && bm.selectedPiece.abilityTargets.Contains(currTile) && bm.selectedPiece.isValidAbilityTarget(currTile, true))//activating special ability
            {
                bm.selectedPiece.useActivatedAbility(currTile, true);
                bm.selectedPiece.usedActivatedAbility = true;
                bm.resetHighlighting();
            }
            else if (bm.selectedPiece != null && bm.selectedPiece.alive && !bm.selectedPiece.exhausted && currTile.targetedBy.Contains(bm.selectedPiece) && bm.selectedPiece.team == 0 && bm.playersTurn &&
                bm.selectedPiece.isValidCandidate(currTile, true) && bm.movingPieces == 0 && !bm.selectedPiece.activatingAbility)//moving piece
            {
                bm.selectedPiece.moveToTile(currTile, true);
                StartCoroutine(bm.selectedPiece.moveTowardsNewTile());
                //bm.resetTiles();
                //bm.resetHighlighting();
            }
            if(bm.selectedPiece != null && !bm.selectedPiece.alive && bm.playersTurn && currTile.isValidPlacement(0, true) && bm.selectedPiece.canAfford() && bm.movingPieces == 0)//placing piece
            {
                bm.placeNewPiece(bm.selectedPiece, currTile);
                bm.selectedPiece.payEnergyCost();
            }
        }

        if (Input.GetMouseButtonUp(0))//use drag and drop to move and place pieces
        {
            if (bm.selectedPiece != null && bm.holdingPiece && bm.selectedPiece.alive && !bm.selectedPiece.exhausted && currTile.targetedBy.Contains(bm.selectedPiece) &&
                bm.selectedPiece.isValidCandidate(currTile, true) && bm.movingPieces == 0)//moving piece
            {
                bm.holdingPiece = false;
                bm.selectedPiece.moveToTile(currTile, true);
                if (bm.selectedPiece.attacking != null)
                {
                    bm.selectedPiece.dealDamage(bm.selectedPiece.attacking, bm.selectedPiece.damage, true);
                    //bm.selectedPiece.attacking = null;
                }
                bm.selectedPiece.arriveOnTile();
                //bm.resetHighlighting();
            }
            else if (bm.selectedPiece != null && bm.holdingPiece && !bm.selectedPiece.alive && bm.playersTurn && 
                     currTile.isValidPlacement(0, true) && bm.selectedPiece.canAfford() && bm.movingPieces == 0)//placing piece
            {
                bm.placeNewPiece(bm.selectedPiece, currTile);
                bm.holdingPiece = false;
                bm.selectedPiece.payEnergyCost();
            }
        }
    }

    public void teamSlotMouseOver(teamSlot currTeamSlot)
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (currTeamSlot.thisPiece != null)//selecting piece
            {
                bm.selectedPiece = currTeamSlot.thisPiece;
                bm.justClicked = true;
                bm.resetHighlighting();
                bm.holdingPiece = true;
                pm.updateText();
            }
        }

        if (Input.GetMouseButtonUp(0))//moving pieces between slots
        {
            if (bm.selectedPiece != null && bm.holdingPiece && !bm.selectedPiece.alive)
            {
                bm.selectedPiece.moveToSlot(currTeamSlot);
                resetHighlighting();
            }
        }
    }

    //routine checks for inputs each frame
    public void checkInputs()
    {
        if (Input.GetKeyDown(KeyCode.Space) && bm.playersTurn)
        {
            bm.playersTurn = false;
            bm.em.takeTurn();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            gm.loadMap();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (bm.movingPieces == 0 && bm.playersTurn && bm.undoStack.Count > 0)
            {
                reversableMove lastMove = bm.undoStack[0];
                bm.undoStack.RemoveAt(0);
                bm.undoMove(lastMove, true);
            }
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (bm.selectedPiece != null)
            {
                bm.selectedPiece.startUsingAbility();
            }
        }

        if (Input.GetMouseButtonDown(0) && !bm.justClicked)
        {
            bm.selectedPiece = null;
            bm.resetTiles();
            bm.resetHighlighting();
        }
        else
        {
            bm.justClicked = false;//if a tile is clicked, it sets this to true
            //this means we can select/deselect pieces regardless of update order
        }

        if (bm.holdingPiece)
        {
            bm.selectedPiece.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0,0,10);
            if (Input.GetMouseButtonUp(0))
            {
                bm.holdingPiece = false;
                if (bm.selectedPiece.alive)
                {
                    bm.selectedPiece.transform.position = bm.selectedPiece.thisTile.transform.position;
                }
                else
                {
                    bm.selectedPiece.transform.position = bm.selectedPiece.thisSlot.transform.position;
                }
            }
        }
    }

    public void resetHighlighting()
    {
        for(int i = 0;i<teamlist.Length;i++)
        {
            teamlist[i].setColor();
        }
    }

    public void createTeamSlots()
    {
        teamlist = new teamSlot[6];
        float listSpacing = 1.3f;
        float listSpacingVert = 1.3f;
        int rowLength = 3;
        Vector3 listOffset = new Vector3(-5.5f - listSpacing*(rowLength-1)*.5f, listSpacingVert * 2, 0);
        Vector3 currentPos = transform.position + listOffset;
        //we create a grid of buttons
        for (int i = 0; i < teamlist.Length; i++)
        {
            teamlist[i] = Instantiate(gm.TeamSlot, currentPos, Quaternion.identity).GetComponent<teamSlot>();
            //teamlist[i].buttonIndex = i;
            //teamlist[i].towerType = i + -2;//-2 and -1 are blocker and filler//43 gets to end

            teamlist[i].init();
            if ((i + 1) % rowLength == 0)
            {
                currentPos = currentPos + new Vector3(-(rowLength - 1) * listSpacing, -listSpacingVert, 0);
            }
            else
            {
                currentPos = currentPos + new Vector3(listSpacing, 0, 0);
            }
        }
    }

    public void fillTeamSlots()
    {
        for (int i = 0; i < gm.playerPieces.Count; i++)
        {
            teamlist[i].thisPiece = gm.playerPieces[i];
            gm.playerPieces[i].thisSlot = teamlist[i];
            gm.playerPieces[i].transform.position = teamlist[i].transform.position;
        }
        resetHighlighting();
    }

    public teamSlot findOpenSlot()
    {
        for (int i = 0;i<teamlist.Length;i++)
        {
            if (teamlist[i].thisPiece == null)
            {
                return teamlist[i];
            }
        }
        return null;
    }

    public void createText()
    {
        energy = new GameObject("energy");
        energy.transform.SetParent(FindObjectOfType<Canvas>().transform);
        energyText = energy.AddComponent<Text>();
        energyText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        energy.layer = 5;
        energyText.alignment = TextAnchor.MiddleCenter;
        energyText.color = new Color(0, 0, 0);
        energyText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100);
        energyText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 400);
        energyText.rectTransform.anchorMin = new Vector2(0, 0);
        energyText.rectTransform.anchorMax = new Vector2(0, 0);

        plays = new GameObject("plays");
        plays.transform.SetParent(FindObjectOfType<Canvas>().transform);
        playsText = plays.AddComponent<Text>();
        playsText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        plays.layer = 5;
        playsText.alignment = TextAnchor.MiddleCenter;
        playsText.color = new Color(0, 0, 0);
        playsText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100);
        playsText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 400);
        playsText.rectTransform.anchorMin = new Vector2(0, 0);
        playsText.rectTransform.anchorMax = new Vector2(0, 0);

        updateText();
    }

    public void updateText()
    {
        Camera c = Camera.main;

        energyPos = c.WorldToScreenPoint((transform.position + new Vector3(-5.5f, -3.5f, 0)));
        energyText.rectTransform.anchoredPosition = energyPos;
        energyText.fontSize = Mathf.FloorToInt(36 * (AspectUtility.screenWidth / 1612f));

        playsPos = c.WorldToScreenPoint((transform.position + new Vector3(-5.5f, -4f, 0)));
        playsText.rectTransform.anchoredPosition = playsPos;
        playsText.fontSize = Mathf.FloorToInt(36 * (AspectUtility.screenWidth / 1612f));

        setText();
    }

    public void setText()
    {
        energyText.text = "Energy: " + bm.playerEnergy;
        if (bm.playersTurn)
        {
            playsText.text = "Plays: " + bm.playsRemaining;
        }
        else
        {
            playsText.text = "Plays: 0";
        }
    }

    public void hideText()
    {
        energyText.text = "";
        playsText.text = "";
    }
}
