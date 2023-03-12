using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class teamSlot : MonoBehaviour
{
    public gameManager gm;
    public battleManager bm;
    public uiManager um;

    public Color defaultColor;
    public Color selectedColor;
    public Color emptyColor;

    public piece thisPiece;

    public void init()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;
        um = bm.um;
        float scale = 0.18f;
        transform.localScale = new Vector3(scale, scale, 1);
        defaultColor = new Color(0.6f, 0.6f, 0.6f);
        selectedColor = new Color(0.8f, 0.8f, 0.8f);
        emptyColor = new Color(0.4f, 0.4f, 0.4f);
        setColor();
    }

    void Update()
    {
        
    }

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (thisPiece != null)
            {
                bm.selectedPiece = thisPiece;
                bm.justClicked = true;
                bm.resetHighlighting();
                um.resetHighlighting();
                bm.holdingPiece = true;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (bm.selectedPiece != null && bm.holdingPiece && !bm.selectedPiece.alive)
            {
                bm.selectedPiece.moveToSlot(this);
                um.resetHighlighting();
            }
        }
    }

    public void setColor()
    {
        if (thisPiece == null)
        {
            this.GetComponent<SpriteRenderer>().color = emptyColor;
        }
        else if (bm.selectedPiece == thisPiece)
        {
            this.GetComponent<SpriteRenderer>().color = selectedColor;
        }
        else
        {
            this.GetComponent<SpriteRenderer>().color = defaultColor;
        }
    }
}
