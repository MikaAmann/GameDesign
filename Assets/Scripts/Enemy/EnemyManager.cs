using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private float attentionRadius = 20f;
    [SerializeField] private ActionResolver actionResolver;
    private List<Enemy> enemyList;

    void Start()
    {
        // Einmalig alle Gegner holen und die Enemy-Komponente rausziehen
        GameObject[] found = GameObject.FindGameObjectsWithTag("Enemy");
        enemyList = new List<Enemy>(found.Length);
        foreach (var go in found)
            enemyList.Add(go.GetComponent<Enemy>());
    }

    // Vom TurnManager aufgerufen, wenn der Gegnerzug startet
    public void ReadyEnemies()
    {
        StartCoroutine(RunEnemyTurn());
    }

    private IEnumerator RunEnemyTurn()
    {
        foreach (var enemy in enemyList)
        {
            // Ferne Gegner überspringen: kein Rechnen, keine Animation
            if (SqrDistanceToPlayer(enemy) >= attentionRadius * attentionRadius)
                continue;

            enemy.DecideTurn();              // instant, committet GridState
            yield return enemy.PlayTurn();   // wartet auf die Animation
        }

        turnManager.nextTurn();              // erst NACH allen Gegnern
    }

    private float SqrDistanceToPlayer(Enemy enemy)
    {
        Vector3Int diff = enemy.entity.CurrentCell - playerController.entity.CurrentCell;
        return diff.sqrMagnitude;
    }

    public void Unregister(Enemy enemy)
    {
        enemyList.Remove(enemy);
        actionResolver.Unregister(enemy.entity.CurrentCell);
    }
}