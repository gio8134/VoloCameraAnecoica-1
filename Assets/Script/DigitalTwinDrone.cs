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
/// Funzionamento:
///
/// 1. Riceve un riferimento a RoomGenerator.
/// 2. Riceve dall'Inspector la URL dell'API /position.
/// 3. In Start() crea una Capsule.
/// 4. La Capsule ha dimensioni:
///      altezza = 10 cm
///      larghezza = 60 cm
/// 5. Crea e assegna un materiale opaco e ben visibile.
/// 6. Ogni 5 secondi effettua una GET HTTP.
/// 7. Legge la posizione {x,y,z} restituita dal server.
/// 8. Muove gradualmente la Capsule verso la nuova posizione.
/// </summary>
public class DigitalTwinDrone : MonoBehaviour
{
    // =========================================================
    // ROOM GENERATOR
    // =========================================================

    [Header("Room Generator")]

    [Tooltip(
        "Riferimento al GameObject che contiene RoomGenerator."
    )]
    [SerializeField]
    private RoomGenerator roomGenerator;


    // =========================================================
    // SERVER
    // =========================================================

    [Header("Position Server")]

    [Tooltip(
        "URL completa dell'API che restituisce una nuova " +
        "posizione. Esempio: http://localhost:8080/position"
    )]
    [SerializeField]
    private string positionUrl =
        "http://localhost:8080/position";


    // =========================================================
    // MOVIMENTO
    // =========================================================

    [Header("Movimento")]

    [Tooltip(
        "Velocità di movimento della Digital Twin Capsule " +
        "in metri al secondo."
    )]
    [SerializeField]
    private float movementSpeed = 2.0f;


    [Tooltip(
        "Intervallo tra una richiesta HTTP e la successiva."
    )]
    [SerializeField]
    private float requestInterval = 5.0f;


    // =========================================================
    // CAPSULA
    // =========================================================

    [Header("Digital Twin Capsule")]

    [Tooltip(
        "Larghezza/diametro della capsula in metri."
    )]
    [SerializeField]
    private float capsuleWidth = 0.60f;


    [Tooltip(
        "Altezza complessiva della capsula in metri."
    )]
    [SerializeField]
    private float capsuleHeight = 0.10f;


    [Tooltip(
        "Nome del GameObject creato."
    )]
    [SerializeField]
    private string capsuleName =
        "DigitalTwinDrone";


    // =========================================================
    // MATERIALE
    // =========================================================

    [Header("Materiale")]

    [Tooltip(
        "Colore della Digital Twin Capsule."
    )]
    [SerializeField]
    private Color capsuleColor =
        new Color(
            1.0f,
            0.05f,
            0.02f,
            1.0f
        );


    // =========================================================
    // RIFERIMENTI INTERNI
    // =========================================================

    private GameObject droneObject;

    private Renderer droneRenderer;

    private Material droneMaterial;


    // =========================================================
    // POSIZIONE TARGET
    // =========================================================

    private Vector3 targetPosition;

    private bool hasTargetPosition = false;


    // =========================================================
    // COROUTINE
    // =========================================================

    private Coroutine positionCoroutine;


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
        // Crea la Digital Twin
        // -----------------------------------------------------

        CreateDrone();


        // -----------------------------------------------------
        // Posizione iniziale
        //
        // Parte dal centro della stanza.
        // -----------------------------------------------------

        transform.position =
            new Vector3(
                0.0f,
                roomGenerator.RoomHeight * 0.5f,
                0.0f
            );


        // -----------------------------------------------------
        // Avvia polling HTTP
        // -----------------------------------------------------

        positionCoroutine =
            StartCoroutine(
                PositionPolling()
            );
    }


    // =========================================================
    // CREA DIGITAL TWIN
    // =========================================================

    private void CreateDrone()
    {
        // -----------------------------------------------------
        // Crea Primitive Capsule
        // -----------------------------------------------------

        droneObject =
            GameObject.CreatePrimitive(
                PrimitiveType.Capsule
            );


        droneObject.name =
            capsuleName;


        // -----------------------------------------------------
        // Parent
        //
        // La capsula viene inserita sotto il GameObject
        // che contiene questo script.
        // -----------------------------------------------------

        droneObject.transform.SetParent(
            transform
        );


        // -----------------------------------------------------
        // Posizione locale iniziale
        // -----------------------------------------------------

        droneObject.transform.localPosition =
            Vector3.zero;


        droneObject.transform.localRotation =
            Quaternion.identity;


        // -----------------------------------------------------
        // Dimensioni
        //
        // La Capsule Unity standard ha:
        //
        // altezza = 2
        // diametro = 1
        //
        // Per ottenere:
        //
        // larghezza = 0.60 m
        // altezza   = 0.10 m
        //
        // la scala viene calcolata in questo modo.
        // -----------------------------------------------------

        float scaleX =
            capsuleWidth;

        float scaleZ =
            capsuleWidth;

        float scaleY =
            capsuleHeight * 0.5f;


        droneObject.transform.localScale =
            new Vector3(
                scaleX,
                scaleY,
                scaleZ
            );


        // -----------------------------------------------------
        // Renderer
        // -----------------------------------------------------

        droneRenderer =
            droneObject.GetComponent<Renderer>();


        // -----------------------------------------------------
        // Crea materiale Built-in
        // -----------------------------------------------------

        CreateDroneMaterial();


        droneRenderer.material =
            droneMaterial;


        // -----------------------------------------------------
        // Collider
        //
        // Non serve per il movimento, quindi viene disabilitato.
        // -----------------------------------------------------

        CapsuleCollider capsuleCollider =
            droneObject.GetComponent<CapsuleCollider>();


        if (capsuleCollider != null)
        {
            capsuleCollider.enabled = false;
        }
    }


    // =========================================================
    // CREA MATERIALE
    // =========================================================

    private void CreateDroneMaterial()
    {
        Shader standardShader =
            Shader.Find(
                "Standard"
            );


        if (standardShader == null)
        {
            Debug.LogError(
                "DigitalTwinDrone: impossibile trovare " +
                "lo shader Standard."
            );

            return;
        }


        droneMaterial =
            new Material(
                standardShader
            );


        droneMaterial.name =
            "MAT_DigitalTwinDrone";


        // -----------------------------------------------------
        // Colore
        // -----------------------------------------------------

        droneMaterial.color =
            capsuleColor;


        // -----------------------------------------------------
        // Materiale completamente opaco
        // -----------------------------------------------------

        droneMaterial.SetFloat(
            "_Mode",
            0.0f
        );


        droneMaterial.SetInt(
            "_SrcBlend",
            (int)UnityEngine.Rendering.BlendMode.One
        );


        droneMaterial.SetInt(
            "_DstBlend",
            (int)UnityEngine.Rendering.BlendMode.Zero
        );


        droneMaterial.SetInt(
            "_ZWrite",
            1
        );


        droneMaterial.DisableKeyword(
            "_ALPHATEST_ON"
        );


        droneMaterial.DisableKeyword(
            "_ALPHABLEND_ON"
        );


        droneMaterial.DisableKeyword(
            "_ALPHAPREMULTIPLY_ON"
        );


        droneMaterial.renderQueue =
            -1;


        // -----------------------------------------------------
        // Non metallico
        // -----------------------------------------------------

        droneMaterial.SetFloat(
            "_Metallic",
            0.0f
        );


        // -----------------------------------------------------
        // Poco lucido
        // -----------------------------------------------------

        droneMaterial.SetFloat(
            "_Glossiness",
            0.20f
        );


        // -----------------------------------------------------
        // Emissione
        //
        // Aiuta a rendere la capsula molto visibile anche
        // nelle zone più scure della stanza.
        // -----------------------------------------------------

        droneMaterial.EnableKeyword(
            "_EMISSION"
        );


        Color emissionColor =
            capsuleColor *
            1.5f;


        droneMaterial.SetColor(
            "_EmissionColor",
            emissionColor
        );
    }


    // =========================================================
    // POLLING POSIZIONE
    // =========================================================

    private IEnumerator PositionPolling()
    {
        while (true)
        {
            // -------------------------------------------------
            // Richiede una nuova posizione
            // -------------------------------------------------

            yield return
                StartCoroutine(
                    GetNewPosition()
                );


            // -------------------------------------------------
            // Aspetta prima della prossima richiesta
            // -------------------------------------------------

            yield return
                new WaitForSeconds(
                    requestInterval
                );
        }
    }


    // =========================================================
    // HTTP GET
    // =========================================================

    private IEnumerator GetNewPosition()
    {
        using (UnityWebRequest request =
               UnityWebRequest.Get(positionUrl))
        {
            // -------------------------------------------------
            // Timeout
            // -------------------------------------------------

            request.timeout = 5;


            // -------------------------------------------------
            // Invia GET
            // -------------------------------------------------

            yield return
                request.SendWebRequest();


            // -------------------------------------------------
            // Controlla risultato
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
            // Legge JSON
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
            // Controlla risultato
            // -------------------------------------------------

            targetPosition =
                new Vector3(
                    positionResponse.x,
                    positionResponse.y,
                    positionResponse.z
                );


            hasTargetPosition = true;


            Debug.Log(
                "DigitalTwinDrone: nuova posizione ricevuta: " +
                targetPosition
            );
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
        // Movimento graduale
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
        if (positionCoroutine != null)
        {
            StopCoroutine(
                positionCoroutine
            );

            positionCoroutine = null;
        }


        if (droneMaterial != null)
        {
            Destroy(
                droneMaterial
            );

            droneMaterial = null;
        }
    }
}

