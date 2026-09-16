using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// GENERATORE AUTOMATICO DI UNA STANZA
///
/// Compatibile con:
/// Unity 6 - 6000.4.2f1
/// Built-in Render Pipeline
///
/// La scena generata comprende:
/// - stanza 22 x 14 x 9 metri
/// - pavimento
/// - soffitto
/// - 4 pareti
/// - piramidi fonoassorbenti sulle pareti interne
/// - materiali
/// - collider
/// - illuminazione
///
/// NON gestisce la Main Camera.
/// La Main Camera viene gestita separatamente
/// da ToyDroneController.cs.
/// </summary>
public class RoomGenerator : MonoBehaviour
{

    // =========================================================
    // ACCESSO PUBBLICO PER GLI ALTRI SCRIPT
    // =========================================================

    public float RoomLength => roomLength;
    public float RoomWidth => roomWidth;
    public float RoomHeight => roomHeight;

    public Transform RoomRoot => roomRoot;


    // =========================================================
    // DIMENSIONI STANZA
    // =========================================================

    [Header("Dimensioni stanza")]

    [SerializeField]
    private float roomLength = 22.0f;

    [SerializeField]
    private float roomWidth = 14.0f;

    [SerializeField]
    private float roomHeight = 9.0f;


    // =========================================================
    // SPESSORE STRUTTURE
    // =========================================================

    [Header("Struttura")]

    [SerializeField]
    private float wallThickness = 0.25f;


    // =========================================================
    // PIRAMIDI ASSORBENTI
    // =========================================================

    [Header("Piramidi assorbenti")]

    [Tooltip("Lato della base della piramide in metri.")]
    [SerializeField]
    private float pyramidBase = 0.60f;

    [Tooltip("Altezza della piramide in metri.")]
    [SerializeField]
    private float pyramidHeight = 2.40f;


    // =========================================================
    // ILLUMINAZIONE
    // =========================================================

    [Header("Illuminazione")]

    [SerializeField]
    private bool createLights = true;

    [SerializeField]
    private float ambientIntensity = 0.25f;


    // =========================================================
    // MATERIALI
    // =========================================================

    private Material wallMaterial;
    private Material floorMaterial;
    private Material ceilingMaterial;
    private Material absorberMaterial;


    // =========================================================
    // ROOT DELLA STANZA
    // =========================================================

    private Transform roomRoot;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        GenerateRoom();
    }


    // =========================================================
    // GENERAZIONE COMPLETA
    // =========================================================

    private void GenerateRoom()
    {
        CreateMaterials();

        roomRoot =
            new GameObject(
                "GENERATED_ROOM"
            ).transform;

        roomRoot.SetParent(transform);

        CreateFloor();

        CreateCeiling();

        CreateWalls();

        CreateAbsorbers();

        if (createLights)
        {
            CreateLighting();
        }
    }


    // =========================================================
    // MATERIALI
    // BUILT-IN RENDER PIPELINE
    // =========================================================

    private void CreateMaterials()
    {
        Shader standardShader =
            Shader.Find("Standard");

        if (standardShader == null)
        {
            Debug.LogError(
                "RoomGenerator: impossibile trovare " +
                "lo shader 'Standard'. " +
                "Il progetto deve utilizzare il " +
                "Built-in Render Pipeline."
            );

            return;
        }


        // =====================================================
        // MURI
        // =====================================================

        wallMaterial =
            new Material(
                standardShader
            );

        wallMaterial.name =
            "MAT_Walls";

        wallMaterial.color =
            new Color(
                0.20f,
                0.20f,
                0.20f,
                1.0f
            );

        wallMaterial.SetFloat(
            "_Metallic",
            0.0f
        );

        wallMaterial.SetFloat(
            "_Glossiness",
            0.20f
        );


        // =====================================================
        // PAVIMENTO
        // =====================================================

        floorMaterial =
            new Material(
                standardShader
            );

        floorMaterial.name =
            "MAT_Floor";

        floorMaterial.color =
            new Color(
                0.16f,
                0.17f,
                0.19f,
                1.0f
            );

        floorMaterial.SetFloat(
            "_Metallic",
            0.0f
        );

        floorMaterial.SetFloat(
            "_Glossiness",
            0.20f
        );


        // =====================================================
        // SOFFITTO
        // =====================================================

        ceilingMaterial =
            new Material(
                standardShader
            );

        ceilingMaterial.name =
            "MAT_Ceiling";

        ceilingMaterial.color =
            new Color(
                0.10f,
                0.10f,
                0.10f,
                1.0f
            );

        ceilingMaterial.SetFloat(
            "_Metallic",
            0.0f
        );

        ceilingMaterial.SetFloat(
            "_Glossiness",
            0.35f
        );


        // =====================================================
        // MATERIALE PIRAMIDI
        // =====================================================

        absorberMaterial =
            new Material(
                standardShader
            );

        absorberMaterial.name =
            "MAT_RadioAbsorbent";

        absorberMaterial.color =
            new Color(
                0.055f,
                0.070f,
                0.085f,
                1.0f
            );

        absorberMaterial.SetFloat(
            "_Metallic",
            0.0f
        );

        absorberMaterial.SetFloat(
            "_Glossiness",
            0.08f
        );
    }


    // =========================================================
    // PAVIMENTO
    // =========================================================

    private void CreateFloor()
    {
        GameObject floor =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        floor.name =
            "Floor";

        floor.transform.SetParent(
            roomRoot
        );

        floor.transform.position =
            new Vector3(
                0.0f,
                -wallThickness * 0.5f,
                0.0f
            );

        floor.transform.localScale =
            new Vector3(
                roomLength,
                wallThickness,
                roomWidth
            );

        Renderer renderer =
            floor.GetComponent<Renderer>();

        renderer.material =
            floorMaterial;

        BoxCollider collider =
            floor.GetComponent<BoxCollider>();

        collider.enabled = true;
    }


    // =========================================================
    // SOFFITTO
    // =========================================================

    private void CreateCeiling()
    {
        GameObject ceiling =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        ceiling.name =
            "Ceiling";

        ceiling.transform.SetParent(
            roomRoot
        );

        ceiling.transform.position =
            new Vector3(
                0.0f,
                roomHeight +
                wallThickness * 0.5f,
                0.0f
            );

        ceiling.transform.localScale =
            new Vector3(
                roomLength,
                wallThickness,
                roomWidth
            );

        Renderer renderer =
            ceiling.GetComponent<Renderer>();

        renderer.material =
            ceilingMaterial;

        BoxCollider collider =
            ceiling.GetComponent<BoxCollider>();

        collider.enabled = true;
    }


    // =========================================================
    // PARETI
    // =========================================================

    private void CreateWalls()
    {
        // -----------------------------------------------------
        // NORD
        // -----------------------------------------------------

        CreateWall(
            "Wall_North",
            new Vector3(
                0.0f,
                roomHeight * 0.5f,
                roomWidth * 0.5f +
                wallThickness * 0.5f
            ),
            new Vector3(
                roomLength,
                roomHeight,
                wallThickness
            )
        );


        // -----------------------------------------------------
        // SUD
        // -----------------------------------------------------

        CreateWall(
            "Wall_South",
            new Vector3(
                0.0f,
                roomHeight * 0.5f,
                -roomWidth * 0.5f -
                wallThickness * 0.5f
            ),
            new Vector3(
                roomLength,
                roomHeight,
                wallThickness
            )
        );


        // -----------------------------------------------------
        // EST
        // -----------------------------------------------------

        CreateWall(
            "Wall_East",
            new Vector3(
                roomLength * 0.5f +
                wallThickness * 0.5f,
                roomHeight * 0.5f,
                0.0f
            ),
            new Vector3(
                wallThickness,
                roomHeight,
                roomWidth
            )
        );


        // -----------------------------------------------------
        // OVEST
        // -----------------------------------------------------

        CreateWall(
            "Wall_West",
            new Vector3(
                -roomLength * 0.5f -
                wallThickness * 0.5f,
                roomHeight * 0.5f,
                0.0f
            ),
            new Vector3(
                wallThickness,
                roomHeight,
                roomWidth
            )
        );
    }


    // =========================================================
    // SINGOLA PARETE
    // =========================================================

    private void CreateWall(
        string wallName,
        Vector3 position,
        Vector3 scale)
    {
        GameObject wall =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        wall.name =
            wallName;

        wall.transform.SetParent(
            roomRoot
        );

        wall.transform.position =
            position;

        wall.transform.localScale =
            scale;

        Renderer renderer =
            wall.GetComponent<Renderer>();

        renderer.material =
            wallMaterial;

        BoxCollider collider =
            wall.GetComponent<BoxCollider>();

        collider.enabled = true;
    }


    // =========================================================
    // GENERAZIONE ASSORBENTI
    // =========================================================

    private void CreateAbsorbers()
    {
        Transform absorberRoot =
            new GameObject(
                "Acoustic_Absorbers"
            ).transform;

        absorberRoot.SetParent(
            roomRoot
        );


        // Parete Nord
        CreateAbsorbersOnLongWall(
            "North",
            roomWidth * 0.5f,
            Vector3.back,
            absorberRoot
        );


        // Parete Sud
        CreateAbsorbersOnLongWall(
            "South",
            -roomWidth * 0.5f,
            Vector3.forward,
            absorberRoot
        );


        // Parete Est
        CreateAbsorbersOnShortWall(
            "East",
            roomLength * 0.5f,
            Vector3.left,
            absorberRoot
        );


        // Parete Ovest
        CreateAbsorbersOnShortWall(
            "West",
            -roomLength * 0.5f,
            Vector3.right,
            absorberRoot
        );
    }


    // =========================================================
    // ASSORBENTI PARETI NORD / SUD
    // =========================================================

    private void CreateAbsorbersOnLongWall(
        string wallName,
        float wallZ,
        Vector3 inwardDirection,
        Transform parent)
    {
        int countX =
            Mathf.FloorToInt(
                roomLength /
                pyramidBase
            );

        int countY =
            Mathf.FloorToInt(
                roomHeight /
                pyramidBase
            );


        float usedLength =
            countX *
            pyramidBase;


        float startX =
            -usedLength * 0.5f +
            pyramidBase * 0.5f;


        float startY =
            pyramidBase * 0.5f;


        for (int y = 0; y < countY; y++)
        {
            for (int x = 0; x < countX; x++)
            {
                float px =
                    startX +
                    x * pyramidBase;

                float py =
                    startY +
                    y * pyramidBase;


                CreatePyramid(
                    wallName +
                    "_Pyramid_" +
                    x +
                    "_" +
                    y,

                    new Vector3(
                        px,
                        py,
                        wallZ
                    ),

                    inwardDirection,

                    parent
                );
            }
        }
    }


    // =========================================================
    // ASSORBENTI PARETI EST / OVEST
    // =========================================================

    private void CreateAbsorbersOnShortWall(
        string wallName,
        float wallX,
        Vector3 inwardDirection,
        Transform parent)
    {
        int countZ =
            Mathf.FloorToInt(
                roomWidth /
                pyramidBase
            );

        int countY =
            Mathf.FloorToInt(
                roomHeight /
                pyramidBase
            );


        float usedWidth =
            countZ *
            pyramidBase;


        float startZ =
            -usedWidth * 0.5f +
            pyramidBase * 0.5f;


        float startY =
            pyramidBase * 0.5f;


        for (int y = 0; y < countY; y++)
        {
            for (int z = 0; z < countZ; z++)
            {
                float pz =
                    startZ +
                    z * pyramidBase;

                float py =
                    startY +
                    y * pyramidBase;


                CreatePyramid(
                    wallName +
                    "_Pyramid_" +
                    z +
                    "_" +
                    y,

                    new Vector3(
                        wallX,
                        py,
                        pz
                    ),

                    inwardDirection,

                    parent
                );
            }
        }
    }


    // =========================================================
    // CREA PIRAMIDE
    //
    // La base è sulla parete.
    // La punta è rivolta verso l'interno.
    // =========================================================

    private void CreatePyramid(
        string pyramidName,
        Vector3 wallPosition,
        Vector3 inwardDirection,
        Transform parent)
    {
        GameObject pyramid =
            new GameObject(
                pyramidName
            );

        pyramid.transform.SetParent(
            parent
        );

        pyramid.transform.position =
            wallPosition;


        // -----------------------------------------------------
        // Calcola una rotazione che porta l'asse Y locale
        // nella direzione interna della stanza.
        // -----------------------------------------------------

        pyramid.transform.rotation =
            Quaternion.FromToRotation(
                Vector3.up,
                inwardDirection
            );


        // -----------------------------------------------------
        // Mesh
        // -----------------------------------------------------

        MeshFilter meshFilter =
            pyramid.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer =
            pyramid.AddComponent<MeshRenderer>();

        MeshCollider meshCollider =
            pyramid.AddComponent<MeshCollider>();


        Mesh mesh =
            new Mesh();

        mesh.name =
            "Acoustic_Pyramid_Mesh";


        float halfBase =
            pyramidBase * 0.5f;


        // -----------------------------------------------------
        // Vertici
        //
        // Base sul piano Y = 0
        // Punta a Y = pyramidHeight
        // -----------------------------------------------------

        Vector3[] vertices =
        {
            new Vector3(
                -halfBase,
                0.0f,
                -halfBase
            ),

            new Vector3(
                halfBase,
                0.0f,
                -halfBase
            ),

            new Vector3(
                halfBase,
                0.0f,
                halfBase
            ),

            new Vector3(
                -halfBase,
                0.0f,
                halfBase
            ),

            // Punta
            new Vector3(
                0.0f,
                pyramidHeight,
                0.0f
            )
        };


        // -----------------------------------------------------
        // Triangoli
        // -----------------------------------------------------

        int[] triangles =
        {
            // Lato 1
            0, 1, 4,

            // Lato 2
            1, 2, 4,

            // Lato 3
            2, 3, 4,

            // Lato 4
            3, 0, 4,

            // Base
            3, 2, 1,
            3, 1, 0
        };


        mesh.vertices =
            vertices;

        mesh.triangles =
            triangles;

        mesh.RecalculateNormals();

        mesh.RecalculateBounds();


        meshFilter.sharedMesh =
            mesh;


        // -----------------------------------------------------
        // Materiale
        // -----------------------------------------------------

        meshRenderer.material =
            absorberMaterial;


        // -----------------------------------------------------
        // Collider
        // -----------------------------------------------------

        meshCollider.sharedMesh =
            mesh;

        meshCollider.convex =
            true;

        meshCollider.enabled =
            true;
    }


    // =========================================================
    // ILLUMINAZIONE
    // =========================================================

    private void CreateLighting()
    {
        Transform lightRoot =
            new GameObject(
                "Room_Lights"
            ).transform;

        lightRoot.SetParent(
            roomRoot
        );


        // -----------------------------------------------------
        // LUCE AMBIENTALE
        // -----------------------------------------------------

        RenderSettings.ambientMode =
            AmbientMode.Flat;

        RenderSettings.ambientLight =
            new Color(
                0.10f,
                0.12f,
                0.16f
            );

        RenderSettings.ambientIntensity =
            ambientIntensity;


        // -----------------------------------------------------
        // LUCE CENTRALE
        // -----------------------------------------------------

        CreatePointLight(
            "Ceiling_Light_Center",

            new Vector3(
                0.0f,
                7.2f,
                0.0f
            ),

            12.0f,
            10.0f,

            new Color(
                1.0f,
                0.93f,
                0.82f
            ),

            lightRoot
        );


        // -----------------------------------------------------
        // LUCE NORD-OVEST
        // -----------------------------------------------------

        CreatePointLight(
            "Ceiling_Light_NW",

            new Vector3(
                -6.0f,
                7.0f,
                4.0f
            ),

            10.0f,
            7.0f,

            new Color(
                1.0f,
                0.95f,
                0.88f
            ),

            lightRoot
        );


        // -----------------------------------------------------
        // LUCE NORD-EST
        // -----------------------------------------------------

        CreatePointLight(
            "Ceiling_Light_NE",

            new Vector3(
                6.0f,
                7.0f,
                4.0f
            ),

            10.0f,
            7.0f,

            new Color(
                1.0f,
                0.95f,
                0.88f
            ),

            lightRoot
        );


        // -----------------------------------------------------
        // LUCE SUD-OVEST
        // -----------------------------------------------------

        CreatePointLight(
            "Ceiling_Light_SW",

            new Vector3(
                -6.0f,
                7.0f,
                -4.0f
            ),

            10.0f,
            7.0f,

            new Color(
                1.0f,
                0.95f,
                0.88f
            ),

            lightRoot
        );


        // -----------------------------------------------------
        // LUCE SUD-EST
        // -----------------------------------------------------

        CreatePointLight(
            "Ceiling_Light_SE",

            new Vector3(
                6.0f,
                7.0f,
                -4.0f
            ),

            10.0f,
            7.0f,

            new Color(
                1.0f,
                0.95f,
                0.88f
            ),

            lightRoot
        );
    }


    // =========================================================
    // CREA POINT LIGHT
    // =========================================================

    private void CreatePointLight(
        string lightName,
        Vector3 position,
        float intensity,
        float range,
        Color color,
        Transform parent)
    {
        GameObject lightObject =
            new GameObject(
                lightName
            );

        lightObject.transform.SetParent(
            parent
        );

        lightObject.transform.position =
            position;


        Light light =
            lightObject.AddComponent<Light>();

        light.type =
            LightType.Point;

        light.intensity =
            intensity;

        light.range =
            range;

        light.color =
            color;

        light.shadows =
            LightShadows.Soft;

        light.shadowStrength =
            0.65f;
    }
}
