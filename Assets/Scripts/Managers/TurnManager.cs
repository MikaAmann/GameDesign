using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private EnemyManager enemyManager;
    private turnOrder activeTurn;
    enum turnOrder
    {
        PLAYER,
        ENEMY
    }
  
    void Start()
    {
        activeTurn = turnOrder.PLAYER;
    }
    
    //Spieler und Enemy haben endTurn() die dann diese Funktion hier aufrufen
    //Spieler handelt das disablen seines eigenen Movements selbst
    //bspw. ruft er EndTurn() nach erfolgreicher MovementRequest auf und setzt dann eine bool flag auf false,
    //welche das Update direkt zu beginn verlässt und so movement verhindert und dann hier bei turn order wieder
    //aktiviert wird?
    public void nextTurn()
    {
        switch (activeTurn)
        {
            case turnOrder.PLAYER:
                activeTurn = turnOrder.ENEMY;
                //call EnemyManager to notify of TurnStart
                enemyManager.ReadyEnemies();
                break;
            
            case turnOrder.ENEMY:
                activeTurn = turnOrder.PLAYER;
                //call Player to notify of TurnStart
                player.setActiveTurn();
                break;
        }
    }
}
