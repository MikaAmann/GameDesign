using UnityEngine;

// Blendet die Belohnungsauswahl ein und haelt fest, was gewaehlt wurde.
// Logikfrei: entscheidet nicht, was die Wahl bewirkt.
public class RewardPanel : MonoBehaviour
{
    public enum Reward { None, Damage, Health }

    [SerializeField] private GameObject root;   // im Editor deaktiviert lassen

    private Reward choice = Reward.None;

    public bool HasChoice => choice != Reward.None;
    public Reward Choice  => choice;

    public void Show()
    {
        choice = Reward.None;
        root.SetActive(true);
    }

    public void Hide() => root.SetActive(false);

    // Diese beiden im OnClick der Buttons zuweisen
    public void ChooseDamage()
    {
        Debug.Log($"ChooseDamage auf Instanz {GetInstanceID()}");
        choice = Reward.Damage;
    } 
    public void ChooseHealth() => choice = Reward.Health;
}