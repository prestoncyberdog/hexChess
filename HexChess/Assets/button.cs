using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class button : MonoBehaviour
{
    public gameManager gm;

    public GameObject label;
    public Text labelText;
    Vector3 labelPos;

    public int fontSize;

    public bool mouseOver;

    public bool initialized;
    public float initializedDelay;
    public Color baseColor;
    public Color highlightColor;

    void Start()
    {
        if (!initialized)
        {
            init();
        }
    }

    public void init()
    {
        initializedDelay = .1f;
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();

        transform.localScale = new Vector3(0.15f, 0.05f, 1);
        baseColor = new Color(0.6f, 0.6f, 0.6f);
        highlightColor = new Color(0.8f, 0.8f, 0.8f);
        mouseOver = false;

        createText();

        updateText();
        setText();

        specificInit();
        this.GetComponent<SpriteRenderer>().color = baseColor;
    }

    public virtual void specificInit()
    {

    }

    void Update()
    {
        if (initializedDelay > 0)
        {
            initializedDelay -=  Time.deltaTime;
            if (initializedDelay<=0)
            {
                initialized = true;
            }
        }
        if (mouseOver && initialized)
        {
            this.GetComponent<SpriteRenderer>().color = highlightColor;
        }
        else
        {
            this.GetComponent<SpriteRenderer>().color = baseColor;
        }
        updateText();
        specificUpdate();
    }

    public virtual void specificUpdate()
    {

    }

    public void updateText()
    {
        Camera c = Camera.main;

        labelPos = c.WorldToScreenPoint(transform.position);
        labelText.rectTransform.anchoredPosition = labelPos;
        labelText.fontSize = Mathf.FloorToInt(fontSize * (AspectUtility.screenWidth / 1612f));

        setText();
    }

    private void OnMouseOver()
    {
        mouseOver = true;
        if (Input.GetMouseButtonDown(0))
        {
            doSomething();
            if (gm.bm != null)
            {
                gm.bm.justClicked = true;
            }
        }
    }

    private void OnMouseExit()
    {
        mouseOver = false;
    }

    public virtual void doSomething()
    {

    }

    public virtual void setText()
    {

    }

    public void destroyMenuButton()
    {
        Destroy(label);
        Destroy(gameObject);
    }

    public void deactivate()
    {
        labelText.text = "";
        gameObject.SetActive(false);
    }

    public void reactivate()
    {
        gameObject.SetActive(true);
        updateText();
    }

    public void createText()
    {
        label = new GameObject("label");
        label.transform.SetParent(FindObjectOfType<Canvas>().transform);
        labelText = label.AddComponent<Text>();
        labelText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        label.layer = 5;
        labelText.color = Color.black;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100);
        labelText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 500);
        labelText.rectTransform.anchorMin = new Vector2(0, 0);
        labelText.rectTransform.anchorMax = new Vector2(0, 0);
        fontSize = 30;
    }

}
