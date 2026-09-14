using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SimulazioneDrone
///
/// Controlla una piccola capsula che rappresenta il personaggio/drone
/// all'interno della stanza generata da RoomGenerator.
///
/// La stanza viene assegnata dall'Inspector tramite il campo:
/// Room Generator
///
/// La capsula:
/// - viene creata al centro della stanza;
/// - misura 50 cm di larghezza e 20 cm di altezza;
/// - rimane ferma per 3 secondi;
/// - successivamente inizia a muoversi;
/// - cambia casualmente direzione e velocità;
/// - mantiene almeno 70 cm di distanza dai coni/piramidi;
/// - rimane all'interno della stanza.
///
/// La Main Camera segue automaticamente la capsula in terza persona.
/// </summary>
public class SimulazioneDrone : MonoBehaviour
{
    // =========================================================
    // ROOM GENERATOR
    // =========================================================

    [Header("Stanza")]

    [Tooltip(
        "Trascina qui il GameObject che contiene RoomGenerator."
    )]
    [SerializeField]
    private RoomGenerator roomGenerator;


    // =========================================================
    // CAPSULA
    // =========================================================

    [Header("Capsula")]

    [Tooltip("Larghezza/diametro della capsula in metri.")]
    [SerializeField]
    private float capsuleWidth = 0.50f;

    [Tooltip("Altezza della capsula in metri.")]
    [SerializeField]
    private float capsuleHeight = 0.20f;


    // =========================================================
    // AVVIO MOVIMENTO
    // =========================================================

    [Header("Avvio")]

    [Tooltip("Tempo di attesa prima dell'inizio del movimento.")]
    [SerializeField]
    private float startDelay = 3.0f;


    // =========================================================
    // MOVIMENTO
    // =========================================================

    [Header("Movimento")]

    [Tooltip("Velocità minima della capsula.")]
    [SerializeField]
    private float minSpeed = 0.25f;

    [Tooltip("Velocità massima della capsula.")]
    [SerializeField]
    private float maxSpeed = 0.75f;

    [Tooltip("Tempo minimo prima di cambiare direzione.")]
    [SerializeField]
    private float minDirectionTime = 2.0f;

    [Tooltip("Tempo massimo prima di cambiare direzione.")]
    [SerializeField]
    private float maxDirectionTime = 5.0f;

    [Tooltip(
        "Quanto rapidamente la capsula ruota verso la nuova direzione."
    )]
    [SerializeField]
    private float rotationSpeed = 2.0f;


    // =========================================================
    // DISTANZA DAI CONI
    // =========================================================

    [Header("Distanza dai coni")]

    [Tooltip(
        "Distanza minima dai coni/piramidi in metri."
    )]
    [SerializeField]
    private float minimumConeDistance = 0.70f;


    // =========================================================
    // ALTEZZA DI MOVIMENTO
    // =========================================================

    [Header("Altezza movimento")]

    [Tooltip(
        "Altezza del centro della capsula dal pavimento."
    )]
    [SerializeField]
    private float movementHeight = 4.50f;


    // =========================================================
    // CAMERA
    // =========================================================

    [Header("Main Camera")]

    [Tooltip(
        "Distanza della camera dietro la capsula."
    )]
    [SerializeField]
    private float cameraDistance = 3.0f;

    [Tooltip(
        "Altezza della camera rispetto alla capsula."
    )]
    [SerializeField]
    private float cameraHeight = 1.20f;

    [Tooltip(
        "Quanto rapidamente la camera segue la capsula."
    )]
    [SerializeField]
    private float cameraFollowSpeed = 5.0f;

    [Tooltip(
        "Quanto rapidamente la camera ruota verso la capsula."
    )]
    [SerializeField]
    private float cameraRotationSpeed = 5.0f;


    // =========================================================
    // MATERIALE
    // =========================================================

    [Header("Materiale capsula")]

    [SerializeField]
    private Color capsuleColor =
        new Color(
            0.15f,
            0.55f,
            1.00f,
            1.00f
        );


    // =========================================================
    // RIFERIMENTI INTERNI
    // =========================================================

    private GameObject capsule;

    private Rigidbody capsuleRigidbody;

    private Camera mainCamera;

    private Material capsuleMaterial;


    // =========================================================
    // DATI STANZA
    // =========================================================

    private float roomLength;
    private float roomWidth;
    private float roomHeight;


    // =========================================================
    // CONI
    // =========================================================

    private readonly List<Collider> coneColliders =
        new List<Collider>();


    // =========================================================
    // MOVIMENTO
    // =========================================================

    private Vector3 currentDirection;

    private Vector3 targetDirection;

    private float currentSpeed;

    private float directionTimer;

    private bool movementStarted;


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
                "SimulazioneDrone: RoomGenerator non assegnato."
            );

            return;
        }


        // -----------------------------------------------------
        // Legge dimensioni stanza
        // -----------------------------------------------------

        roomLength =
            roomGenerator.RoomLength;

        roomWidth =
            roomGenerator.RoomWidth;

        roomHeight =
            roomGenerator.RoomHeight;


        // -----------------------------------------------------
        // Cerca Main Camera
        // -----------------------------------------------------

        mainCamera =
            Camera.main;


        if (mainCamera == null)
        {
            Debug.LogError(
                "SimulazioneDrone: Main Camera non trovata."
            );

            return;
        }


        // -----------------------------------------------------
        // Cerca i collider dei coni
        // -----------------------------------------------------

        FindConeColliders();


        // -----------------------------------------------------
        // Crea capsula
        // -----------------------------------------------------

        CreateCapsule();


        // -----------------------------------------------------
        // Posiziona al centro della stanza
        // -----------------------------------------------------

        PlaceCapsuleAtRoomCenter();


        // -----------------------------------------------------
        // Prepara movimento
        // -----------------------------------------------------

        ChooseNewDirection();


        // -----------------------------------------------------
        // Prepara camera
        // -----------------------------------------------------

        SetupCamera();


        // -----------------------------------------------------
        // Avvio ritardato
        // -----------------------------------------------------

        StartCoroutine(
            StartMovementAfterDelay()
        );
    }


    // =========================================================
    // CERCA COLLIDER DEI CONI
    // =========================================================

    private void FindConeColliders()
    {
        coneColliders.Clear();


        if (roomGenerator.RoomRoot == null)
        {
            Debug.LogWarning(
                "SimulazioneDrone: RoomRoot non è ancora disponibile. " +
                "Assicurati che RoomGenerator abbia generato la stanza."
            );

            return;
        }


        Transform absorbers =
            roomGenerator.RoomRoot.Find(
                "Acoustic_Absorbers"
            );


        if (absorbers == null)
        {
            Debug.LogWarning(
                "SimulazioneDrone: oggetto " +
                "'Acoustic_Absorbers' non trovato."
            );

            return;
        }


        Collider[] colliders =
            absorbers.GetComponentsInChildren<Collider>(
                true
            );


        foreach (Collider collider in colliders)
        {
            if (collider != null &&
                collider.enabled)
            {
                coneColliders.Add(
                    collider
                );
            }
        }


        Debug.Log(
            "SimulazioneDrone: trovati " +
            coneColliders.Count +
            " collider dei coni."
        );
    }


    // =========================================================
    // CREA CAPSULA
    // =========================================================

    private void CreateCapsule()
    {
        capsule =
            GameObject.CreatePrimitive(
                PrimitiveType.Capsule
            );


        capsule.name =
            "Drone_Capsule";


        // -----------------------------------------------------
        // DIMENSIONI
        // -----------------------------------------------------

        //
        // Unity Capsule:
        // diametro = 1
        // altezza = 2
        //
        // Per ottenere:
        // larghezza = 0.50 m
        // altezza   = 0.20 m
        //
        // non possiamo usare semplicemente:
        // scale = (0.5, 0.2, 0.5)
        //
        // perché la geometria originale della Capsule
        // è alta 2 metri.
        //
        // Usiamo invece:
        // X = 0.50
        // Y = 0.10
        // Z = 0.50
        //

        capsule.transform.localScale =
            new Vector3(
                capsuleWidth,
                capsuleHeight * 0.5f,
                capsuleWidth
            );


        // -----------------------------------------------------
        // MATERIAL
        // -----------------------------------------------------

        Shader standardShader =
            Shader.Find("Standard");


        if (standardShader != null)
        {
            capsuleMaterial =
                new Material(
                    standardShader
                );


            capsuleMaterial.name =
                "MAT_DroneCapsule";


            capsuleMaterial.color =
                capsuleColor;


            capsuleMaterial.SetFloat(
                "_Metallic",
                0.20f
            );


            capsuleMaterial.SetFloat(
                "_Glossiness",
                0.65f
            );


            Renderer renderer =
                capsule.GetComponent<Renderer>();


            renderer.material =
                capsuleMaterial;
        }


        // -----------------------------------------------------
        // COLLIDER
        // -----------------------------------------------------

        CapsuleCollider collider =
            capsule.GetComponent<CapsuleCollider>();


        collider.enabled =
            true;


        collider.isTrigger =
            false;


        // -----------------------------------------------------
        // RIGIDBODY
        // -----------------------------------------------------

        capsuleRigidbody =
            capsule.AddComponent<Rigidbody>();


        capsuleRigidbody.useGravity =
            false;


        capsuleRigidbody.isKinematic =
            true;


        capsuleRigidbody.constraints =
            RigidbodyConstraints.FreezeRotation;


        // -----------------------------------------------------
        // Collision detection
        // -----------------------------------------------------

        capsuleRigidbody.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;
    }


    // =========================================================
    // POSIZIONE CENTRALE
    // =========================================================

    private void PlaceCapsuleAtRoomCenter()
    {
        Vector3 center =
            new Vector3(
                0.0f,
                movementHeight,
                0.0f
            );


        capsule.transform.position =
            center;


        capsule.transform.rotation =
            Quaternion.identity;
    }


    // =========================================================
    // COROUTINE AVVIO
    // =========================================================

    private IEnumerator StartMovementAfterDelay()
    {
        movementStarted =
            false;


        yield return new WaitForSeconds(
            startDelay
        );


        movementStarted =
            true;


        ChooseNewDirection();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (capsule == null)
            return;


        if (!movementStarted)
            return;


        UpdateMovement();
    }


    // =========================================================
    // MOVIMENTO
    // =========================================================

    private void UpdateMovement()
    {
        directionTimer -=
            Time.deltaTime;


        // -----------------------------------------------------
        // Cambio casuale di direzione
        // -----------------------------------------------------

        if (directionTimer <= 0.0f)
        {
            ChooseNewDirection();
        }


        // -----------------------------------------------------
        // Rotazione fluida verso la direzione target
        // -----------------------------------------------------

        currentDirection =
            Vector3.Slerp(
                currentDirection,
                targetDirection,
                rotationSpeed *
                Time.deltaTime
            );


        currentDirection.Normalize();


        // -----------------------------------------------------
        // Nuova posizione
        // -----------------------------------------------------

        Vector3 newPosition =
            capsule.transform.position +
            currentDirection *
            currentSpeed *
            Time.deltaTime;


        // -----------------------------------------------------
        // Mantieni altezza
        // -----------------------------------------------------

        newPosition.y =
            movementHeight;


        // -----------------------------------------------------
        // Controllo distanza dai coni
        // -----------------------------------------------------

        if (IsTooCloseToCone(
            newPosition
        ))
        {
            ChooseDirectionAwayFromCones();

            return;
        }


        // -----------------------------------------------------
        // Controllo limiti stanza
        // -----------------------------------------------------

        if (IsOutsideSafeRoomArea(
            newPosition
        ))
        {
            ChooseDirectionInsideRoom();

            return;
        }


        // -----------------------------------------------------
        // Applica posizione
        // -----------------------------------------------------

        capsule.transform.position =
            newPosition;
    }


    // =========================================================
    // NUOVA DIREZIONE CASUALE
    // =========================================================

    private void ChooseNewDirection()
    {
        Vector2 random =
            Random.insideUnitCircle.normalized;


        if (random.sqrMagnitude <
            0.01f)
        {
            random =
                Vector2.right;
        }


        targetDirection =
            new Vector3(
                random.x,
                0.0f,
                random.y
            );


        targetDirection.Normalize();


        currentSpeed =
            Random.Range(
                minSpeed,
                maxSpeed
            );


        directionTimer =
            Random.Range(
                minDirectionTime,
                maxDirectionTime
            );
    }


    // =========================================================
    // DIREZIONE LONTANO DAI CONI
    // =========================================================

    private void ChooseDirectionAwayFromCones()
    {
        Vector3 position =
            capsule.transform.position;


        Vector3 escapeDirection =
            Vector3.zero;


        int validColliders =
            0;


        foreach (Collider cone in coneColliders)
        {
            if (cone == null ||
                !cone.enabled)
            {
                continue;
            }


            Vector3 closestPoint =
                cone.ClosestPoint(
                    position
                );


            Vector3 away =
                position -
                closestPoint;


            float distance =
                away.magnitude;


            if (distance <
                minimumConeDistance)
            {
                if (distance >
                    0.001f)
                {
                    escapeDirection +=
                        away.normalized;
                }
                else
                {
                    escapeDirection +=
                        Random.insideUnitSphere;
                }


                validColliders++;
            }
        }


        if (validColliders == 0)
        {
            ChooseNewDirection();
            return;
        }


        escapeDirection.y =
            0.0f;


        if (escapeDirection.sqrMagnitude <
            0.001f)
        {
            ChooseNewDirection();
            return;
        }


        escapeDirection.Normalize();


        targetDirection =
            escapeDirection;


        currentDirection =
            escapeDirection;


        currentSpeed =
            Random.Range(
                minSpeed,
                maxSpeed
            );


        directionTimer =
            Random.Range(
                minDirectionTime,
                maxDirectionTime
            );
    }


    // =========================================================
    // DIREZIONE VERSO INTERNO STANZA
    // =========================================================

    private void ChooseDirectionInsideRoom()
    {
        Vector3 position =
            capsule.transform.position;


        Vector3 direction =
            Vector3.zero;


        float halfLength =
            roomLength * 0.5f;


        float halfWidth =
            roomWidth * 0.5f;


        float margin =
            1.0f;


        // -----------------------------------------------------
        // Parete sinistra
        // -----------------------------------------------------

        if (position.x <
            -halfLength + margin)
        {
            direction +=
                Vector3.right;
        }


        // -----------------------------------------------------
        // Parete destra
        // -----------------------------------------------------

        if (position.x >
            halfLength - margin)
        {
            direction +=
                Vector3.left;
        }


        // -----------------------------------------------------
        // Parete posteriore
        // -----------------------------------------------------

        if (position.z <
            -halfWidth + margin)
        {
            direction +=
                Vector3.forward;
        }


        // -----------------------------------------------------
        // Parete anteriore
        // -----------------------------------------------------

        if (position.z >
            halfWidth - margin)
        {
            direction +=
                Vector3.back;
        }


        if (direction.sqrMagnitude <
            0.001f)
        {
            ChooseNewDirection();
            return;
        }


        direction.Normalize();


        targetDirection =
            direction;


        currentDirection =
            direction;


        directionTimer =
            Random.Range(
                minDirectionTime,
                maxDirectionTime
            );
    }


    // =========================================================
    // DISTANZA DAI CONI
    // =========================================================

    private bool IsTooCloseToCone(
        Vector3 position)
    {
        foreach (Collider cone in coneColliders)
        {
            if (cone == null ||
                !cone.enabled)
            {
                continue;
            }


            Vector3 closestPoint =
                cone.ClosestPoint(
                    position
                );


            float distance =
                Vector3.Distance(
                    position,
                    closestPoint
                );


            if (distance <
                minimumConeDistance)
            {
                return true;
            }
        }


        return false;
    }


    // =========================================================
    // LIMITI STANZA
    // =========================================================

    private bool IsOutsideSafeRoomArea(
        Vector3 position)
    {
        float halfLength =
            roomLength * 0.5f;


        float halfWidth =
            roomWidth * 0.5f;


        float radius =
            capsuleWidth * 0.5f;


        float safeMargin =
            0.20f;


        float minX =
            -halfLength +
            radius +
            safeMargin;


        float maxX =
            halfLength -
            radius -
            safeMargin;


        float minZ =
            -halfWidth +
            radius +
            safeMargin;


        float maxZ =
            halfWidth -
            radius -
            safeMargin;


        if (position.x < minX)
            return true;


        if (position.x > maxX)
            return true;


        if (position.z < minZ)
            return true;


        if (position.z > maxZ)
            return true;


        return false;
    }


    // =========================================================
    // CAMERA
    // =========================================================

    private void SetupCamera()
    {
        if (mainCamera == null)
            return;


        // -----------------------------------------------------
        // La camera segue la capsula.
        // Non viene aggiunto nessun RoomCameraController.
        // -----------------------------------------------------

        ThirdPersonCamera cameraController =
            mainCamera.GetComponent<
                ThirdPersonCamera
            >();


        if (cameraController == null)
        {
            cameraController =
                mainCamera.gameObject.AddComponent<
                    ThirdPersonCamera
                >();
        }


        cameraController.target =
            capsule.transform;


        cameraController.distance =
            cameraDistance;


        cameraController.height =
            cameraHeight;


        cameraController.followSpeed =
            cameraFollowSpeed;


        cameraController.rotationSpeed =
            cameraRotationSpeed;
    }
}


// =====================================================================
// THIRD PERSON CAMERA
// =====================================================================

public class ThirdPersonCamera : MonoBehaviour
{
    [HideInInspector]
    public Transform target;


    [HideInInspector]
    public float distance =
        3.0f;


    [HideInInspector]
    public float height =
        1.20f;


    [HideInInspector]
    public float followSpeed =
        5.0f;


    [HideInInspector]
    public float rotationSpeed =
        5.0f;


    // =========================================================
    // LATE UPDATE
    // =========================================================

    private void LateUpdate()
    {
        if (target == null)
            return;


        // -----------------------------------------------------
        // Direzione dietro il personaggio
        // -----------------------------------------------------

        Vector3 backward =
            -target.forward;


        // -----------------------------------------------------
        // Posizione desiderata
        // -----------------------------------------------------

        Vector3 desiredPosition =
            target.position +
            backward *
            distance +
            Vector3.up *
            height;


        // -----------------------------------------------------
        // Movimento fluido
        // -----------------------------------------------------

        transform.position =
            Vector3.Lerp(
                transform.position,
                desiredPosition,
                followSpeed *
                Time.deltaTime
            );


        // -----------------------------------------------------
        // Punto da guardare
        // -----------------------------------------------------

        Vector3 lookTarget =
            target.position +
            Vector3.up *
            0.05f;


        Vector3 direction =
            lookTarget -
            transform.position;


        if (direction.sqrMagnitude >
            0.001f)
        {
            Quaternion desiredRotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up
                );


            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    desiredRotation,
                    rotationSpeed *
                    Time.deltaTime
                );
        }
    }
}
