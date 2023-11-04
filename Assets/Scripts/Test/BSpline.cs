using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BSpline
{
    private List<Vector3> points;
    private float[] slopePoints;

    public void SetPoints(Vector3[] points, float[] slopePoints)
    {
        this.points = points.ToList<Vector3>();

        // BSpline correction including first and last point
        Vector3 firstPoint = points[0];
        Vector3 lastPoint = points[^1];

        this.points.Insert(0, firstPoint);
        this.points.Insert(0, firstPoint);
        this.points.Add(lastPoint);
        this.points.Add(lastPoint);

        this.slopePoints = slopePoints;
    }

    public Vector3 GetSplinePoint(float t)
    {
        GetSplineIndexes(in t, out int u, out float inter);
        return GetPointOnSubSpline(inter, points[u], points[u + 1], points[u + 2], points[u + 3]);
    }

    public Vector3 GetSplineTangent(float t)
    {
        GetSplineIndexes(in t, out int u, out float inter);
        return GetTangentOnSubSpline(inter, points[u], points[u + 1], points[u + 2], points[u + 3]);
    }

    public Mesh GetSplineMesh(int resolution, float thickness, Vector3 bitangent)
    {
        var mesh = new Mesh();
        float tStep = 1f / resolution;

        // Prepare lists
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var uvs = new List<Vector2>();
        //var colors = new List<Color>();

        // Create first verts
        Vector3 curvePoint = GetSplinePoint(0);
        verts.Add(curvePoint + bitangent * thickness); //	Vert 0
        verts.Add(curvePoint - bitangent * thickness); //	Vert 1

        //colors.Add(Color.HSVToRGB(Mathf.Lerp(0f, 0.83f, Mathf.InverseLerp(-0.1f, 0.5f, Mathf.Lerp(slopePoints[u], slopePoints[u + 1], 0))), 1, 0.8f));
        //colors.Add(Color.HSVToRGB(Mathf.Lerp(0f, 0.83f, Mathf.InverseLerp(-0.1f, 0.5f, Mathf.Lerp(slopePoints[u], slopePoints[u + 1], 0))), 1, 0.8f));

        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(0, 0));

        for (int i = 1; i < resolution; i++)
        {
            Vector3 currentPoint = GetSplinePoint(tStep * i);

            // Add verts
            verts.Add(currentPoint + bitangent * thickness); // Vert 2*i
            verts.Add(currentPoint - bitangent * thickness); // Vert 2*i + 1

            // Add vertex color
            //colors.Add(Color.HSVToRGB(Mathf.Lerp(0f, 0.83f, Mathf.InverseLerp(-0.1f, 0.5f, Mathf.Lerp(slopePoints[u], slopePoints[u + 1], tStep * i))), 1, 0.8f));
            //colors.Add(Color.HSVToRGB(Mathf.Lerp(0f, 0.83f, Mathf.InverseLerp(-0.1f, 0.5f, Mathf.Lerp(slopePoints[u], slopePoints[u + 1], tStep * i))), 1, 0.8f));

            // Add uvs
            uvs.Add(new Vector2(tStep * i, 1));
            uvs.Add(new Vector2(tStep * i, 0));

            // Add tris
            int vertOffset = 2 + 2 * (i - 1);
            tris.AddRange(new int[] { vertOffset + 0, vertOffset - 1, vertOffset - 2 });
            tris.AddRange(new int[] { vertOffset - 1, vertOffset + 0, vertOffset + 1 });
        }

        // Build mesh
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.uv = uvs.ToArray();
        //mesh.colors = colors.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    private void GetSplineIndexes(in float t, out int u, out float inter)
    {
        float lerp = Mathf.Lerp(0, points.Count - 3, t);
        u = (int)lerp;
        inter = lerp % 1;
    }

    private Vector3 GetPointOnSubSpline(float t, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        float[,] interpolators = new float[1, 4];
        float[,] matrix = GetBSplineMatrix();
        float[,] points = new float[4, 3];
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

        float[,] result = MatrixMultiply(interpolators, MatrixMultiply(matrix, points));
        return new Vector3(result[0, 0], result[0, 1], result[0, 2]) * bSplineMultiplier;
    }
    private Vector3 GetTangentOnSubSpline(float t, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        float[,] interpolators = new float[1, 4];
        float[,] matrix = GetBSplineMatrix();
        float[,] points = new float[4, 3];
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

        float[,] result = MatrixMultiply(interpolators, MatrixMultiply(matrix, points));
        return new Vector3(result[0, 0], result[0, 1], result[0, 2]) * bSplineMultiplier;
    }

    private float[,] GetBSplineMatrix()
    {
        float[,] matrix = new float[4, 4];

        matrix[0, 0] = 1;
        matrix[0, 1] = 4;
        matrix[0, 2] = 1;
        matrix[0, 3] = 0;
        matrix[1, 0] = -3;
        matrix[1, 1] = 0;
        matrix[1, 2] = 3;
        matrix[1, 3] = 0;
        matrix[2, 0] = 3;
        matrix[2, 1] = -6;
        matrix[2, 2] = 3;
        matrix[2, 3] = 0;
        matrix[3, 0] = -1;
        matrix[3, 1] = 3;
        matrix[3, 2] = -3;
        matrix[3, 3] = 1;

        return matrix;
    }

    private float[,] MatrixMultiply(float[,] matA, float[,] matB)
    {
        float[,] res;
        int rowsA = matA.GetLength(0);
        int colsA = matA.GetLength(1);
        int rowsB = matB.GetLength(0);
        int colsB = matB.GetLength(1);

        if (colsA != rowsB)
            res = null;
        else
        {
            res = new float[rowsA, colsB];
            for (int i = 0; i < colsB; i++)
                for (int j = 0; j < rowsA; j++)
                {
                    float sum = 0;
                    for (int k = 0; k < colsA; k++)
                        sum += matA[j, k] * matB[k, i];
                    res[j, i] = sum;
                }
        }

        return res;
    }
}
