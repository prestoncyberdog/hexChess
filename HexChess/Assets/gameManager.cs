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
    public Transform Objective;
    public Transform MapGenerator;

    public battleManager bm;

    public List<piece> playerPieces;
    public Vector3 AWAY;

    void Start()
    {
        AWAY = new Vector3(1000, 1000, 0);
        //createInitialTeam();
    }

    public void createInitialTeam()
    {
        playerPieces = new List<piece>();
        for (int i = 0; i < 11; i++)
        {
            piece newPiece = Instantiate(Pieces[Random.Range(0,Pieces.Length)], AWAY, Quaternion.identity).GetComponent<piece>();
            newPiece.team = 0;
            newPiece.init();
            playerPieces.Add(newPiece);
        }
    }

    void Update()
    {
        
    }

    public void loadMap()
    {
        SceneManager.LoadScene("gameBoard");
    }
}
