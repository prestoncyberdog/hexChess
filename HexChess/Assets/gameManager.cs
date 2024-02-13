using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class gameManager : MonoBehaviour
{
    public Transform Tile;
    public Transform[] Pieces;
    public Transform EnemyManager;
    public Transform UIManager;
    public Transform TeamSlot;
    public Transform MapGenerator;
    public Transform ChampionMarker;
    public Transform HealthBar;
    public Transform HealthBarPip;

    public battleManager bm;

    public List<piece> playerPieces;
    public piece[] champions;
    public Vector3 AWAY;

    public int pushDamage;

    void Start()
    {
        AWAY = new Vector3(1000, 1000, 0);
        pushDamage = 1;
        //createInitialTeam();
    }

    public void createInitialTeam()
    {
        AWAY = new Vector3(1000, 1000, 0);
        
        playerPieces = new List<piece>();
        piece newPiece;
        for (int i = 0; i < 8; i++)
        {
            newPiece = Instantiate(Pieces[Random.Range(0,Pieces.Length)], AWAY, Quaternion.identity).GetComponent<piece>();
            newPiece.team = 0;
            newPiece.init();
            playerPieces.Add(newPiece);
        }

        champions = new piece[2];
        newPiece = Instantiate(Pieces[1], AWAY, Quaternion.identity).GetComponent<piece>();
        newPiece.team = 0;
        newPiece.init();
        newPiece.champion = true;
        champions[0] = newPiece;
    }

    void Update()
    {
        
    }

    public void loadMap()
    {
        SceneManager.LoadScene("gameBoard");
    }
}
