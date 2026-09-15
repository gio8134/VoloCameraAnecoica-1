using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// DIGITAL TWIN DRONE
///
/// Compatibile con:
/// Unity 6 - 6000.4.2f1
/// Built-in Render Pipeline
///
/// Il Drone viene creato utilizzando un prefab assegnato
/// dall'Inspector.
///
/// Ogni 5 secondi viene effettuata una richiesta GET
/// all'URL configurato.
///
/// Il server deve restituire un JSON del tipo:
///
/// {"x":0.548,"y":0.954,"z":-3.721}
///
/// Il Drone viene quindi spostato gradualmente verso
/// la posizione ricevuta.
/// </summary>
public class DigitalTwinDrone : MonoBehaviour
{
    // =========================================================
    // ROOM GENERATOR
    // =========================================================

    [Header("Room Generator")]

    [Tooltip(
        "Riferimento al componente RoomGenerator."
    )]
    [SerializeField]
    private RoomGenerator roomGenerator;


    // =========================================================
    // DRONE PREFAB
    // =========================================================

    [Header("Drone Prefab")]

    [Tooltip(
        "Prefab del Drone. Trascinare qui " +
        "Assets/Drone.prefab."
    )]
    [SerializeField]
    private GameObject dronePrefab;


    // =========================================================
    // SERVER
    // =========================================================

    [Header("Position Server")]

    [Tooltip(
        "URL completa dell'API che restituisce la posizione. " +
        "Esempio: http://localhost:8080/position"
    )]
    [SerializeField]
    private string positionUrl =
        "http://localhost:8080/position";


    // =========================================================
    // MOVIMENTO
    // =========================================================

    [Header("Movimento")]

    [Tooltip(
        "Velocità di movimento del Drone in metri al secondo."
    )]
    [SerializeField]
    private float movementSpeed = 2.0f;


    [Tooltip(
        "Intervallo tra due richieste HTTP."
    )]
    [SerializeField]
    private float requestInterval = 5.0f;


    // =========================================================
    // HTTP
    // =========================================================

    [Header("HTTP")]

    [Tooltip(
        "Timeout della richiesta HTTP in secondi."
    )]
    [SerializeField]
    private int requestTimeout = 5;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool logResponses = true;


    // =========================================================
    // RIFERIMENTO DRONE
    // =========================================================

    private GameObject droneObject;


    // =========================================================
    // TARGET
    // =========================================================

    private Vector3 targetPosition;

    private bool hasTargetPosition = false;


    // =========================================================
    // COROUTINE
    // =========================================================

    private Coroutine positionCoroutine;


    // =========================================================
    // PROPRIETÀ PUBBLICHE
    // UTILIZZATE DA DigitalTwinTracker
    // =========================================================

    /// <summary>
    /// Indica se è stata ricevuta almeno una posizione
    /// dal server.
    /// </summary>
    public bool HasTargetPosition
    {
        get
        {
            return hasTargetPosition;
        }
    }


    /// <summary>
    /// Ultima posizione ricevuta dal server.
    /// </summary>
    public Vector3 TargetPosition
    {
        get
        {
            return targetPosition;
        }
    }


    /// <summary>
    /// Posizione corrente del GameObject Drone.
    /// </summary>
    public Vector3 CurrentPosition
    {
        get
        {
            if (droneObject == null)
            {
                return transform.position;
            }

            return droneObject.transform.position;
        }
    }


    /// <summary>
    /// Riferimento al GameObject Drone istanziato.
    /// </summary>
    public GameObject DroneObject
    {
        get
        {
            return droneObject;
        }
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // -----------------------------------------------------
        // Controllo RoomGenerator
        // -----------------------------------------------------

        if (roomGenerator == null)
        {
            Debug.LogError(
                "DigitalTwinDrone: nessun RoomGenerator " +
                "assegnato nell'Inspector."
            );

            return;
        }


        // -----------------------------------------------------
        // Controllo prefab
        // -----------------------------------------------------

        if (dronePrefab == null)
        {
            Debug.LogError(
                "DigitalTwinDrone: nessun Drone Prefab " +
                "assegnato nell'Inspector.\n" +
                "Trascinare Assets/Drone.prefab nel campo " +
                "'Drone Prefab'."
            );

            return;
        }


        // -----------------------------------------------------
        // Controllo URL
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(positionUrl))
        {
            Debug.LogError(
                "DigitalTwinDrone: la URL del server " +
                "non è stata configurata."
            );

            return;
        }


        // -----------------------------------------------------
        // Controllo velocità
        // -----------------------------------------------------

        if (movementSpeed < 0.0f)
        {
            movementSpeed = 0.0f;
        }


        // -----------------------------------------------------
        // Controllo intervallo
        // -----------------------------------------------------

        if (requestInterval < 0.1f)
        {
            requestInterval = 0.1f;
        }


        // -----------------------------------------------------
        // Controllo timeout
        // -----------------------------------------------------

        if (requestTimeout < 1)
        {
            requestTimeout = 1;
        }


        // -----------------------------------------------------
        // Crea Drone
        // -----------------------------------------------------

        CreateDrone();


        if (droneObject == null)
        {
            Debug.LogError(
                "DigitalTwinDrone: impossibile istanziare " +
                "il prefab Drone."
            );

            return;
        }


        // -----------------------------------------------------
        // Posizione iniziale
        // -----------------------------------------------------

        droneObject.transform.position =
            new Vector3(
                0.0f,
                roomGenerator.RoomHeight * 0.5f,
                0.0f
            );


        // -----------------------------------------------------
        // Nessun target iniziale
        // -----------------------------------------------------

        hasTargetPosition = false;


        // -----------------------------------------------------
        // Avvia polling
        // -----------------------------------------------------

        positionCoroutine =
            StartCoroutine(
                PositionPolling()
            );
    }


    // =========================================================
    // CREA DRONE
    // =========================================================

    private void CreateDrone()
    {
        droneObject =
            Instantiate(
                dronePrefab
            );


        droneObject.name =
            dronePrefab.name;


        droneObject.transform.SetParent(
            transform,
            true
        );


        droneObject.transform.position =
            transform.position;
    }


    // =========================================================
    // POLLING
    // =========================================================

    private IEnumerator PositionPolling()
    {
        while (true)
        {
            // -------------------------------------------------
            // Richiesta posizione
            // -------------------------------------------------

            yield return
                StartCoroutine(
                    GetNewPosition()
                );


            // -------------------------------------------------
            // Attesa
            // -------------------------------------------------

            yield return
                new WaitForSeconds(
                    requestInterval
                );
        }
    }


    // =========================================================
    // GET HTTP
    // =========================================================

    private IEnumerator GetNewPosition()
    {
        using (UnityWebRequest request =
               UnityWebRequest.Get(positionUrl))
        {
            // -------------------------------------------------
            // Timeout
            // -------------------------------------------------

            request.timeout =
                requestTimeout;


            // -------------------------------------------------
            // Invio
            // -------------------------------------------------

            yield return
                request.SendWebRequest();


            // -------------------------------------------------
            // Errore HTTP
            // -------------------------------------------------

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    "DigitalTwinDrone: errore HTTP.\n" +
                    $"URL: {positionUrl}\n" +
                    $"Errore: {request.error}"
                );

                yield break;
            }


            // -------------------------------------------------
            // Risposta
            // -------------------------------------------------

            string json =
                request.downloadHandler.text;


            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning(
                    "DigitalTwinDrone: il server ha " +
                    "restituito una risposta vuota."
                );

                yield break;
            }


            // -------------------------------------------------
            // Parsing JSON
            // -------------------------------------------------

            PositionResponse positionResponse;


            try
            {
                positionResponse =
                    JsonUtility.FromJson<PositionResponse>(
                        json
                    );
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "DigitalTwinDrone: impossibile " +
                    "interpretare il JSON.\n" +
                    $"Risposta: {json}\n" +
                    $"Errore: {exception.Message}"
                );

                yield break;
            }


            // -------------------------------------------------
            // Controllo null
            // -------------------------------------------------

            if (positionResponse == null)
            {
                Debug.LogWarning(
                    "DigitalTwinDrone: risposta JSON " +
                    "non valida.\n" +
                    $"Risposta: {json}"
                );

                yield break;
            }


            // -------------------------------------------------
            // Controllo numeri
            // -------------------------------------------------

            if (float.IsNaN(positionResponse.x) ||
                float.IsNaN(positionResponse.y) ||
                float.IsNaN(positionResponse.z) ||
                float.IsInfinity(positionResponse.x) ||
                float.IsInfinity(positionResponse.y) ||
                float.IsInfinity(positionResponse.z))
            {
                Debug.LogWarning(
                    "DigitalTwinDrone: coordinate non valide.\n" +
                    $"Risposta: {json}"
                );

                yield break;
            }


            // -------------------------------------------------
            // Nuovo target
            // -------------------------------------------------

            targetPosition =
                new Vector3(
                    positionResponse.x,
                    positionResponse.y,
                    positionResponse.z
                );


            hasTargetPosition =
                true;


            // -------------------------------------------------
            // Log
            // -------------------------------------------------

            if (logResponses)
            {
                Debug.Log(
                    "DigitalTwinDrone: nuova posizione ricevuta: " +
                    targetPosition
                );
            }
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!hasTargetPosition)
        {
            return;
        }


        if (droneObject == null)
        {
            return;
        }


        // -----------------------------------------------------
        // Movimento graduale verso il target
        // -----------------------------------------------------

        droneObject.transform.position =
            Vector3.MoveTowards(
                droneObject.transform.position,
                targetPosition,
                movementSpeed *
                Time.deltaTime
            );
    }


    // =========================================================
    // VERIFICA TARGET RAGGIUNTO
    // =========================================================

    /// <summary>
    /// Restituisce true quando il Drone si trova entro
    /// la tolleranza specificata dal target.
    /// </summary>
    public bool HasReachedTarget(
        float tolerance)
    {
        if (!hasTargetPosition)
        {
            return false;
        }


        if (droneObject == null)
        {
            return false;
        }


        if (tolerance < 0.0f)
        {
            tolerance = 0.0f;
        }


        float distance =
            Vector3.Distance(
                droneObject.transform.position,
                targetPosition
            );


        return distance <= tolerance;
    }


    // =========================================================
    // POSIZIONE CORRENTE
    // =========================================================

    /// <summary>
    /// Restituisce la posizione corrente del Drone.
    /// </summary>
    public Vector3 GetCurrentPosition()
    {
        if (droneObject == null)
        {
            return transform.position;
        }


        return droneObject.transform.position;
    }


    // =========================================================
    // JSON RESPONSE
    // =========================================================

    [Serializable]
    private class PositionResponse
    {
        public float x;

        public float y;

        public float z;
    }


    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        // -----------------------------------------------------
        // Ferma coroutine
        // -----------------------------------------------------

        if (positionCoroutine != null)
        {
            StopCoroutine(
                positionCoroutine
            );

            positionCoroutine = null;
        }


        // -----------------------------------------------------
        // Distrugge Drone
        // -----------------------------------------------------

        if (droneObject != null)
        {
            Destroy(
                droneObject
            );

            droneObject = null;
        }
    }
}
