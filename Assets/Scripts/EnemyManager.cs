using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    
    [SerializeField] private TurnManager turnManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void startTurn()
    {
        //Placeholder for testing
        //Debug.Log("Starting Enemy turn");
        turnManager.nextTurn();
    }
}
