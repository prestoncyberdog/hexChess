using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class healthBar : MonoBehaviour
{
    public gameManager gm;
    public battleManager bm;

    public piece owner;
    public Transform[] pips;
    public Color[] colors;

    void Start()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;

        colors = new Color[4];
        colors[0] = new Color(0,.7f,0);
        colors[1] = new Color(1,0,0);
        colors[2] = new Color(0,0,0, 0.2f); //for missing health 
        colors[3] = new Color(0,0,0, 0.2f); //for missing health 

        createPips();
        setPositions();
        setColors();
        
        if (!owner.alive)
        {
            deactivate();
        }
    }

    //sets the positions for each pip in the health bar
    public void setPositions()
    {
        int rowSize = 5;
        float xjump = .4f * bm.generator.tileScale;
        float yjump = .18f * bm.generator.tileScale;
        Vector3 nextPos = owner.transform.position + new Vector3(xjump * - (rowSize - 1)/2, .4f, 0);

        for(int i = 0;i<pips.Length;i++)
        {
            pips[i].position = nextPos;

            if ((i+1) % 5 == 0)
            {
                nextPos = nextPos + new Vector3(xjump * -4, yjump, 0);
            }
            else
            {
                nextPos = nextPos + new Vector3(xjump, 0, 0);
            }
        }
    }

    public void setColors()
    {
        for (int i = 0;i<pips.Length;i++)
        {
            if (owner.health >= i+1)
            {
                pips[i].gameObject.GetComponent<SpriteRenderer>().color = colors[owner.team];
            }
            else
            {
                pips[i].gameObject.GetComponent<SpriteRenderer>().color = colors[owner.team + 2];
            }
        }
    }

    public void deactivate()
    {
        for (int i = 0;i<pips.Length;i++)
        {
            pips[i].gameObject.SetActive(false);
        }
        gameObject.SetActive(false);
    }

    public void reactivate()
    {
        gameObject.SetActive(true);
        for (int i = 0;i<pips.Length;i++)
        {
            pips[i].gameObject.SetActive(true);
        }
    }

    public void createPips()
    {
        pips = new Transform[owner.health];
        for (int i = 0;i<owner.health;i++)
        {
            pips[i] = Instantiate(gm.HealthBarPip, gm.AWAY, Quaternion.identity);
            pips[i].localScale = new Vector3(bm.generator.tileScale * .07f, bm.generator.tileScale * .025f, 1);
            pips[i].gameObject.GetComponent<SpriteRenderer>().color = colors[owner.team];
        }
    }

    public void destroyAll()
    {
        for (int i = 0;i<pips.Length;i++)
        {
            Destroy(pips[i].gameObject);
        }
        Destroy(gameObject);
    }

    void Update()
    {
        
    }
}
