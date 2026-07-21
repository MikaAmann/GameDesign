using UnityEngine;
using UnityEngine.Tilemaps;

public class Services : MonoBehaviour
{
    private static Services instance;

    public static Services I
    {
        get
        {
            // Lazy: schuetzt gegen unbestimmte Awake-Reihenfolge
            if (instance == null)
            {
                instance = FindAnyObjectByType<Services>();

                if (instance == null)
                    Debug.LogError("Kein Services-Objekt in der Szene gefunden.");
            }
            return instance;
        }
    }

    [SerializeField] private GridState gridState;
    [SerializeField] private WalkabilityService walkability;
    [SerializeField] private ActionResolver resolver;
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private PlayerController playerController;
    

    public GridState Grid => gridState;
    public WalkabilityService Walkability => walkability;
    public ActionResolver Resolver => resolver;
    public Tilemap Tilemap => tilemap;

    public PlayerController PlayerController => playerController;
    
    private void Awake() => instance = this;
}