using UnityEngine;
using UnityEngine.UI;

public class RadarSemiCircle : Graphic
{

    // =========================================================
    // ACCESSO PUBBLICO PER GLI ALTRI SCRIPT
    // =========================================================

    //public int settoreRadar => settore;


    [Header("Settore da illuminare")]
    [Tooltip("0 = tutti spenti, 1-5 = settore da illuminare, >5 = tutti spenti")]
    [Range(0, 10)]
    public int settore = 0;

    [Header("Radar")]
    [Range(1, 10)]
    public int numberOfSectors = 5;

    // Apertura totale del cono
    [Range(1f, 180f)]
    public float totalAngle = 45f;

    // Direzione del cono:
    // 90° = verso l'alto
    public float directionAngle = 90f;

    [Header("Colori")]
    // Colore dei settori spenti
    public Color greenColor = new Color(0f, 1f, 0f, 0.35f);

    // Colore del settore acceso
    public Color redColor = new Color(1f, 0f, 0f, 0.65f);

    // Colore dei contorni
    public Color outlineColor = Color.green;

    [Header("Contorni")]
    [Range(1f, 10f)]
    public float outlineThickness = 2f;

    [Range(4, 64)]
    public int segments = 24;


    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        float radius = Mathf.Min(
            rectTransform.rect.width,
            rectTransform.rect.height
        ) * 0.5f;

        float sectorWidth =
            radius / numberOfSectors;

        // Apertura angolare
        float startAngle =
            directionAngle - totalAngle * 0.5f;

        float endAngle =
            directionAngle + totalAngle * 0.5f;


        // =================================================
        // SETTORI
        // =================================================

        for (int i = 0; i < numberOfSectors; i++)
        {
            float innerRadius = i * sectorWidth;
            float outerRadius = (i + 1) * sectorWidth;

            // Indice i parte da 0, mentre il nostro
            // settore pubblico parte da 1.
            int sectorIndex = i + 1;

            Color color = greenColor;

            // Accende esclusivamente il settore richiesto
            if (settore >= 1 &&
                settore <= numberOfSectors &&
                settore == sectorIndex)
            {
                color = redColor;
            }

            DrawRingSector(
                vh,
                innerRadius,
                outerRadius,
                startAngle,
                endAngle,
                color
            );
        }


        // =================================================
        // ARCHI ESTERNI DEI SETTORI
        // =================================================

        for (int i = 0; i < numberOfSectors; i++)
        {
            float outerRadius =
                (i + 1) * sectorWidth;

            DrawArc(
                vh,
                outerRadius,
                startAngle,
                endAngle,
                outlineColor,
                outlineThickness
            );
        }


        // =================================================
        // LINEE LATERALI DEL CONO
        // =================================================

        DrawLine(
            vh,
            Vector2.zero,
            GetPoint(radius, startAngle),
            outlineColor,
            outlineThickness
        );

        DrawLine(
            vh,
            Vector2.zero,
            GetPoint(radius, endAngle),
            outlineColor,
            outlineThickness
        );
    }


    // =====================================================
    // DISEGNA UN SETTORE CONCENTRICO
    // =====================================================

    void DrawRingSector(
        VertexHelper vh,
        float innerRadius,
        float outerRadius,
        float startAngle,
        float endAngle,
        Color color)
    {
        int startIndex = vh.currentVertCount;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;

            float angle = Mathf.Lerp(
                startAngle,
                endAngle,
                t
            );

            Vector2 outer =
                GetPoint(outerRadius, angle);

            Vector2 inner =
                GetPoint(innerRadius, angle);

            UIVertex vOuter =
                UIVertex.simpleVert;

            vOuter.position = outer;
            vOuter.color = color;

            UIVertex vInner =
                UIVertex.simpleVert;

            vInner.position = inner;
            vInner.color = color;

            vh.AddVert(vOuter);
            vh.AddVert(vInner);
        }

        for (int i = 0; i < segments; i++)
        {
            int current =
                startIndex + i * 2;

            vh.AddTriangle(
                current,
                current + 1,
                current + 2
            );

            vh.AddTriangle(
                current + 1,
                current + 3,
                current + 2
            );
        }
    }


    // =====================================================
    // DISEGNA UN ARCO
    // =====================================================

    void DrawArc(
        VertexHelper vh,
        float radius,
        float startAngle,
        float endAngle,
        Color color,
        float thickness)
    {
        for (int i = 0; i < segments; i++)
        {
            float t1 =
                i / (float)segments;

            float t2 =
                (i + 1) / (float)segments;

            float a1 =
                Mathf.Lerp(
                    startAngle,
                    endAngle,
                    t1
                );

            float a2 =
                Mathf.Lerp(
                    startAngle,
                    endAngle,
                    t2
                );

            Vector2 p1 =
                GetPoint(radius, a1);

            Vector2 p2 =
                GetPoint(radius, a2);

            DrawLine(
                vh,
                p1,
                p2,
                color,
                thickness
            );
        }
    }


    // =====================================================
    // DISEGNA UNA LINEA
    // =====================================================

    void DrawLine(
        VertexHelper vh,
        Vector2 start,
        Vector2 end,
        Color color,
        float thickness)
    {
        Vector2 direction =
            (end - start).normalized;

        Vector2 normal =
            new Vector2(
                -direction.y,
                direction.x
            ) * thickness * 0.5f;

        int index =
            vh.currentVertCount;

        UIVertex v1 =
            UIVertex.simpleVert;

        v1.position =
            start + normal;

        v1.color = color;


        UIVertex v2 =
            UIVertex.simpleVert;

        v2.position =
            start - normal;

        v2.color = color;


        UIVertex v3 =
            UIVertex.simpleVert;

        v3.position =
            end - normal;

        v3.color = color;


        UIVertex v4 =
            UIVertex.simpleVert;

        v4.position =
            end + normal;

        v4.color = color;


        vh.AddVert(v1);
        vh.AddVert(v2);
        vh.AddVert(v3);
        vh.AddVert(v4);


        vh.AddTriangle(
            index,
            index + 1,
            index + 2
        );

        vh.AddTriangle(
            index,
            index + 2,
            index + 3
        );
    }


    // =====================================================
    // CALCOLA UN PUNTO SULL'ARCO
    // =====================================================

    Vector2 GetPoint(
        float radius,
        float angle)
    {
        float radians =
            angle * Mathf.Deg2Rad;

        return new Vector2(
            Mathf.Cos(radians),
            Mathf.Sin(radians)
        ) * radius;
    }


    // =====================================================
    // API PUBBLICA
    // =====================================================

    public void SetSettore(int index)
    {
        settore = index;

        SetVerticesDirty();
    }
}
