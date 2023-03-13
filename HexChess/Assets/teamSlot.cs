using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class teamSlot : MonoBehaviour
{
    public gameManager gm;
    public battleManager bm;
    public uiManager um;

    public GameObject cost;
    public Text costText;
    Vector3 costPos;

    public Color defaultColor;
    public Color offTurnColor;
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
        offTurnColor = new Color(0.5f, 0.5f, 0.5f);
        selectedColor = new Color(0.8f, 0.8f, 0.8f);
        emptyColor = new Color(0.4f, 0.4f, 0.4f);
        setColor();
        createText();
    }

    void Update()
    {
        updateText();
        //setColor();
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
        if (bm.playersTurn && bm.playsRemaining > 0)
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
        else
        {
            if (thisPiece == null)
            {
                this.GetComponent<SpriteRenderer>().color = emptyColor;
            }
            else if (bm.selectedPiece == thisPiece)
            {
                this.GetComponent<SpriteRenderer>().color = defaultColor;
            }
            else
            {
                this.GetComponent<SpriteRenderer>().color = offTurnColor;
            }
        }
    }

    public void createText()
    {
        cost = new GameObject("cost");
        cost.transform.SetParent(FindObjectOfType<Canvas>().transform);
        costText = cost.AddComponent<Text>();
        costText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        cost.layer = 5;
        costText.alignment = TextAnchor.MiddleCenter;
        costText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100);
        costText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 100);
        costText.rectTransform.anchorMin = new Vector2(0, 0);
        costText.rectTransform.anchorMax = new Vector2(0, 0);
        updateText();
    }

    public void updateText()
    {
        Camera c = Camera.main;
        costPos = c.WorldToScreenPoint((transform.position + new Vector3(-0.32f, -0.32f, 0)));
        costText.rectTransform.anchoredPosition = costPos;
        costText.fontSize = Mathf.FloorToInt(20 * (AspectUtility.screenWidth / 1612f));
        setText();
    }

    public void setText()
    {
        if (thisPiece == null)
        {
            hideText();
        }
        else
        {
            costText.text = "" + thisPiece.cost;
            if (thisPiece.canAfford())
            {
                costText.color = new Color(0, 0, 1);
            }
            else
            {
                costText.color = new Color(1, 0, 0);
            }
        }
    }

    public void hideText()
    {
        costText.text = "";
    }
}
