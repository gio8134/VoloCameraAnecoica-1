using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

public class DroneDriver : MonoBehaviour
{
    // =========================================================================
    // DRONE
    // =========================================================================

    [Header("Radar")]
    [SerializeField]
    private RadarSemiCircle radar;

    [Header("PuntoTarget")]
    [SerializeField]
    private Vector3 referencePoint = new Vector3(7.7f, 0f, 0f);

    [Header("Drone")]

    

    [Tooltip(
        "Root del prefab/GameObject del drone. " +
        "Il prefab può contenere MeshRenderer o SkinnedMeshRenderer nei figli."
    )]
    [SerializeField]
    private Transform drone;

    [Tooltip("Velocità di movimento in Unity units/second.")]
    [Min(0.001f)]
    [SerializeField]
    private float movementSpeed = 2.0f;

    [Tooltip("Distanza sotto la quale il drone viene considerato arrivato.")]
    [Min(0.0001f)]
    [SerializeField]
    private float arrivalThreshold = 0.01f;

    [Header("Visual Diagnostics")]

    [Tooltip(
        "Controlla automaticamente i Renderer del prefab " +
        "e prova ad abilitarli."
    )]
    [SerializeField]
    private bool autoEnableRenderers = true;

    [Tooltip("Stampa informazioni dettagliate sul prefab all'avvio.")]
    [SerializeField]
    private bool logDroneDiagnostics = true;

    // =========================================================================
    // HTTP SERVER
    // =========================================================================

    [Header("HTTP Server")]

    [Tooltip(
        "Indirizzo su cui ascoltare. " +
        "'localhost' accetta richieste dalla macchina locale."
    )]
    [SerializeField]
    private string listenHost = "localhost";

    [Tooltip("Porta del server HTTP.")]
    [Min(1)]
    [SerializeField]
    private int httpPort = 8080;

    [Tooltip("Endpoint REST.")]
    [SerializeField]
    private string endpoint = "/move";

    [Tooltip("Stampa nel Console le richieste HTTP ricevute.")]
    [SerializeField]
    private bool logRequests = true;

    // =========================================================================
    // DESTINATION MARKERS
    // =========================================================================

    [Header("Destination Markers")]

    [Tooltip("Raggio delle sfere generate al raggiungimento delle destinazioni.")]
    [Min(0.001f)]
    [SerializeField]
    private float markerRadius = 0.20f;

    [Tooltip("Colore delle sfere generate.")]
    [SerializeField]
    private Color markerColor = Color.red;

    // =========================================================================
    // PATH
    // =========================================================================

    [Header("Path")]

    [Tooltip("Colore delle linee che collegano due destinazioni consecutive.")]
    [SerializeField]
    private Color pathColor = Color.yellow;

    [Tooltip("Spessore delle linee.")]
    [Min(0.0001f)]
    [SerializeField]
    private float pathWidth = 0.02f;

    // =========================================================================
    // INTERNAL STATE
    // =========================================================================

    private HttpListener httpListener;
    private Thread httpThread;

    private volatile bool serverRunning;

    private readonly ConcurrentQueue<Vector3> targetQueue =
        new ConcurrentQueue<Vector3>();

    private Vector3 currentTarget;
    private bool hasTarget;

    private Vector3? previousMarkerPosition;

    private readonly object listenerLock = new object();

    private Renderer[] droneRenderers;


    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        Debug.Log("[DroneDriver] Awake()");

        if (!ValidateDrone())
        {
            Debug.LogError(
                "[DroneDriver] Il componente non può essere avviato " +
                "perché il drone non è valido."
            );

            return;
        }

        DiagnoseDrone();

        if (autoEnableRenderers)
        {
            EnsureDroneVisible();
        }

        StartHttpServer();
    }

    public void SetRadar()
    {
        float distanza = Vector3.Distance(drone.transform.position, referencePoint);
        Debug.Log($" distanza dal referencePoint = {distanza}");
        float scaglione = distanza / 5.0f;
        int settoreCurrent = Mathf.RoundToInt(scaglione);
        radar.SetSettore(settoreCurrent+1);
    }



    private void Update()
    {
        ProcessIncomingTargets();
        MoveDrone();
        SetRadar();
    }


    private void OnDestroy()
    {
        Debug.Log("[DroneDriver] OnDestroy()");

        StopHttpServer();
    }


    private void OnApplicationQuit()
    {
        Debug.Log("[DroneDriver] OnApplicationQuit()");

        StopHttpServer();
    }


    // =========================================================================
    // DRONE VALIDATION
    // =========================================================================

    private bool ValidateDrone()
    {
        if (drone == null)
        {
            Debug.LogError(
                "[DroneDriver] Nessun drone assegnato nell'Inspector.\n" +
                "Trascina nel campo 'Drone' il ROOT del prefab."
            );

            return false;
        }

        if (!drone.gameObject.activeSelf)
        {
            Debug.LogWarning(
                $"[DroneDriver] Il GameObject '{drone.name}' " +
                "è disattivato."
            );
        }

        return true;
    }


    // =========================================================================
    // DRONE DIAGNOSTICS
    // =========================================================================

    private void DiagnoseDrone()
    {
        if (drone == null)
            return;

        Debug.Log(
            $"[DroneDriver] Drone assegnato: '{drone.name}'"
        );

        Debug.Log(
            $"[DroneDriver] Position: {drone.position}"
        );

        Debug.Log(
            $"[DroneDriver] Rotation: {drone.rotation.eulerAngles}"
        );

        Debug.Log(
            $"[DroneDriver] Local Scale: {drone.localScale}"
        );

        Debug.Log(
            $"[DroneDriver] Lossy Scale: {drone.lossyScale}"
        );

        droneRenderers =
            drone.GetComponentsInChildren<Renderer>(true);

        if (droneRenderers == null ||
            droneRenderers.Length == 0)
        {
            Debug.LogError(
                $"[DroneDriver] NESSUN RENDERER trovato sotto '{drone.name}'.\n" +
                "Il prefab potrebbe non contenere una mesh oppure " +
                "potresti aver assegnato l'oggetto sbagliato."
            );

            return;
        }

        Debug.Log(
            $"[DroneDriver] Renderer trovati: {droneRenderers.Length}"
        );

        foreach (Renderer renderer in droneRenderers)
        {
            if (renderer == null)
                continue;

            string rendererType =
                renderer.GetType().Name;

            string objectName =
                renderer.gameObject.name;

            bool active =
                renderer.gameObject.activeInHierarchy;

            bool enabled =
                renderer.enabled;

            Material[] materials =
                renderer.sharedMaterials;

            Debug.Log(
                $"[DroneDriver] Renderer: '{objectName}' | " +
                $"Tipo={rendererType} | " +
                $"Enabled={enabled} | " +
                $"ActiveInHierarchy={active} | " +
                $"Materiali={materials.Length}"
            );

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];

                if (material == null)
                {
                    Debug.LogWarning(
                        $"[DroneDriver] Renderer '{objectName}' " +
                        $"ha il materiale {i} NULL."
                    );
                }
                else
                {
                    Debug.Log(
                        $"[DroneDriver]   Material[{i}] = " +
                        $"'{material.name}'"
                    );
                }
            }
        }
    }


    // =========================================================================
    // ENSURE DRONE VISIBILITY
    // =========================================================================

    private void EnsureDroneVisible()
    {
        if (drone == null)
            return;

        Debug.Log(
            "[DroneDriver] Controllo visibilità del drone..."
        );

        if (!drone.gameObject.activeSelf)
        {
            Debug.LogWarning(
                $"[DroneDriver] Il root '{drone.name}' era disattivato. " +
                "Lo abilito."
            );

            drone.gameObject.SetActive(true);
        }

        if (droneRenderers == null)
        {
            droneRenderers =
                drone.GetComponentsInChildren<Renderer>(true);
        }

        foreach (Renderer renderer in droneRenderers)
        {
            if (renderer == null)
                continue;

            if (!renderer.gameObject.activeSelf)
            {
                Debug.LogWarning(
                    $"[DroneDriver] Abilito il GameObject " +
                    $"'{renderer.gameObject.name}'."
                );

                renderer.gameObject.SetActive(true);
            }

            if (!renderer.enabled)
            {
                Debug.LogWarning(
                    $"[DroneDriver] Abilito Renderer " +
                    $"'{renderer.gameObject.name}'."
                );

                renderer.enabled = true;
            }
        }

        Debug.Log(
            "[DroneDriver] Controllo visibilità completato."
        );
    }


    // =========================================================================
    // TARGET MANAGEMENT
    // =========================================================================

    private void ProcessIncomingTargets()
    {
        Vector3 newTarget;

        bool receivedTarget = false;

        // Se arrivano più richieste prima del frame successivo,
        // utilizziamo l'ultima destinazione ricevuta.

        while (targetQueue.TryDequeue(out newTarget))
        {
            currentTarget = newTarget;
            receivedTarget = true;
        }

        if (!receivedTarget)
            return;

        hasTarget = true;

        if (logRequests)
        {
            Debug.Log(
                $"[DroneDriver] Nuova destinazione: " +
                $"X={currentTarget.x:F3}, " +
                $"Y={currentTarget.y:F3}, " +
                $"Z={currentTarget.z:F3}"
            );
        }
    }


    // =========================================================================
    // DRONE MOVEMENT
    // =========================================================================

    private void MoveDrone()
    {
        if (!hasTarget)
            return;

        if (drone == null)
            return;

        Vector3 currentPosition =
            drone.position;

        float distance =
            Vector3.Distance(
                currentPosition,
                currentTarget
            );

        if (distance <= arrivalThreshold)
        {
            drone.position = currentTarget;

            OnReachedDestination(
                currentTarget
            );

            hasTarget = false;

            return;
        }

        drone.position =
            Vector3.MoveTowards(
                currentPosition,
                currentTarget,
                movementSpeed * Time.deltaTime
            );

        // Controllo anche dopo il movimento.

        if (Vector3.Distance(
                drone.position,
                currentTarget) <= arrivalThreshold)
        {
            drone.position =
                currentTarget;

            OnReachedDestination(
                currentTarget
            );

            hasTarget = false;
        }
    }


    // =========================================================================
    // DESTINATION REACHED
    // =========================================================================

    private void OnReachedDestination(
        Vector3 position)
    {
        if (logRequests)
        {
            Debug.Log(
                $"[DroneDriver] Destinazione raggiunta: " +
                $"X={position.x:F3}, " +
                $"Y={position.y:F3}, " +
                $"Z={position.z:F3}"
            );
        }

        CreateMarker(position);
    }


    // =========================================================================
    // MARKERS
    // =========================================================================

    private void CreateMarker(
        Vector3 position)
    {
        GameObject sphere =
            GameObject.CreatePrimitive(
                PrimitiveType.Sphere
            );

        sphere.name =
            $"DroneDestination_{Time.frameCount}";

        sphere.transform.position =
            position;

        sphere.transform.localScale =
            Vector3.one *
            (markerRadius * 2.0f);

        Renderer sphereRenderer =
            sphere.GetComponent<Renderer>();

        if (sphereRenderer != null)
        {
            sphereRenderer.material.color =
                markerColor;
        }

        if (previousMarkerPosition.HasValue)
        {
            CreatePathLine(
                previousMarkerPosition.Value,
                position
            );
        }

        previousMarkerPosition =
            position;
    }


    // =========================================================================
    // PATH LINE
    // =========================================================================

    private void CreatePathLine(
        Vector3 start,
        Vector3 end)
    {
        GameObject lineObject =
            new GameObject(
                $"DronePath_{Time.frameCount}"
            );

        LineRenderer lineRenderer =
            lineObject.AddComponent<LineRenderer>();

        lineRenderer.useWorldSpace = true;

        lineRenderer.positionCount = 2;

        lineRenderer.SetPosition(
            0,
            start
        );

        lineRenderer.SetPosition(
            1,
            end
        );

        lineRenderer.startWidth =
            pathWidth;

        lineRenderer.endWidth =
            pathWidth;

        lineRenderer.startColor =
            pathColor;

        lineRenderer.endColor =
            pathColor;

        lineRenderer.numCapVertices = 4;

        Shader lineShader =
            Shader.Find("Sprites/Default");

        if (lineShader != null)
        {
            Material material =
                new Material(lineShader);

            material.color =
                pathColor;

            lineRenderer.material =
                material;
        }
        else
        {
            Debug.LogWarning(
                "[DroneDriver] Shader " +
                "'Sprites/Default' non trovato."
            );
        }
    }


    // =========================================================================
    // HTTP SERVER
    // =========================================================================

    private void StartHttpServer()
    {
        try
        {
            if (!HttpListener.IsSupported)
            {
                Debug.LogError(
                    "[DroneDriver] HttpListener " +
                    "non è supportato su questa piattaforma."
                );

                return;
            }

            httpListener =
                new HttpListener();

            string prefix =
                $"http://{listenHost}:{httpPort}/";

            httpListener.Prefixes.Add(
                prefix
            );

            httpListener.Start();

            serverRunning = true;

            httpThread =
                new Thread(
                    HttpServerLoop
                )
                {
                    IsBackground = true,
                    Name =
                        "DroneDriver_HTTP_Server"
                };

            httpThread.Start();

            Debug.Log(
                $"[DroneDriver] HTTP server AVVIATO su {prefix}"
            );

            Debug.Log(
                $"[DroneDriver] Endpoint: " +
                $"{prefix.TrimEnd('/')}" +
                $"{endpoint}" +
                "?x=10&y=5&z=-3"
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[DroneDriver] Impossibile avviare " +
                $"il server HTTP:\n{ex}"
            );

            StopHttpServer();
        }
    }


    // =========================================================================
    // HTTP LOOP
    // =========================================================================

    private void HttpServerLoop()
    {
        while (serverRunning)
        {
            HttpListenerContext context = null;

            try
            {
                context =
                    httpListener.GetContext();

                HandleHttpRequest(
                    context
                );
            }
            catch (HttpListenerException)
            {
                // Normale quando il listener viene fermato.

                if (serverRunning)
                {
                    Debug.LogWarning(
                        "[DroneDriver] Errore durante " +
                        "la ricezione della richiesta HTTP."
                    );
                }
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[DroneDriver] Errore HTTP:\n{ex}"
                );
            }
            finally
            {
                try
                {
                    context?.Response?.Close();
                }
                catch
                {
                    // Ignora errori durante la chiusura.
                }
            }
        }
    }


    // =========================================================================
    // HTTP REQUEST
    // =========================================================================

    private void HandleHttpRequest(
        HttpListenerContext context)
    {
        HttpListenerRequest request =
            context.Request;

        HttpListenerResponse response =
            context.Response;

        // ---------------------------------------------------------------------
        // CORS
        // ---------------------------------------------------------------------

        response.Headers[
            "Access-Control-Allow-Origin"
        ] = "*";

        response.Headers[
            "Access-Control-Allow-Methods"
        ] = "GET, OPTIONS";

        response.Headers[
            "Access-Control-Allow-Headers"
        ] = "Content-Type";

        // ---------------------------------------------------------------------
        // OPTIONS
        // ---------------------------------------------------------------------

        if (request.HttpMethod.Equals(
                "OPTIONS",
                StringComparison.OrdinalIgnoreCase))
        {
            SendJsonResponse(
                response,
                204,
                "{}"
            );

            return;
        }

        // ---------------------------------------------------------------------
        // PATH
        // ---------------------------------------------------------------------

        string requestPath =
            request.Url.AbsolutePath
                .TrimEnd('/');

        if (string.IsNullOrEmpty(requestPath))
            requestPath = "/";

        string expectedPath =
            endpoint.TrimEnd('/');

        // ---------------------------------------------------------------------
        // METHOD
        // ---------------------------------------------------------------------

        if (!request.HttpMethod.Equals(
                "GET",
                StringComparison.OrdinalIgnoreCase))
        {
            SendJsonResponse(
                response,
                405,
                "{\"error\":\"Only GET is supported.\"}"
            );

            return;
        }

        // ---------------------------------------------------------------------
        // ENDPOINT
        // ---------------------------------------------------------------------

        if (!string.Equals(
                requestPath,
                expectedPath,
                StringComparison.OrdinalIgnoreCase))
        {
            SendJsonResponse(
                response,
                404,
                "{\"error\":\"Endpoint not found.\"}"
            );

            return;
        }

        // ---------------------------------------------------------------------
        // PARAMETERS
        // ---------------------------------------------------------------------

        string xString =
            request.QueryString["x"];

        string yString =
            request.QueryString["y"];

        string zString =
            request.QueryString["z"];

        if (string.IsNullOrWhiteSpace(xString) ||
            string.IsNullOrWhiteSpace(yString) ||
            string.IsNullOrWhiteSpace(zString))
        {
            SendJsonResponse(
                response,
                400,
                "{\"error\":\"Parameters x, y and z are required.\"}"
            );

            return;
        }

        // ---------------------------------------------------------------------
        // PARSE
        // ---------------------------------------------------------------------

        if (!TryParseFloat(
                xString,
                out float x) ||
            !TryParseFloat(
                yString,
                out float y) ||
            !TryParseFloat(
                zString,
                out float z))
        {
            SendJsonResponse(
                response,
                400,
                "{\"error\":\"Parameters x, y and z must be valid numbers.\"}"
            );

            return;
        }

        // ---------------------------------------------------------------------
        // FINITE CHECK
        // ---------------------------------------------------------------------

        if (float.IsNaN(x) ||
            float.IsNaN(y) ||
            float.IsNaN(z) ||
            float.IsInfinity(x) ||
            float.IsInfinity(y) ||
            float.IsInfinity(z))
        {
            SendJsonResponse(
                response,
                400,
                "{\"error\":\"Coordinates must be finite numbers.\"}"
            );

            return;
        }

        Vector3 target =
            new Vector3(
                x,
                y,
                z
            );

        // ---------------------------------------------------------------------
        // THREAD SAFE QUEUE
        // ---------------------------------------------------------------------

        // IMPORTANTE:
        //
        // Questo metodo gira sul thread HTTP.
        //
        // Non modifichiamo Transform Unity da questo thread.
        //
        // Mettiamo la destinazione nella queue.
        //
        // Update() la leggerà sul main thread.

        targetQueue.Enqueue(
            target
        );

        // ---------------------------------------------------------------------
        // LOG
        // ---------------------------------------------------------------------

        if (logRequests)
        {
            Debug.Log(
                $"[DroneDriver] HTTP GET ricevuta: " +
                $"{request.Url}"
            );
        }

        // ---------------------------------------------------------------------
        // RESPONSE
        // ---------------------------------------------------------------------

        string json =
            "{" +
            "\"status\":\"accepted\"," +
            $"\"x\":{x.ToString(CultureInfo.InvariantCulture)}," +
            $"\"y\":{y.ToString(CultureInfo.InvariantCulture)}," +
            $"\"z\":{z.ToString(CultureInfo.InvariantCulture)}" +
            "}";

        SendJsonResponse(
            response,
            202,
            json
        );
    }


    // =========================================================================
    // FLOAT PARSING
    // =========================================================================

    private bool TryParseFloat(
        string value,
        out float result)
    {
        return float.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out result
        );
    }


    // =========================================================================
    // HTTP RESPONSE
    // =========================================================================

    private void SendJsonResponse(
        HttpListenerResponse response,
        int statusCode,
        string json)
    {
        byte[] data =
            Encoding.UTF8.GetBytes(
                json
            );

        response.StatusCode =
            statusCode;

        response.ContentType =
            "application/json";

        response.ContentEncoding =
            Encoding.UTF8;

        response.ContentLength64 =
            data.Length;

        try
        {
            using Stream output =
                response.OutputStream;

            output.Write(
                data,
                0,
                data.Length
            );
        }
        catch
        {
            // Il client potrebbe aver chiuso la connessione.
        }
    }


    // =========================================================================
    // SHUTDOWN
    // =========================================================================

    private void StopHttpServer()
    {
        serverRunning = false;

        lock (listenerLock)
        {
            if (httpListener != null)
            {
                try
                {
                    if (httpListener.IsListening)
                    {
                        httpListener.Stop();
                    }
                }
                catch
                {
                    // Ignora errori durante shutdown.
                }

                try
                {
                    httpListener.Close();
                }
                catch
                {
                    // Ignora errori durante shutdown.
                }

                httpListener = null;
            }
        }

        if (httpThread != null &&
            httpThread.IsAlive &&
            Thread.CurrentThread != httpThread)
        {
            try
            {
                httpThread.Join(500);
            }
            catch
            {
                // Ignora errori durante shutdown.
            }
        }

        httpThread = null;
    }
}
