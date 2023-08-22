using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class CubeGrid : MonoBehaviour
{
    public float cellSize = 0.5f;
    public int gridSize = 5;
    public GameObject meshOutput;
    public bool debug = false;

    public int seed = 3432534;
    public float noiseSize = 10;

    private Cube[, ,] cubes;
    private List<Cube> filledCubes;


    public Vector3 gridToWorld(Vector3 gridPos)
    {
        return transform.position + transform.rotation * (cellSize * gridPos);
    }

    public bool isEmpty(Cube cube)
    {
        return cube.polyhedra == 0 || cube.polyhedra == 16777215; // either completely inside or outside
    }

 
    private void OnDrawGizmos()
    {
        if (debug)
        {
            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    for (int k = 0; k < gridSize; k++)
                    {
                        Cube cube = cubes[i, j, k];
                        Vector3 cubeWorldPos = gridToWorld(cube.pos);
                        Gizmos.DrawWireCube(cubeWorldPos, Vector3.one * cellSize);
                        Gizmos.DrawLine(gridToWorld(cube.pos + new Vector3(-0.5f, -0.5f, 0.5f)), gridToWorld(cube.pos + new Vector3(0.5f, 0.5f, -0.5f))); // 1 - 7
                        Gizmos.DrawLine(gridToWorld(cube.pos + new Vector3(-0.5f, -0.5f, -0.5f)), gridToWorld(cube.pos + new Vector3(0.5f, 0.5f, -0.5f))); // 0 - 7
                        Gizmos.DrawLine(gridToWorld(cube.pos + new Vector3(0.5f, -0.5f, 0.5f)), gridToWorld(cube.pos + new Vector3(0.5f, 0.5f, -0.5f))); // 2 - 7
                        Gizmos.DrawLine(gridToWorld(cube.pos + new Vector3(-0.5f, 0.5f, 0.5f)), gridToWorld(cube.pos + new Vector3(0.5f, 0.5f, -0.5f))); // 5 - 7
                        Gizmos.DrawLine(gridToWorld(cube.pos + new Vector3(-0.5f, -0.5f, 0.5f)), gridToWorld(cube.pos + new Vector3(0.5f, -0.5f, -0.5f))); // 1 - 3
                        Gizmos.DrawLine(gridToWorld(cube.pos + new Vector3(-0.5f, -0.5f, 0.5f)), gridToWorld(cube.pos + new Vector3(-0.5f, 0.5f, -0.5f))); // 1 - 4
                        Gizmos.DrawLine(gridToWorld(cube.pos + new Vector3(-0.5f, -0.5f, 0.5f)), gridToWorld(cube.pos + new Vector3(0.5f, 0.5f, 0.5f))); // 1 - 6
                        Handles.Label(Vector3.Lerp(cube.polyVertexInterp(0, 0), cubeWorldPos, 0.3f), "vert 0");
                        Handles.Label(Vector3.Lerp(cube.polyVertexInterp(1, 1), cubeWorldPos, 0.3f), "vert 1");
                        Handles.Label(Vector3.Lerp(cube.polyVertexInterp(2, 2), cubeWorldPos, 0.3f), "vert 2");
                        Handles.Label(Vector3.Lerp(cube.polyVertexInterp(3, 3), cubeWorldPos, 0.3f), "vert 3");
                        Handles.Label(cubeWorldPos, "1" + (1 - i) + (1 - j) + (1 - k));
                    }
                }
            }
        }
        
    }


    public bool isInSurface(Vector3 pos)
    {
        Vector3 v = pos * (1 / noiseSize);
        return NoiseS3D.Noise(v.x, v.y, v.z) > 0.5f;
        //return pos.y < 0;
    }

    public void Load()
    {
        cubes = new Cube[gridSize, gridSize, gridSize];

        filledCubes = new List<Cube>();
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                for (int k = 0; k < gridSize; k++)
                {
                    int halfGridSize = gridSize / 2;
                    Vector3 pos = new Vector3(i - halfGridSize, j - halfGridSize, k - halfGridSize);
                    cubes[i, j, k] = new Cube(pos, transform.position, transform.rotation, cellSize);
                    cubes[i, j, k].setPolyValues(isInSurface);
                    if (!isEmpty(cubes[i, j, k]))
                        filledCubes.Add(cubes[i, j, k]);

                }
            }
        }
    }


    // tested
    public Mesh createMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        foreach (Cube cube in filledCubes)
        {
            for (int polyI = 0; polyI < 6; polyI++)
            {
                // get only the desired polygon's state
                byte polyState = (byte)((cube.polyhedra >> polyI * 4) & 15);

                int vertStartI = polyI * 4;
                // we have 8 possible states
                switch (polyState)
                {
                    // trivial cases (0000 or 1111)
                    case 0:
                        break;
                    case 15:
                        break;

                    // 0001 or 1110
                    case 1:
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 1));
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 2));
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 3));
                        triangles.Add(vertices.Count - 1);
                        triangles.Add(vertices.Count - 2);
                        triangles.Add(vertices.Count - 3);
                        break;
                    case 14:
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 1));
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 2));
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 3));
                        triangles.Add(vertices.Count - 3);
                        triangles.Add(vertices.Count - 2);
                        triangles.Add(vertices.Count - 1);
                        break;

                    // 0010 or 1101
                    case 2:
                        vertices.Add(cube.polyVertexInterp(vertStartI + 1, vertStartI));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 1, vertStartI + 2));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 1, vertStartI + 3));

                        triangles.Add(vertices.Count - 3);
                        triangles.Add(vertices.Count - 2);
                        triangles.Add(vertices.Count - 1);
                        break;
                    case 13:
                        vertices.Add(cube.polyVertexInterp(vertStartI + 1, vertStartI));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 1, vertStartI + 2));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 1, vertStartI + 3));

                        triangles.Add(vertices.Count - 1);
                        triangles.Add(vertices.Count - 2);
                        triangles.Add(vertices.Count - 3);
                        break;

                    // 0011 or 1100
                    case 3:
                        vertices.Add(cube.polyVertexInterp(vertStartI + 1, vertStartI + 2));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 1, vertStartI + 3));
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 3));
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 2));

                        triangles.Add(vertices.Count - 4);
                        triangles.Add(vertices.Count - 3);
                        triangles.Add(vertices.Count - 2);

                        triangles.Add(vertices.Count - 1);
                        triangles.Add(vertices.Count - 4);
                        triangles.Add(vertices.Count - 2);
                        break;
                    case 12:
                        vertices.Add(cube.polyVertexInterp(vertStartI + 1, vertStartI + 2));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 1, vertStartI + 3));
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 3));
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 2));

                        triangles.Add(vertices.Count - 2);
                        triangles.Add(vertices.Count - 3);
                        triangles.Add(vertices.Count - 4);

                        triangles.Add(vertices.Count - 2);
                        triangles.Add(vertices.Count - 4);
                        triangles.Add(vertices.Count - 1);
                        break;

                    // 0100 or 1011
                    case 4:
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 2));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 2, vertStartI + 3));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 1, vertStartI + 2));

                        triangles.Add(vertices.Count - 3);
                        triangles.Add(vertices.Count - 2);
                        triangles.Add(vertices.Count - 1);
                        break;
                    case 11:
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 2));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 2, vertStartI + 3));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 1, vertStartI + 2));

                        triangles.Add(vertices.Count - 1);
                        triangles.Add(vertices.Count - 2);
                        triangles.Add(vertices.Count - 3);
                        break;

                    // 0101 or 1010
                    case 5:
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 1));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 1, vertStartI + 2));
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 3));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 2, vertStartI + 3));

                        triangles.Add(vertices.Count - 2);
                        triangles.Add(vertices.Count - 3);
                        triangles.Add(vertices.Count - 4);

                        triangles.Add(vertices.Count - 1);
                        triangles.Add(vertices.Count - 3);
                        triangles.Add(vertices.Count - 2);
                        break;
                    case 10:
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 1));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 1, vertStartI + 2));
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 3));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 2, vertStartI + 3));

                        triangles.Add(vertices.Count - 4);
                        triangles.Add(vertices.Count - 3);
                        triangles.Add(vertices.Count - 2);

                        triangles.Add(vertices.Count - 2);
                        triangles.Add(vertices.Count - 3);
                        triangles.Add(vertices.Count - 1);
                        break;

                    // 0110 or 1001
                    case 6:
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 1));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 2, vertStartI + 3));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 1, vertStartI + 3));
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 2));

                        triangles.Add(vertices.Count - 4);
                        triangles.Add(vertices.Count - 3);
                        triangles.Add(vertices.Count - 2);

                        triangles.Add(vertices.Count - 3);
                        triangles.Add(vertices.Count - 4);
                        triangles.Add(vertices.Count - 1);
                        break;
                    case 9:
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 1));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 2, vertStartI + 3));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 1, vertStartI + 3));
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 2));

                        triangles.Add(vertices.Count - 2);
                        triangles.Add(vertices.Count - 3);
                        triangles.Add(vertices.Count - 4);

                        triangles.Add(vertices.Count - 1);
                        triangles.Add(vertices.Count - 4);
                        triangles.Add(vertices.Count - 3);
                        break;

                    // 0111 or 1000
                    case 7:
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 3));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 1, vertStartI + 3));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 2, vertStartI + 3));

                        triangles.Add(vertices.Count - 1);
                        triangles.Add(vertices.Count - 2);
                        triangles.Add(vertices.Count - 3);
                        break;
                    case 8:
                        vertices.Add(cube.polyVertexInterp(vertStartI, vertStartI + 3));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 1, vertStartI + 3));
                        vertices.Add(cube.polyVertexInterp(vertStartI + 2, vertStartI + 3));

                        triangles.Add(vertices.Count - 3);
                        triangles.Add(vertices.Count - 2);
                        triangles.Add(vertices.Count - 1);
                        break;

                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // more vertices
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    void Start()
    {
        NoiseS3D.seed = seed;
        Load();
        Mesh mesh = createMesh();
        meshOutput.GetComponent<MeshFilter>().mesh = mesh;
        meshOutput.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
        meshOutput.GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
