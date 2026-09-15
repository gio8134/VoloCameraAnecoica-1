using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// BROKER POSIZIONI
///
/// Compatibile con:
/// Unity 6 - 6000.4.2f1
/// Built-in Render Pipeline
///
/// API:
///
/// GET /position
///
/// Risposta:
///
/// {
///     "x": 0.548,
///     "y": 0.954,
///     "z": -3.721
/// }
///
/// Il server HTTP gira su un thread separato.
/// Non vengono utilizzate API Unity non thread-safe
/// dal thread HTTP.
/// </summary>
public class BrokerPosizioni : MonoBehaviour
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
    // SERVER HTTP
    // =========================================================

    [Header("HTTP Server")]

    [Tooltip(
        "Porta TCP sulla quale ascolta il server HTTP."
    )]
    [SerializeField]
    private int port = 8080;


    [Tooltip(
        "Se attivo, il server ascolta su tutte le interfacce " +
        "di rete. Se disattivo, solo su localhost."
    )]
    [SerializeField]
    private bool listenOnAllInterfaces = false;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool logRequests = true;


    // =========================================================
    // SERVER
    // =========================================================

    private HttpListener httpListener;

    private Thread serverThread;

    private volatile bool serverRunning;


    // =========================================================
    // DIMENSIONI STANZA
    // =========================================================

    private float roomLength;

    private float roomWidth;

    private float roomHeight;


    // =========================================================
    // RANDOM
    // =========================================================

    private readonly System.Random random =
        new System.Random();


    private readonly object randomLock =
        new object();


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
                "BrokerPosizioni: nessun RoomGenerator " +
                "assegnato nell'Inspector."
            );

            return;
        }


        // -----------------------------------------------------
        // Lettura dimensioni stanza
        //
        // Questa operazione avviene sul main thread.
        // -----------------------------------------------------

        roomLength =
            roomGenerator.RoomLength;

        roomWidth =
            roomGenerator.RoomWidth;

        roomHeight =
            roomGenerator.RoomHeight;


        // -----------------------------------------------------
        // Controllo dimensioni
        // -----------------------------------------------------

        if (roomLength <= 0.0f ||
            roomWidth <= 0.0f ||
            roomHeight <= 0.0f)
        {
            Debug.LogError(
                "BrokerPosizioni: dimensioni stanza non valide."
            );

            return;
        }


        // -----------------------------------------------------
        // Avvio server
        // -----------------------------------------------------

        StartHttpServer();
    }


    // =========================================================
    // AVVIO SERVER
    // =========================================================

    private void StartHttpServer()
    {
        try
        {
            httpListener =
                new HttpListener();


            string host =
                listenOnAllInterfaces
                    ? "+"
                    : "localhost";


            string prefix =
                $"http://{host}:{port}/";


            httpListener.Prefixes.Add(
                prefix
            );


            httpListener.Start();


            serverRunning = true;


            serverThread =
                new Thread(
                    ServerLoop
                );


            serverThread.IsBackground = true;


            serverThread.Start();


            Debug.Log(
                "BrokerPosizioni: server HTTP avviato.\n" +
                $"Endpoint: http://localhost:{port}/position\n" +
                $"Stanza: {roomLength} x {roomWidth} x {roomHeight} m"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "BrokerPosizioni: impossibile avviare " +
                "il server HTTP.\n" +
                exception
            );
        }
    }


    // =========================================================
    // SERVER LOOP
    // =========================================================

    private void ServerLoop()
    {
        while (serverRunning)
        {
            try
            {
                HttpListenerContext context =
                    httpListener.GetContext();


                ProcessRequest(
                    context
                );
            }
            catch (HttpListenerException)
            {
                if (serverRunning)
                {
                    Debug.LogError(
                        "BrokerPosizioni: errore HttpListener."
                    );
                }
            }
            catch (ObjectDisposedException)
            {
                // Listener chiuso.
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "BrokerPosizioni: errore nel server:\n" +
                    exception
                );
            }
        }
    }


    // =========================================================
    // PROCESS REQUEST
    // =========================================================

    private void ProcessRequest(
        HttpListenerContext context)
    {
        HttpListenerRequest request =
            context.Request;


        HttpListenerResponse response =
            context.Response;


        string path =
            request.Url.AbsolutePath;


        if (logRequests)
        {
            Debug.Log(
                $"BrokerPosizioni: {request.HttpMethod} {path}"
            );
        }


        // =====================================================
        // GET /position
        // =====================================================

        if (request.HttpMethod == "GET" &&
            path == "/position")
        {
            HandlePositionRequest(
                response
            );

            return;
        }


        // =====================================================
        // 404
        // =====================================================

        SendJsonResponse(
            response,
            404,
            new ErrorResponse
            {
                error = "Endpoint not found"
            }
        );
    }


    // =========================================================
    // GET /position
    // =========================================================

    private void HandlePositionRequest(
        HttpListenerResponse response)
    {
        double x;

        double y;

        double z;


        // -----------------------------------------------------
        // Generazione posizione casuale
        //
        // System.Random è thread-safe grazie al lock.
        // -----------------------------------------------------

        lock (randomLock)
        {
            x =
                RandomRange(
                    -roomLength * 0.5,
                    roomLength * 0.5
                );


            y =
                RandomRange(
                    0.0,
                    roomHeight
                );


            z =
                RandomRange(
                    -roomWidth * 0.5,
                    roomWidth * 0.5
                );
        }


        // -----------------------------------------------------
        // Crea oggetto risposta
        // -----------------------------------------------------

        PositionResponse position =
            new PositionResponse
            {
                x = (float)x,
                y = (float)y,
                z = (float)z
            };


        // -----------------------------------------------------
        // Serializzazione JSON
        //
        // NON costruiamo più manualmente la stringa JSON.
        // JsonUtility si occupa della corretta conversione.
        // -----------------------------------------------------

        string json =
            JsonUtility.ToJson(
                position
            );


        if (logRequests)
        {
            Debug.Log(
                $"BrokerPosizioni: posizione generata {json}"
            );
        }


        // -----------------------------------------------------
        // Invio risposta
        // -----------------------------------------------------

        SendJsonResponse(
            response,
            200,
            json
        );
    }


    // =========================================================
    // RANDOM RANGE
    // =========================================================

    private double RandomRange(
        double min,
        double max)
    {
        return
            min +
            random.NextDouble() *
            (max - min);
    }


    // =========================================================
    // INVIO JSON
    // =========================================================

    private void SendJsonResponse(
        HttpListenerResponse response,
        int statusCode,
        object data)
    {
        string json =
            JsonUtility.ToJson(
                data
            );


        SendJsonResponse(
            response,
            statusCode,
            json
        );
    }


    // =========================================================
    // INVIO STRINGA JSON
    // =========================================================

    private void SendJsonResponse(
        HttpListenerResponse response,
        int statusCode,
        string json)
    {
        try
        {
            byte[] buffer =
                Encoding.UTF8.GetBytes(
                    json
                );


            response.StatusCode =
                statusCode;


            response.ContentType =
                "application/json; charset=utf-8";


            response.ContentEncoding =
                Encoding.UTF8;


            response.ContentLength64 =
                buffer.Length;


            using (Stream output =
                   response.OutputStream)
            {
                output.Write(
                    buffer,
                    0,
                    buffer.Length
                );
            }
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "BrokerPosizioni: errore durante " +
                "l'invio della risposta HTTP:\n" +
                exception
            );
        }
    }


    // =========================================================
    // JSON POSITION RESPONSE
    // =========================================================

    [Serializable]
    private class PositionResponse
    {
        public float x;

        public float y;

        public float z;
    }


    // =========================================================
    // JSON ERROR RESPONSE
    // =========================================================

    [Serializable]
    private class ErrorResponse
    {
        public string error;
    }


    // =========================================================
    // STOP
    // =========================================================

    private void OnDestroy()
    {
        StopHttpServer();
    }


    // =========================================================
    // STOP SERVER
    // =========================================================

    private void StopHttpServer()
    {
        serverRunning = false;


        if (httpListener != null)
        {
            try
            {
                httpListener.Stop();

                httpListener.Close();
            }
            catch
            {
                // Ignora errori di chiusura.
            }


            httpListener = null;
        }


        if (serverThread != null &&
            serverThread.IsAlive)
        {
            try
            {
                serverThread.Join(500);
            }
            catch
            {
                // Ignora errori di chiusura.
            }


            serverThread = null;
        }


        Debug.Log(
            "BrokerPosizioni: server HTTP arrestato."
        );
    }
}