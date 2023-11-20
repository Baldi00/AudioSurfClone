using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BSpline
{
    private List<Vector3> points;
    private List<Color> colors;

    // Cache for opimization purposes
    private readonly float[,] interpolators;
    private readonly float[,] bSplineMatrix;
    private readonly float[,] subSplinePointsMatrix;
    private readonly float[,] matrixMultiplyResult;
    private readonly float bSplineMultiplier = 1f / 6f;

    public BSpline()
    {
        interpolators = new float[1, 4];
        bSplineMatrix = new float[4, 4];
        subSplinePointsMatrix = new float[4, 3];
        matrixMultiplyResult = new float[4, 4];
        InitializeBSplineMatrix();
    }

    /// <summary>
    /// Sets the points of the spline
    /// </summary>
    /// <param name="points">The world position points of the spline</param>
    public void SetPoints(Vector3[] points)
    {
        this.points = points.ToList();
        BSplineCorrectionAddFirstAndLastPoint(this.points);
    }

    /// <summary>
    /// Sets the colors for each point of the spline (colors length must be the same as the number of points)
    /// </summary>
    /// <param name="colors">The colors of each point of the spline</param>
    public void SetColors(Color[] colors)
    {
        this.colors = colors.ToList();
        BSplineCorrectionAddFirstAndLastPoint(this.colors);
    }

    /// <summary>
    /// Returns a copy of the list of spline points
    /// </summary>
    /// <returns>A copy of the list of spline points</returns>
    public List<Vector3> GetPoints()
    {
        return new List<Vector3>(points);
    }

    /// <summary>
    /// Returns the point of the spline at the given percentage
    /// </summary>
    /// <param name="t">The percentage in the range [0,1]</param>
    /// <returns>The point of the spline at the given percentage</returns>
    public Vector3 GetPointAt(float t)
    {
        GetSubSplineIndexes(in t, out int firstSubSplinePointIndex, out float subSplineInterpolator);
        InitializePointInterpolators(subSplineInterpolator);
        InitializeSubSplinePointsMatrix(firstSubSplinePointIndex);
        MatrixMultiply(interpolators, MatrixMultiply(bSplineMatrix, subSplinePointsMatrix));
        return new Vector3(
            matrixMultiplyResult[0, 0],
            matrixMultiplyResult[0, 1],
            matrixMultiplyResult[0, 2]) * bSplineMultiplier;
    }

    /// <summary>
    /// Returns the tangent of the spline at the given percentage
    /// </summary>
    /// <param name="t">The percentage in the range [0,1]</param>
    /// <returns>The tangent of the spline at the given percentage</returns>
    public Vector3 GetTangentAt(float t)
    {
        GetSubSplineIndexes(in t, out int firstSubSplinePointIndex, out float subSplineInterpolator);
        InitializeTangentInterpolators(subSplineInterpolator);
        InitializeSubSplinePointsMatrix(firstSubSplinePointIndex);
        MatrixMultiply(interpolators, MatrixMultiply(bSplineMatrix, subSplinePointsMatrix));
        return new Vector3(
            matrixMultiplyResult[0, 0],
            matrixMultiplyResult[0, 1],
            matrixMultiplyResult[0, 2]) * bSplineMultiplier;
    }

    /// <summary>
    /// Returns the color of the spline at the given percentage
    /// </summary>
    /// <param name="t">The percentage in the range [0,1]</param>
    /// <returns>The color of the spline at the given percentage</returns>
    public Color GetColorAt(float t)
    {
        GetSubSplineIndexes(in t, out int firstSubSplinePointIndex, out float subSplineInterpolator);
        return
            Color.Lerp(colors[firstSubSplinePointIndex], colors[firstSubSplinePointIndex + 1], subSplineInterpolator);
    }

    /// <summary>
    /// Gets the sub-spline indexes of the spline at a given percentage.
    /// The whole spline is a sequence of third order polynomial bsplines composed by 4 points each.
    /// This methods returns the index of the first sub-spline index and the interpolator (in range [0,1])
    /// inside that sub-spline corresponding to the given percentage.
    /// E.g. .____.____.____.v___. t = 0.75 -> firstSubPointIndex = 3, subSplineInterpolator = 0.25
    /// </summary>
    /// <param name="t">The percentage of the whole spline in the range [0,1]</param>
    /// <param name="firstSubSplinePointIndex">The first sub-spline point index</param>
    /// <param name="subSplineInterpolator">The interpolator value inside the sub-spline</param>
    public void GetSubSplineIndexes(in float t, out int firstSubSplinePointIndex, out float subSplineInterpolator)
    {
        float lerp = Mathf.Lerp(0, points.Count - 4, t);
        firstSubSplinePointIndex = (int)lerp;
        subSplineInterpolator = lerp % 1;
    }

    /// <summary>
    /// Returns the bitangent to the spline perpendicular to the tangent and in the nearest direction
    /// to the desired bitangent
    /// </summary>
    /// <param name="t">The percentage on the spline in the range [0,1]</param>
    /// <param name="desiredBitangent">The desired bitangent for the spline</param>
    /// <returns>The bitangent to the spline perpendicular to the tangent</returns>
    public Vector3 GetBitangentPerpendicularToTangent(float t, Vector3 desiredBitangent)
    {
        Vector3 tangent = GetTangentAt(t).normalized;
        Vector3 projection = Vector3.Project(desiredBitangent.normalized, tangent);
        return (desiredBitangent.normalized - projection).normalized;
    }

    /// <summary>
    /// Creates a mesh based on the current spline and the given parameters
    /// </summary>
    /// <param name="resolution">The number of subdivision of the mesh (e.g. 1024 -> 1024 quads)</param>
    /// <param name="halfThickness">Half of the thickness of the mesh</param>
    /// <param name="bitangent">The bitangent direction of the mesh compared to the spline tangent</param>
    /// <returns>The created mesh</returns>
    public Mesh GetSplineMesh(int resolution, float halfThickness, Vector3 bitangent)
    {
        var mesh = new Mesh();
        float tStep = 1f / resolution;

        // Prepare lists
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var uvs = new List<Vector2>();
        var vertexColors = new List<Color>();

        // Create first verts
        Vector3 curvePoint = GetPointAt(0);

        verts.Add(curvePoint + GetBitangentPerpendicularToTangent(0, bitangent) * halfThickness); // Vert 0
        verts.Add(curvePoint - GetBitangentPerpendicularToTangent(0, bitangent) * halfThickness); // Vert 1

        vertexColors.Add(GetColorAt(0));
        vertexColors.Add(GetColorAt(0));

        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(0, 0));

        float lastVertex1x = (GetPointAt(1) + GetBitangentPerpendicularToTangent(1, bitangent) * halfThickness).x;
        float lastVertex2x = (GetPointAt(1) - GetBitangentPerpendicularToTangent(1, bitangent) * halfThickness).x;

        for (int i = 1; i < resolution; i++)
        {
            Vector3 currentPoint = GetPointAt(tStep * i);

            // Add verts
            Vector3 vertex1 = currentPoint + GetBitangentPerpendicularToTangent(tStep * i, bitangent) * halfThickness;
            Vector3 vertex2 = currentPoint - GetBitangentPerpendicularToTangent(tStep * i, bitangent) * halfThickness;
            verts.Add(vertex1); // Vert 2*i
            verts.Add(vertex2); // Vert 2*i + 1

            // Add vertex color
            vertexColors.Add(GetColorAt(tStep * i));
            vertexColors.Add(GetColorAt(tStep * i));

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

    /// <summary>
    /// Initializes the BSpline matrix
    /// </summary>
    private void InitializeBSplineMatrix()
    {
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

    /// <summary>
    /// Initializes the sub-spline points matrix starting from the first sub-spline point index
    /// </summary>
    /// <param name="firstSubSplinePointIndex">The index of the first sub-spline point</param>
    private void InitializeSubSplinePointsMatrix(int firstSubSplinePointIndex)
    {
        subSplinePointsMatrix[0, 0] = points[firstSubSplinePointIndex].x;
        subSplinePointsMatrix[0, 1] = points[firstSubSplinePointIndex].y;
        subSplinePointsMatrix[0, 2] = points[firstSubSplinePointIndex].z;
        subSplinePointsMatrix[1, 0] = points[firstSubSplinePointIndex + 1].x;
        subSplinePointsMatrix[1, 1] = points[firstSubSplinePointIndex + 1].y;
        subSplinePointsMatrix[1, 2] = points[firstSubSplinePointIndex + 1].z;
        subSplinePointsMatrix[2, 0] = points[firstSubSplinePointIndex + 2].x;
        subSplinePointsMatrix[2, 1] = points[firstSubSplinePointIndex + 2].y;
        subSplinePointsMatrix[2, 2] = points[firstSubSplinePointIndex + 2].z;
        subSplinePointsMatrix[3, 0] = points[firstSubSplinePointIndex + 3].x;
        subSplinePointsMatrix[3, 1] = points[firstSubSplinePointIndex + 3].y;
        subSplinePointsMatrix[3, 2] = points[firstSubSplinePointIndex + 3].z;
    }

    /// <summary>
    /// Initializes the interpolators for the points interpolation (1 t t^2 t^3)
    /// </summary>
    /// <param name="interpolator">The interpolation value on the sub-spline</param>
    private void InitializePointInterpolators(float interpolator)
    {
        interpolators[0, 0] = 1;
        interpolators[0, 1] = interpolator;
        interpolators[0, 2] = interpolator * interpolator;
        interpolators[0, 3] = interpolator * interpolator * interpolator;
    }

    /// <summary>
    /// Initializes the interpolators for the tangent interpolation
    /// (0 1 2t 3t^2, first derivative of points interpolation)
    /// </summary>
    /// <param name="interpolator">The interpolation value on the sub-spline</param>
    private void InitializeTangentInterpolators(float interpolator)
    {
        interpolators[0, 0] = 0;
        interpolators[0, 1] = 1;
        interpolators[0, 2] = 2 * interpolator;
        interpolators[0, 3] = 3 * interpolator * interpolator;
    }

    /// <summary>
    /// Computes the matrix multiplication of the two given matrices
    /// </summary>
    /// <param name="matA">The first matrix to multiply</param>
    /// <param name="matB">The second matrix to multiply</param>
    /// <returns>The result of the matrix multiplication</returns>
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
                matrixMultiplyResult[j, i] = sum;
            }

        return matrixMultiplyResult;
    }

    /// <summary>
    /// BSplines don't include first and last point by default.
    /// This method corrects this by duplicating twice the first and last point in order to include them in the BSpline
    /// </summary>
    /// <typeparam name="T">The type of the list to correct (e.g. Vector3, Color)</typeparam>
    /// <param name="listToCorrect">The list to fix with the fist and last point</param>
    private void BSplineCorrectionAddFirstAndLastPoint<T>(List<T> listToCorrect)
    {
        T firstPoint = listToCorrect[0];
        T lastPoint = listToCorrect[^1];

        listToCorrect.Insert(0, firstPoint);
        listToCorrect.Insert(0, firstPoint);
        listToCorrect.Add(lastPoint);
        listToCorrect.Add(lastPoint);
    }
}
