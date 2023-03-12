using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class uiManager : MonoBehaviour
{
    public gameManager gm;
    public battleManager bm;

    public teamSlot[] teamlist;
    
    public void init()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;

        createTeamSlots();
        fillTeamSlots();
    }

    void Update()
    {
        
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
        teamlist = new teamSlot[20];
        float listSpacing = 1.3f;
        float listSpacingVert = 1.3f;
        int rowLength = 4;
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
}
