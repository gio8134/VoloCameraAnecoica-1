using UnityEngine;

/// <summary>
/// DIGITAL TWIN TRACKER
///
/// Compatibile con:
/// Unity 6 - 6000.4.2f1
/// Built-in Render Pipeline
///
/// Funzionamento:
///
/// Ogni 5 secondi:
///
/// 1. Legge la posizione REALE del Drone.
/// 2. Confronta la posizione con l'ultima posizione
///    nella quale è stata creata una sfera.
/// 3. Se la distanza è inferiore a 25 cm,
///    non crea una nuova sfera.
/// 4. Se la distanza è uguale o superiore a 25 cm,
///    crea una sfera rossa.
/// 5. La nuova sfera viene collegata alla precedente
///    tramite una linea gialla.
/// 6. La sfera viene distrutta dopo 30 secondi.
/// 7. Anche la relativa linea viene distrutta dopo 30 secondi.
///
/// La sfera ha raggio 20 cm.
///
/// Il materiale della sfera e quello della linea
/// vengono creati direttamente dallo script.
/// </summary>
public class DigitalTwinTracker : MonoBehaviour
{
    // =========================================================
    // DIGITAL TWIN DRONE
    // =========================================================

    [Header("Digital Twin Drone")]

    [Tooltip(
        "Riferimento al componente DigitalTwinDrone."
    )]
    [SerializeField]
    private DigitalTwinDrone digitalTwinDrone;


    // =========================================================
    // INTERVALLO DI CAMPIONAMENTO
    // =========================================================

    [Header("Tracking")]

    [Tooltip(
        "Intervallo in secondi tra due controlli della " +
        "posizione del Drone."
    )]
    [SerializeField]
    private float trackingInterval = 5.0f;


    [Tooltip(
        "Distanza minima tra due posizioni registrate. " +
        "0.25 = 25 centimetri."
    )]
    [SerializeField]
    private float minimumDistance = 0.25f;


    // =========================================================
    // SFERE
    // =========================================================

    [Header("Position Spheres")]

    [Tooltip(
        "Raggio delle sfere in metri. " +
        "0.20 = 20 centimetri."
    )]
    [SerializeField]
    private float sphereRadius = 0.20f;


    [Tooltip(
        "Tempo in secondi durante il quale ogni sfera " +
        "rimane visibile."
    )]
    [SerializeField]
    private float sphereLifetime = 30.0f;


    [Tooltip(
        "Colore delle sfere."
    )]
    [SerializeField]
    private Color sphereColor =
        Color.red;


    // =========================================================
    // LINEE
    // =========================================================

    [Header("Connection Lines")]

    [Tooltip(
        "Larghezza delle linee che collegano le sfere."
    )]
    [SerializeField]
    private float lineWidth = 0.04f;


    [Tooltip(
        "Colore delle linee."
    )]
    [SerializeField]
    private Color lineColor =
        Color.yellow;


    // =========================================================
    // MATERIALI
    // =========================================================

    private Material sphereMaterial;

    private Material lineMaterial;


    // =========================================================
    // ULTIMA POSIZIONE REGISTRATA
    // =========================================================

    private Vector3 lastRecordedPosition;

    private bool hasRecordedPosition = false;


    // =========================================================
    // TIMER
    // =========================================================

    private float trackingTimer = 0.0f;


    // =========================================================
    // ULTIMA SFERA
    // =========================================================

    private GameObject lastSphere;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // -----------------------------------------------------
        // Controllo DigitalTwinDrone
        // -----------------------------------------------------

        if (digitalTwinDrone == null)
        {
            Debug.LogError(
                "DigitalTwinTracker: nessun " +
                "DigitalTwinDrone assegnato nell'Inspector."
            );

            enabled = false;

            return;
        }


        // -----------------------------------------------------
        // Controllo intervallo
        // -----------------------------------------------------

        if (trackingInterval < 0.1f)
        {
            trackingInterval = 0.1f;
        }


        // -----------------------------------------------------
        // Controllo distanza
        // -----------------------------------------------------

        if (minimumDistance < 0.0f)
        {
            minimumDistance = 0.0f;
        }


        // -----------------------------------------------------
        // Controllo raggio
        // -----------------------------------------------------

        if (sphereRadius <= 0.0f)
        {
            sphereRadius = 0.20f;
        }


        // -----------------------------------------------------
        // Controllo durata
        // -----------------------------------------------------

        if (sphereLifetime <= 0.0f)
        {
            sphereLifetime = 30.0f;
        }


        // -----------------------------------------------------
        // Controllo spessore linea
        // -----------------------------------------------------

        if (lineWidth <= 0.0f)
        {
            lineWidth = 0.04f;
        }


        // -----------------------------------------------------
        // Crea materiali
        // -----------------------------------------------------

        CreateSphereMaterial();

        CreateLineMaterial();


        // -----------------------------------------------------
        // Prima verifica dopo trackingInterval
        // -----------------------------------------------------

        trackingTimer =
            trackingInterval;
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // -----------------------------------------------------
        // Timer
        // -----------------------------------------------------

        trackingTimer -=
            Time.deltaTime;


        if (trackingTimer > 0.0f)
        {
            return;
        }


        // -----------------------------------------------------
        // Reset timer
        // -----------------------------------------------------

        trackingTimer =
            trackingInterval;


        // -----------------------------------------------------
        // Controlla posizione Drone
        // -----------------------------------------------------

        TrackDronePosition();
    }


    // =========================================================
    // TRACK POSIZIONE DRONE
    // =========================================================

    private void TrackDronePosition()
    {
        // -----------------------------------------------------
        // Controllo riferimento
        // -----------------------------------------------------

        if (digitalTwinDrone == null)
        {
            return;
        }


        // -----------------------------------------------------
        // Controllo Drone
        // -----------------------------------------------------

        if (digitalTwinDrone.DroneObject == null)
        {
            return;
        }


        // -----------------------------------------------------
        // Legge la posizione REALE del Drone.
        // -----------------------------------------------------

        Vector3 currentPosition =
            digitalTwinDrone.CurrentPosition;


        // -----------------------------------------------------
        // Prima posizione
        //
        // La prima sfera viene sempre creata.
        // -----------------------------------------------------

        if (!hasRecordedPosition)
        {
            CreatePositionSphere(
                currentPosition
            );


            lastRecordedPosition =
                currentPosition;


            hasRecordedPosition =
                true;


            return;
        }


        // -----------------------------------------------------
        // Distanza dalla precedente sfera
        // -----------------------------------------------------

        float distance =
            Vector3.Distance(
                currentPosition,
                lastRecordedPosition
            );


        // -----------------------------------------------------
        // Movimento inferiore alla distanza minima
        // -----------------------------------------------------

        if (distance < minimumDistance)
        {
            return;
        }


        // -----------------------------------------------------
        // Il Drone si è spostato abbastanza.
        // Crea nuova sfera.
        // -----------------------------------------------------

        CreatePositionSphere(
            currentPosition
        );


        // -----------------------------------------------------
        // Memorizza posizione.
        // -----------------------------------------------------

        lastRecordedPosition =
            currentPosition;
    }


    // =========================================================
    // CREA SFERA
    // =========================================================

    private void CreatePositionSphere(
        Vector3 position)
    {
        // -----------------------------------------------------
        // Salva riferimento alla sfera precedente.
        // -----------------------------------------------------

        GameObject previousSphere =
            lastSphere;


        // -----------------------------------------------------
        // Crea Sphere
        // -----------------------------------------------------

        GameObject sphere =
            GameObject.CreatePrimitive(
                PrimitiveType.Sphere
            );


        // -----------------------------------------------------
        // Nome
        // -----------------------------------------------------

        sphere.name =
            "Drone_Position_Marker";


        // -----------------------------------------------------
        // Posizione
        // -----------------------------------------------------

        sphere.transform.position =
            position;


        // -----------------------------------------------------
        // Dimensione
        //
        // La primitive Sphere di Unity ha diametro 1 metro.
        //
        // Raggio desiderato = 0.20 m
        // Diametro = 0.40 m
        // -----------------------------------------------------

        float diameter =
            sphereRadius * 2.0f;


        sphere.transform.localScale =
            new Vector3(
                diameter,
                diameter,
                diameter
            );


        // -----------------------------------------------------
        // Materiale
        // -----------------------------------------------------

        Renderer renderer =
            sphere.GetComponent<Renderer>();


        if (renderer != null)
        {
            renderer.sharedMaterial =
                sphereMaterial;
        }


        // -----------------------------------------------------
        // Rimuove Collider.
        // -----------------------------------------------------

        Collider collider =
            sphere.GetComponent<Collider>();


        if (collider != null)
        {
            Destroy(
                collider
            );
        }


        // -----------------------------------------------------
        // Se esiste una sfera precedente, crea una linea
        // che collega la precedente alla nuova.
        // -----------------------------------------------------

        if (previousSphere != null)
        {
            CreateConnectionLine(
                previousSphere.transform.position,
                sphere.transform.position
            );
        }


        // -----------------------------------------------------
        // La nuova sfera diventa la precedente per il prossimo
        // punto.
        // -----------------------------------------------------

        lastSphere =
            sphere;


        // -----------------------------------------------------
        // Distruzione automatica della sfera.
        // -----------------------------------------------------

        Destroy(
            sphere,
            sphereLifetime
        );
    }


    // =========================================================
    // CREA LINEA
    // =========================================================

    private void CreateConnectionLine(
        Vector3 startPosition,
        Vector3 endPosition)
    {
        // -----------------------------------------------------
        // GameObject linea
        // -----------------------------------------------------

        GameObject lineObject =
            new GameObject(
                "Drone_Position_Connection"
            );


        // -----------------------------------------------------
        // Parent
        // -----------------------------------------------------

        lineObject.transform.SetParent(
            transform,
            true
        );


        // -----------------------------------------------------
        // LineRenderer
        // -----------------------------------------------------

        LineRenderer line =
            lineObject.AddComponent<LineRenderer>();


        // -----------------------------------------------------
        // Numero punti
        // -----------------------------------------------------

        line.positionCount =
            2;


        // -----------------------------------------------------
        // Posizioni
        // -----------------------------------------------------

        line.SetPosition(
            0,
            startPosition
        );


        line.SetPosition(
            1,
            endPosition
        );


        // -----------------------------------------------------
        // Spessore
        // -----------------------------------------------------

        line.startWidth =
            lineWidth;


        line.endWidth =
            lineWidth;


        // -----------------------------------------------------
        // Materiale
        // -----------------------------------------------------

        line.sharedMaterial =
            lineMaterial;


        // -----------------------------------------------------
        // Colore
        // -----------------------------------------------------

        line.startColor =
            lineColor;


        line.endColor =
            lineColor;


        // -----------------------------------------------------
        // La linea deve essere sempre visibile come elemento
        // 3D con illuminazione minima.
        // -----------------------------------------------------

        line.alignment =
            LineAlignment.View;


        // -----------------------------------------------------
        // Distruzione automatica.
        //
        // La linea ha la stessa durata della sfera appena
        // creata.
        // -----------------------------------------------------

        Destroy(
            lineObject,
            sphereLifetime
        );
    }


    // =========================================================
    // CREA MATERIALE SFERA
    // =========================================================

    private void CreateSphereMaterial()
    {
        // -----------------------------------------------------
        // Shader Built-in
        // -----------------------------------------------------

        Shader standardShader =
            Shader.Find(
                "Standard"
            );


        if (standardShader == null)
        {
            Debug.LogError(
                "DigitalTwinTracker: impossibile trovare " +
                "lo shader Standard."
            );

            return;
        }


        // -----------------------------------------------------
        // Materiale
        // -----------------------------------------------------

        sphereMaterial =
            new Material(
                standardShader
            );


        sphereMaterial.name =
            "MAT_Drone_Position_Red";


        // -----------------------------------------------------
        // Colore
        // -----------------------------------------------------

        sphereMaterial.color =
            sphereColor;


        // -----------------------------------------------------
        // Opaco
        // -----------------------------------------------------

        sphereMaterial.SetFloat(
            "_Metallic",
            0.0f
        );


        sphereMaterial.SetFloat(
            "_Glossiness",
            0.15f
        );


        sphereMaterial.SetFloat(
            "_Mode",
            0.0f
        );


        sphereMaterial.SetInt(
            "_SrcBlend",
            (int)UnityEngine.Rendering.BlendMode.One
        );


        sphereMaterial.SetInt(
            "_DstBlend",
            (int)UnityEngine.Rendering.BlendMode.Zero
        );


        sphereMaterial.SetInt(
            "_ZWrite",
            1
        );


        sphereMaterial.DisableKeyword(
            "_ALPHABLEND_ON"
        );


        sphereMaterial.DisableKeyword(
            "_ALPHAPREMULTIPLY_ON"
        );


        sphereMaterial.DisableKeyword(
            "_ALPHATEST_ON"
        );


        sphereMaterial.renderQueue =
            -1;
    }


    // =========================================================
    // CREA MATERIALE LINEA
    // =========================================================

    private void CreateLineMaterial()
    {
        // -----------------------------------------------------
        // Shader Built-in
        // -----------------------------------------------------

        Shader standardShader =
            Shader.Find(
                "Standard"
            );


        if (standardShader == null)
        {
            Debug.LogError(
                "DigitalTwinTracker: impossibile trovare " +
                "lo shader Standard per la linea."
            );

            return;
        }


        // -----------------------------------------------------
        // Materiale
        // -----------------------------------------------------

        lineMaterial =
            new Material(
                standardShader
            );


        lineMaterial.name =
            "MAT_Drone_Position_Line_Yellow";


        // -----------------------------------------------------
        // Giallo
        // -----------------------------------------------------

        lineMaterial.color =
            lineColor;


        // -----------------------------------------------------
        // Opaco
        // -----------------------------------------------------

        lineMaterial.SetFloat(
            "_Metallic",
            0.0f
        );


        lineMaterial.SetFloat(
            "_Glossiness",
            0.15f
        );


        lineMaterial.SetFloat(
            "_Mode",
            0.0f
        );


        lineMaterial.SetInt(
            "_SrcBlend",
            (int)UnityEngine.Rendering.BlendMode.One
        );


        lineMaterial.SetInt(
            "_DstBlend",
            (int)UnityEngine.Rendering.BlendMode.Zero
        );


        lineMaterial.SetInt(
            "_ZWrite",
            1
        );


        lineMaterial.DisableKeyword(
            "_ALPHABLEND_ON"
        );


        lineMaterial.DisableKeyword(
            "_ALPHAPREMULTIPLY_ON"
        );


        lineMaterial.DisableKeyword(
            "_ALPHATEST_ON"
        );


        lineMaterial.renderQueue =
            -1;
    }


    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        // -----------------------------------------------------
        // Distrugge materiale sfera
        // -----------------------------------------------------

        if (sphereMaterial != null)
        {
            Destroy(
                sphereMaterial
            );

            sphereMaterial = null;
        }


        // -----------------------------------------------------
        // Distrugge materiale linea
        // -----------------------------------------------------

        if (lineMaterial != null)
        {
            Destroy(
                lineMaterial
            );

            lineMaterial = null;
        }
    }
}
