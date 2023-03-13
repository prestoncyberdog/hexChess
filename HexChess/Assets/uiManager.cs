using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class uiManager : MonoBehaviour
{
    public gameManager gm;
    public battleManager bm;

    public teamSlot[] teamlist;

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

        createTeamSlots();
        fillTeamSlots();
        createText();
    }

    void Update()
    {
        updateText();
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
