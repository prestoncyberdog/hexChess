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

        transform.localScale = new Vector3(bm.generator.tileScale * 0.5f, bm.generator.tileScale * 0.5f, 1);

        hypoTeam = team;
        setColor();
    }

    void Update()
    {
        
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
