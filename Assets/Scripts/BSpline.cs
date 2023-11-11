using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BSpline
{
    private List<Vector3> splinePoints;
    private List<Color> splineColors;

    private float[,] bSplineMatrix;

    private float distance;

    // Cache for opimization purposes
    private float[,] interpolators;
    private float[,] points;
    private float[,] res;

    public BSpline()
    {
        interpolators = new float[1, 4];
        points = new float[4, 3];
        res = new float[4, 4];
        PrecomputeBSplineMatrix();
    }

    public void SetPoints(Vector3[] points)
    {
        splinePoints = points.ToList<Vector3>();

        // BSpline correction including first and last point
        Vector3 firstPoint = points[0];
        Vector3 lastPoint = points[^1];

        distance = lastPoint.x - firstPoint.x;

        splinePoints.Insert(0, firstPoint);
        splinePoints.Insert(0, firstPoint);
        splinePoints.Add(lastPoint);
        splinePoints.Add(lastPoint);
    }

    public void SetColors(Color[] colors)
    {
        splineColors = colors.ToList<Color>();

        // BSpline correction including first and last point
        Color firstPoint = colors[0];
        Color lastPoint = colors[^1];

        splineColors.Insert(0, firstPoint);
        splineColors.Insert(0, firstPoint);
        splineColors.Add(lastPoint);
        splineColors.Add(lastPoint);
    }

    public Vector3 GetSplinePoint(float t)
    {
        GetSplineIndexes(in t, out int u, out float inter);
        return GetPointOnSubSpline(inter, splinePoints[u], splinePoints[u + 1], splinePoints[u + 2], splinePoints[u + 3]);
    }

    public Vector3 GetSplineTangent(float t)
    {
        GetSplineIndexes(in t, out int u, out float inter);
        return GetTangentOnSubSpline(inter, splinePoints[u], splinePoints[u + 1], splinePoints[u + 2], splinePoints[u + 3]);
    }

    public Color GetSplineColor(float t)
    {
        GetSplineIndexes(in t, out int u, out float inter);
        return Color.Lerp(splineColors[u], splineColors[u + 1], inter);
    }

    public float GetPercentageWithSpeedApplied(float percentage)
    {
        return GetSplinePoint(percentage).x / distance;
    }

    public Mesh GetSplineMesh(int resolution, float thickness, Vector3 bitangent)
    {
        var mesh = new Mesh();
        float tStep = 1f / resolution;

        // Prepare lists
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var uvs = new List<Vector2>();
        var vertexColors = new List<Color>();

        // Create first verts
        Vector3 curvePoint = GetSplinePoint(0);

        verts.Add(curvePoint + GetBitangentPerpendicularToTangent(0, bitangent) * thickness); // Vert 0
        verts.Add(curvePoint - GetBitangentPerpendicularToTangent(0, bitangent) * thickness); // Vert 1

        vertexColors.Add(GetSplineColor(0));
        vertexColors.Add(GetSplineColor(0));

        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(0, 0));

        float lastVertex1x = (GetSplinePoint(1) + GetBitangentPerpendicularToTangent(1, bitangent) * thickness).x;
        float lastVertex2x = (GetSplinePoint(1) - GetBitangentPerpendicularToTangent(1, bitangent) * thickness).x;

        for (int i = 1; i < resolution; i++)
        {
            Vector3 currentPoint = GetSplinePoint(tStep * i);

            // Add verts
            Vector3 vertex1 = currentPoint + GetBitangentPerpendicularToTangent(tStep * i, bitangent) * thickness;
            Vector3 vertex2 = currentPoint - GetBitangentPerpendicularToTangent(tStep * i, bitangent) * thickness;
            verts.Add(vertex1); // Vert 2*i
            verts.Add(vertex2); // Vert 2*i + 1

            // Add vertex color
            vertexColors.Add(GetSplineColor(tStep * i));
            vertexColors.Add(GetSplineColor(tStep * i));

            // Add uvs
            uvs.Add(new Vector2(vertex1.x / lastVertex1x, 1));
            uvs.Add(new Vector2(vertex2.x / lastVertex2x, 0));

            // Add tris
            int vertOffset = 2 + 2 * (i - 1);
            tris.AddRange(new int[] { vertOffset + 0, vertOffset - 1, vertOffset - 2 });
            tris.AddRange(new int[] { vertOffset - 1, vertOffset + 0, vertOffset + 1 });
        }

        // Build mesh
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors = vertexColors.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    public Vector3 GetBitangentPerpendicularToTangent(float t, Vector3 bitangent)
    {
        Vector3 tangent = GetSplineTangent(t).normalized;
        Vector3 projection = Vector3.Project(bitangent.normalized, tangent);
        return (bitangent.normalized - projection).normalized;
    }

    public void GetSplineIndexes(in float t, out int u, out float inter)
    {
        float lerp = Mathf.Lerp(0, splinePoints.Count - 4, t);
        u = (int)lerp;
        inter = lerp % 1;
    }

    public float GetSplinePercentageFromTrackIndex(int index)
    {
        return Mathf.InverseLerp(0, splinePoints.Count - 4, index);
    }

    private Vector3 GetPointOnSubSpline(float t, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        float bSplineMultiplier = 1f / 6f;

        interpolators[0, 0] = 1;
        interpolators[0, 1] = t;
        interpolators[0, 2] = t * t;
        interpolators[0, 3] = t * t * t;

        points[0, 0] = p1.x;
        points[0, 1] = p1.y;
        points[0, 2] = p1.z;
        points[1, 0] = p2.x;
        points[1, 1] = p2.y;
        points[1, 2] = p2.z;
        points[2, 0] = p3.x;
        points[2, 1] = p3.y;
        points[2, 2] = p3.z;
        points[3, 0] = p4.x;
        points[3, 1] = p4.y;
        points[3, 2] = p4.z;

        float[,] result = MatrixMultiply(interpolators, MatrixMultiply(bSplineMatrix, points));
        return new Vector3(result[0, 0], result[0, 1], result[0, 2]) * bSplineMultiplier;
    }

    private Vector3 GetTangentOnSubSpline(float t, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        float bSplineMultiplier = 1f / 6f;

        interpolators[0, 0] = 0;
        interpolators[0, 1] = 1;
        interpolators[0, 2] = 2 * t;
        interpolators[0, 3] = 3 * t * t;

        points[0, 0] = p1.x;
        points[0, 1] = p1.y;
        points[0, 2] = p1.z;
        points[1, 0] = p2.x;
        points[1, 1] = p2.y;
        points[1, 2] = p2.z;
        points[2, 0] = p3.x;
        points[2, 1] = p3.y;
        points[2, 2] = p3.z;
        points[3, 0] = p4.x;
        points[3, 1] = p4.y;
        points[3, 2] = p4.z;

        float[,] result = MatrixMultiply(interpolators, MatrixMultiply(bSplineMatrix, points));
        return new Vector3(result[0, 0], result[0, 1], result[0, 2]) * bSplineMultiplier;
    }

    private void PrecomputeBSplineMatrix()
    {
        bSplineMatrix = new float[4, 4];
        bSplineMatrix[0, 0] = 1;
        bSplineMatrix[0, 1] = 4;
        bSplineMatrix[0, 2] = 1;
        bSplineMatrix[0, 3] = 0;
        bSplineMatrix[1, 0] = -3;
        bSplineMatrix[1, 1] = 0;
        bSplineMatrix[1, 2] = 3;
        bSplineMatrix[1, 3] = 0;
        bSplineMatrix[2, 0] = 3;
        bSplineMatrix[2, 1] = -6;
        bSplineMatrix[2, 2] = 3;
        bSplineMatrix[2, 3] = 0;
        bSplineMatrix[3, 0] = -1;
        bSplineMatrix[3, 1] = 3;
        bSplineMatrix[3, 2] = -3;
        bSplineMatrix[3, 3] = 1;
    }

    private float[,] MatrixMultiply(float[,] matA, float[,] matB)
    {
        int rowsA = matA.GetLength(0);
        int colsA = matA.GetLength(1);
        int colsB = matB.GetLength(1);

        for (int i = 0; i < colsB; i++)
            for (int j = 0; j < rowsA; j++)
            {
                float sum = 0;
                for (int k = 0; k < colsA; k++)
                    sum += matA[j, k] * matB[k, i];
                res[j, i] = sum;
            }

        return res;
    }
}
