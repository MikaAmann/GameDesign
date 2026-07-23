using UnityEngine;
using UnityEngine.Tilemaps;

// Truhe im Grid. Wird vom Spieler durch Betreten geoeffnet.
// Kennt nur die eigene Belohnung, keine Spiellogik.
public class Chest : MonoBehaviour
{
    [Header("Belohnung")]
    //[SerializeField] private int xpReward = 5;
    [SerializeField] private int healthBonus;
    [SerializeField] private int damageBonus;

    [Header("Darstellung")]
    [SerializeField] private Sprite openedSprite;

    private Vector3Int currentCell;
    private bool isOpened;

    public Vector3Int CurrentCell => currentCell;
    
    private void Start()
    {
        Tilemap tilemap = Services.I.Tilemap;
        currentCell = tilemap.WorldToCell(transform.position);
        Services.I.Grid.Register(currentCell, gameObject);   // Setup-Phase, Invariante 4
        transform.position = tilemap.GetCellCenterWorld(currentCell);
    }

    // Belohnung vergeben und Optik umstellen. Das Freigeben der Zelle
    // macht der Aufrufer ueber den ActionResolver.
    public void Open(RewardPanel.Reward reward)
    {
        if (isOpened) return;
        isOpened = true;

        UnitStats playerStats = Services.I.PlayerController.GetComponent<UnitStats>();

        if (reward == RewardPanel.Reward.Damage)
            playerStats.RaiseDamage(damageBonus);
        else
            playerStats.RaiseMaxHealth(healthBonus);

        if (openedSprite != null)
            GetComponent<SpriteRenderer>().sprite = openedSprite;
    }
}