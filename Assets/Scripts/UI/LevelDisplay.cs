using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Zeigt Lebenspunkte als Balken. Reiner Consumer: kennt keine Spiellogik.
public class LevelDisplay : MonoBehaviour
{
    [SerializeField] private Image fill;              // Image Type = Filled
    [SerializeField] private TextMeshProUGUI level;
    [SerializeField] private TextMeshProUGUI label;   // optional

    public void SetLevel(int currentXP, int maxXP, int currentLevel)
    {
        fill.fillAmount = maxXP > 0 ? (float)currentXP / maxXP : 0f;
        
        level.text = currentLevel.ToString();

        if (label != null)
            label.text = $"{currentXP} / {maxXP}";
        
    }
}