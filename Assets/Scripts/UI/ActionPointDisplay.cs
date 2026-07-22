using System.Collections.Generic;
using UnityEngine;

/// Zeigt Aktionspunkte als Reihe von Icons.
/// Reiner Consumer: kennt keine Spiellogik, nur Zahlen.
public class ActionPointDisplay : MonoBehaviour
{
    [SerializeField] private RectTransform container;  // hat die Horizontal Layout Group
    [SerializeField] private GameObject iconPrefab;

    private readonly List<GameObject> icons = new List<GameObject>();

    /// Passt die Anzahl der Slots an. Instanziiert/zerstoert nur bei Aenderung.
    private void SetMax(int max)
    {
        while (icons.Count < max)
            icons.Add(Instantiate(iconPrefab, container));

        while (icons.Count > max)
        {
            int last = icons.Count - 1;
            Destroy(icons[last]);
            icons.RemoveAt(last);
        }
    }

    /// Idempotent: gleicher Wert erzeugt immer das gleiche Bild.
    public void SetActionPoints(int current, int max)
    {
        SetMax(max);

        // Index 0 = ganz links. Deaktiviert wird also von rechts nach links.
        for (int i = 0; i < icons.Count; i++)
            icons[i].SetActive(i < current);
    }
}