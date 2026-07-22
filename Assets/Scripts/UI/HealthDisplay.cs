using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Zeigt Lebenspunkte als Balken. Reiner Consumer: kennt keine Spiellogik.
public class HealthDisplay : MonoBehaviour
{
    [SerializeField] private Image fill;              // Image Type = Filled
    [SerializeField] private TextMeshProUGUI label;   // optional

    public void SetHealth(int current, int max)
    {
        fill.fillAmount = max > 0 ? (float)current / max : 0f;

        if (label != null)
            label.text = $"{current} / {max}";
    }
}