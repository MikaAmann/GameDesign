using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitView : MonoBehaviour
{
    [SerializeField] private float moveDuration = .12f;
    [SerializeField] private float attackSpeed  = .07f;
    [SerializeField] private float returnSpeed  = .12f;

    private Tilemap tilemap;

    private void Awake() => tilemap = Services.I.Tilemap;

    // Gleitet visuell zur Zielzelle. Die Belegung hat der Resolver schon committet.
    public IEnumerator MoveTo(Vector3Int cell)
    {
        Vector3 start  = transform.position;
        Vector3 target = tilemap.GetCellCenterWorld(cell);
        float elapsed  = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, elapsed / moveDuration);
            yield return null;
        }
        transform.position = target;
    }

    // Hop zur Zielzelle und zurueck. onImpact feuert im Moment des Aufpralls.
    // Die View weiss nicht, was dort passiert, sie kennt nur den Zeitpunkt.
    public IEnumerator AttackHop(Vector3Int targetCell, Action onImpact)
    {
        Vector3 home      = transform.position;
        Vector3 impactPos = tilemap.GetCellCenterWorld(targetCell);

        yield return Hop(home, impactPos, attackSpeed);

        onImpact?.Invoke();

        yield return Hop(impactPos, home, returnSpeed);
    }

    public void SnapToCell(Vector3Int cell)
    {
        transform.position = tilemap.GetCellCenterWorld(cell);
    }

    private IEnumerator Hop(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        transform.position = to;
    }
}