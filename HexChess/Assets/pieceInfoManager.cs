using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class pieceInfoManager : MonoBehaviour
{
    public gameManager gm;
    public battleManager bm;
    public uiManager um;

    public GameObject pieceName;
    public Text pieceNameText;
    Vector3 pieceNamePos;

    public GameObject damage;
    public Text damageText;
    Vector3 damagePos;

    public GameObject health;
    public Text healthText;
    Vector3 healthPos;

    public GameObject cost;
    public Text costText;
    Vector3 costPos;

    public void init()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;
        um = gameObject.GetComponent<uiManager>();

        createText();
    }

    void Update()
    {
        
    }

    public void createText()
    {
        pieceName = new GameObject("pieceName");
        pieceName.transform.SetParent(FindObjectOfType<Canvas>().transform);
        pieceNameText = pieceName.AddComponent<Text>();
        pieceNameText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        pieceName.layer = 5;
        pieceNameText.alignment = TextAnchor.MiddleCenter;
        pieceNameText.color = new Color(0, 0, 0);
        pieceNameText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100);
        pieceNameText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 400);
        pieceNameText.rectTransform.anchorMin = new Vector2(0, 0);
        pieceNameText.rectTransform.anchorMax = new Vector2(0, 0);

        damage = new GameObject("damage");
        damage.transform.SetParent(FindObjectOfType<Canvas>().transform);
        damageText = damage.AddComponent<Text>();
        damageText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        damage.layer = 5;
        damageText.alignment = TextAnchor.MiddleCenter;
        damageText.color = new Color(0, 0, 0);
        damageText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100);
        damageText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 400);
        damageText.rectTransform.anchorMin = new Vector2(0, 0);
        damageText.rectTransform.anchorMax = new Vector2(0, 0);

        health = new GameObject("health");
        health.transform.SetParent(FindObjectOfType<Canvas>().transform);
        healthText = health.AddComponent<Text>();
        healthText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        health.layer = 5;
        healthText.alignment = TextAnchor.MiddleCenter;
        healthText.color = new Color(0, 0, 0);
        healthText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100);
        healthText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 400);
        healthText.rectTransform.anchorMin = new Vector2(0, 0);
        healthText.rectTransform.anchorMax = new Vector2(0, 0);

        cost = new GameObject("cost");
        cost.transform.SetParent(FindObjectOfType<Canvas>().transform);
        costText = cost.AddComponent<Text>();
        costText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        cost.layer = 5;
        costText.alignment = TextAnchor.MiddleCenter;
        costText.color = new Color(0, 0, 0);
        costText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100);
        costText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 400);
        costText.rectTransform.anchorMin = new Vector2(0, 0);
        costText.rectTransform.anchorMax = new Vector2(0, 0);

        updateText();
    }

    public void updateText()
    {
        Camera c = Camera.main;

        Vector3 startPoint = transform.position + new Vector3(-5.5f, .5f, 0);

        pieceNamePos = c.WorldToScreenPoint((startPoint + new Vector3(0, 0f, 0)));
        pieceNameText.rectTransform.anchoredPosition = pieceNamePos;
        pieceNameText.fontSize = Mathf.FloorToInt(20 * (AspectUtility.screenWidth / 1612f));

        damagePos = c.WorldToScreenPoint((startPoint +  new Vector3(0, -.3f, 0)));
        damageText.rectTransform.anchoredPosition = damagePos;
        damageText.fontSize = Mathf.FloorToInt(20 * (AspectUtility.screenWidth / 1612f));
        
        healthPos = c.WorldToScreenPoint((startPoint + new Vector3(0, -.6f, 0)));
        healthText.rectTransform.anchoredPosition = healthPos;
        healthText.fontSize = Mathf.FloorToInt(20 * (AspectUtility.screenWidth / 1612f));

        costPos = c.WorldToScreenPoint((startPoint + new Vector3(0, -.9f, 0)));
        costText.rectTransform.anchoredPosition = costPos;
        costText.fontSize = Mathf.FloorToInt(20 * (AspectUtility.screenWidth / 1612f));

        setText();
    }

    public void setText()
    {
        if (bm.selectedPiece == null)
        {
            hideText();
            return;
        }

        pieceNameText.text = "Name: " + bm.selectedPiece.pieceName;
        damageText.text = "Damage: " + bm.selectedPiece.damage;
        if (bm.selectedPiece.alive)
        {
            healthText.text = "Health: " + bm.selectedPiece.health + "/" + bm.selectedPiece.maxHealth;
            costText.text = "";
        }
        else
        {
            healthText.text = "Health: " + bm.selectedPiece.health;
            costText.text = "Cost: " + bm.selectedPiece.cost;
        }
    }

    public void hideText()
    {
        pieceNameText.text = "";
        damageText.text = "";
        healthText.text = "";
        costText.text = "";
    }
}
