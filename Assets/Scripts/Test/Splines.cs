using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Splines : MonoBehaviour
{
    public enum SplineType
    {
        CatmullRom,
        BSpline
    }

    public SplineType splineType;
    [SerializeField, Range(1, 300)]
    private int resolution;
    [SerializeField]
    private float meshThickness = 1;

    private Vector3[] points;
    private float[] slopePoints;

    private float currentTime;
    private float finalPointX;

    void Update()
    {
        int u = (int)(currentTime * points.Length);
        float interpolator = (currentTime * points.Length) % 1;
        Vector3 point = GetCurvePoint(interpolator, points[u], points[u + 1], points[u + 2], points[u + 3]) + Vector3.up * 2;
        Camera.main.transform.position = point;
        Camera.main.transform.forward = Vector3.Lerp(Camera.main.transform.forward, GetCurveTangent(interpolator, points[u], points[u + 1], points[u + 2], points[u + 3]), 2.5f * Time.deltaTime);
    }

    public void SetPoints(Vector3[] points, float[] slopePoints)
    {
        this.points = points;
        this.slopePoints = slopePoints;
        finalPointX = GetCurvePoint(1, points[^4], points[^3], points[^2], points[^1]).x;
    }

    public void SetCurrentTime(float currentTime)
    {
        this.currentTime = currentTime;
    }

    private Mesh GetSplineMesh()
    {
        Mesh mesh = new Mesh();
        float tStep = 1 / (float)resolution;

        //	Prepare lists
        List<Vector3> verts = new List<Vector3>();
        List<Color> colors = new List<Color>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int u = 0; u < points.Length - 3; u++)
        {
            //	Create first verts
            Vector3 curvePoint = GetCurvePoint(0, points[u], points[u + 1], points[u + 2], points[u + 3]);
            verts.Add(curvePoint + Vector3.forward * meshThickness); //	Vert 0
            verts.Add(curvePoint - Vector3.forward * meshThickness); //	Vert 1

            colors.Add(Color.HSVToRGB(Mathf.Lerp(0f, 0.83f, Mathf.InverseLerp(-0.1f, 0.5f, Mathf.Lerp(slopePoints[u], slopePoints[u + 1], 0))), 1, 0.8f));
            colors.Add(Color.HSVToRGB(Mathf.Lerp(0f, 0.83f, Mathf.InverseLerp(-0.1f, 0.5f, Mathf.Lerp(slopePoints[u], slopePoints[u + 1], 0))), 1, 0.8f));

            uvs.Add(new Vector2(curvePoint.x / finalPointX, 1.0f));
            uvs.Add(new Vector2(curvePoint.x / finalPointX, 0.0f));

            for (int i = 1; i < resolution + 1; i++)
            {
                Vector3 nextPoint = GetCurvePoint(tStep * i, points[u], points[u + 1], points[u + 2], points[u + 3]);

                // Add verts
                verts.Add(nextPoint + Vector3.forward * meshThickness);
                verts.Add(nextPoint - Vector3.forward * meshThickness);

                // Add vertex color
                colors.Add(Color.HSVToRGB(Mathf.Lerp(0f, 0.83f, Mathf.InverseLerp(-0.1f, 0.5f, Mathf.Lerp(slopePoints[u], slopePoints[u + 1], tStep * i))), 1, 0.8f));
                colors.Add(Color.HSVToRGB(Mathf.Lerp(0f, 0.83f, Mathf.InverseLerp(-0.1f, 0.5f, Mathf.Lerp(slopePoints[u], slopePoints[u + 1], tStep * i))), 1, 0.8f));

                // Add uvs
                uvs.Add(new Vector2(nextPoint.x / finalPointX, 1.0f));
                uvs.Add(new Vector2(nextPoint.x / finalPointX, 0.0f));

                // Add tris
                int vertOffset = 2 + 2 * (i - 1) + u * 2 * resolution + u * 2;
                tris.AddRange(new int[] { vertOffset + 0, vertOffset - 1, vertOffset - 2 });
                tris.AddRange(new int[] { vertOffset - 1, vertOffset + 0, vertOffset + 1 });
            }
        }

        // Build mesh
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors = colors.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    [ContextMenu("Generate Mesh")]
    public void GenerateMesh()
    {
        GetComponent<MeshFilter>().mesh = GetSplineMesh();
    }

    private Vector3 GetCurvePoint(float t, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        float[,] interpolators = new float[1, 4];
        float[,] matrix = GetSplineMatrix(splineType);
        float[,] points = new float[4, 3];
        float multiplier = 1;

        interpolators[0, 0] = 1;
        interpolators[0, 1] = t;
        interpolators[0, 2] = t * t;
        interpolators[0, 3] = t * t * t;

        switch (splineType)
        {
            case SplineType.CatmullRom:
                multiplier = 0.5f;
                break;
            case SplineType.BSpline:
                multiplier = 1f / 6f;
                break;
        }

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
        return new Vector3(result[0, 0], result[0, 1], result[0, 2]) * multiplier;
    }
    private Vector3 GetCurveTangent(float t, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        float[,] interpolators = new float[1, 4];
        float[,] matrix = GetSplineMatrix(splineType);
        float[,] points = new float[4, 3];
        float multiplier = 1;

        interpolators[0, 0] = 0;
        interpolators[0, 1] = 1;
        interpolators[0, 2] = 2 * t;
        interpolators[0, 3] = 3 * t * t;

        switch (splineType)
        {
            case SplineType.CatmullRom:
                multiplier = 0.5f;
                break;
            case SplineType.BSpline:
                multiplier = 1f / 6f;
                break;
        }

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
        return new Vector3(result[0, 0], result[0, 1], result[0, 2]) * multiplier;
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

    private float[,] GetSplineMatrix(SplineType type)
    {
        float[,] matrix = new float[4, 4];

        switch (type)
        {
            case SplineType.CatmullRom:
                matrix[0, 0] = 0;
                matrix[0, 1] = 2;
                matrix[0, 2] = 0;
                matrix[0, 3] = 0;
                matrix[1, 0] = -1;
                matrix[1, 1] = 0;
                matrix[1, 2] = 1;
                matrix[1, 3] = 0;
                matrix[2, 0] = 2;
                matrix[2, 1] = -5;
                matrix[2, 2] = 4;
                matrix[2, 3] = -1;
                matrix[3, 0] = -1;
                matrix[3, 1] = 3;
                matrix[3, 2] = -3;
                matrix[3, 3] = 1;
                break;
            case SplineType.BSpline:
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
                break;
        }

        return matrix;
    }
}
