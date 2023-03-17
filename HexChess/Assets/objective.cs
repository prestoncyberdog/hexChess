using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class objective : MonoBehaviour
{
    public gameManager gm;
    public battleManager bm;

    public int team;
    public int hypoTeam;
    public tile thisTile;

    public Color playerColor;
    public Color enemyColor;
    public Color neutralColor;

    public void init()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;

        playerColor = new Color(0.2f, 0.2f, 1f);
        enemyColor = new Color(1f, 0.2f, 0.2f);
        neutralColor = new Color(1f, 0.4f, 1f);

        transform.localScale = new Vector3(bm.generator.tileScale * 0.9f, bm.generator.tileScale * 0.9f, 1);
        transform.Rotate(new Vector3(0, 0, 30));

        hypoTeam = team;
        setColor();
    }

    void Update()
    {
        
    }

    public void checkStatus(bool real)
    {
        int playerCount = 0;
        int enemyCount = 0;
        if (real)
        {
            for (int i = 0; i < thisTile.neighbors.Length; i++)
            {
                if (thisTile.neighbors[i] != null && thisTile.neighbors[i].thisPiece != null && thisTile.neighbors[i].thisPiece.team == 0)
                {
                    playerCount++;
                }
                else if (thisTile.neighbors[i] != null && thisTile.neighbors[i].thisPiece != null && thisTile.neighbors[i].thisPiece.team == 1)
                {
                    enemyCount++;
                }
            }
            if (playerCount - enemyCount >= 4)
            {
                team = 0;
            }
            else if (enemyCount - playerCount >= 4)
            {
                team = 1;
            }
        }
        else
        {
            for (int i = 0; i < thisTile.neighbors.Length; i++)
            {
                if (thisTile.neighbors[i] != null && thisTile.neighbors[i].hypoPiece != null && thisTile.neighbors[i].hypoPiece.team == 0)
                {
                    playerCount++;
                }
                else if (thisTile.neighbors[i] != null && thisTile.neighbors[i].hypoPiece != null && thisTile.neighbors[i].hypoPiece.team == 1)
                {
                    enemyCount++;
                }
            }
            if (playerCount - enemyCount >= 4)
            {
                hypoTeam = 0;
            }
            else if (enemyCount - playerCount >= 4)
            {
                hypoTeam = 1;
            }
        }
    }

    public void setColor()
    {
        if (team == -1)
        {
            this.GetComponent<SpriteRenderer>().color = neutralColor;
        }
        else if (team == 0)
        {
            this.GetComponent<SpriteRenderer>().color = playerColor;
        }
        else if (team == 1)
        {
            this.GetComponent<SpriteRenderer>().color = enemyColor;
        }
        else
        {
            Debug.Log("Objective is not on any team or neutral.");
        }
    }
}
