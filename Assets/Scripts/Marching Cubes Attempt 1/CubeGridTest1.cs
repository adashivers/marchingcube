using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class CubeGridTest1 : MonoBehaviour
{
    public float cellSize = 0.5f;
    public int gridSize = 5;
    public GameObject meshOutput;

    private Cube[, ,] cubes;
    private Mesh mesh;
    private List<Cube> filledCubes;


    public Vector3 gridToWorld(Vector3 gridPos)
    {
        return transform.position + transform.rotation * (cellSize * gridPos);
    }

    public bool isEmpty(Cube cube)
    {
        return cube.polyhedra == 0 || cube.polyhedra == 16777215; // either completely inside or outside
    }
    /*
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 1, 1, 0.2f);
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                for (int k = 0; k < gridSize; k++)
                {
                    Cube cube = cubes[i, j, k];
                    if (cube.polyhedra != 0 && cube.polyhedra != 16777215)
                    {
                        Vector3 cubeWorldPos = gridToWorld(cube.pos);
                        Gizmos.DrawWireCube(cubeWorldPos, Vector3.one * cellSize);
                        Gizmos.DrawLine(gridToWorld(cube.pos + new Vector3(-0.5f, -0.5f, 0.5f)), gridToWorld(cube.pos + new Vector3(0.5f, 0.5f, -0.5f))); // 1 - 7
                        Gizmos.DrawLine(gridToWorld(cube.pos + new Vector3(-0.5f, -0.5f, -0.5f)), gridToWorld(cube.pos + new Vector3(0.5f, 0.5f, -0.5f))); // 0 - 7
                        Gizmos.DrawLine(gridToWorld(cube.pos + new Vector3(0.5f, -0.5f, 0.5f)), gridToWorld(cube.pos + new Vector3(0.5f, 0.5f, -0.5f))); // 2 - 7
                        Gizmos.DrawLine(gridToWorld(cube.pos + new Vector3(-0.5f, 0.5f, 0.5f)), gridToWorld(cube.pos + new Vector3(0.5f, 0.5f, -0.5f))); // 5 - 7
                        Gizmos.DrawLine(gridToWorld(cube.pos + new Vector3(-0.5f, -0.5f, 0.5f)), gridToWorld(cube.pos + new Vector3(0.5f, -0.5f, -0.5f))); // 1 - 3
                        Gizmos.DrawLine(gridToWorld(cube.pos + new Vector3(-0.5f, -0.5f, 0.5f)), gridToWorld(cube.pos + new Vector3(-0.5f, 0.5f, -0.5f))); // 1 - 4
                        Gizmos.DrawLine(gridToWorld(cube.pos + new Vector3(-0.5f, -0.5f, 0.5f)), gridToWorld(cube.pos + new Vector3(0.5f, 0.5f, 0.5f))); // 1 - 6

                        for (int p = 0; p < 6; p++)
                        {
                            int pStart = p * 4;
                            Vector3 polyCenter =
                                cube.polyVertexInterp(pStart + 0, pStart + 0)
                                + cube.polyVertexInterp(pStart + 1, pStart + 1)
                                + cube.polyVertexInterp(pStart + 2, pStart + 2)
                                + cube.polyVertexInterp(pStart + 3, pStart + 3);
                            polyCenter *= 0.25f;
                            Handles.Label(polyCenter, "poly " + p);
                            Handles.Label(Vector3.Lerp(cube.polyVertexInterp(pStart + 0, pStart + 0), polyCenter, 0.3f), "p" + p + "v0");
                            Handles.Label(Vector3.Lerp(cube.polyVertexInterp(pStart + 1, pStart + 1), polyCenter, 0.3f), "p" + p + "v1");
                            Handles.Label(Vector3.Lerp(cube.polyVertexInterp(pStart + 2, pStart + 2), polyCenter, 0.3f), "p" + p + "v2");
                            Handles.Label(Vector3.Lerp(cube.polyVertexInterp(pStart + 3, pStart + 3), polyCenter, 0.3f), "p" + p + "v3");
                        }
                        
                    }
                }
            }
        }
    }
    */

    public bool isInSurface(Vector3 pos)
    {
        return pos.magnitude <= cellSize * gridSize * 0.5f;
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

        // --- DEBUG ---
        //cubes[0, 0, 0].polyhedra = 0; // 0000
        //cubes[0, 0, 1].polyhedra = 1 | (1 << 4) | (1 << 8) | (1 << 12) | (1 << 16) | (1 << 20); // 0001
        //cubes[0, 1, 0].polyhedra = 2 | (2 << 4) | (2 << 8) | (2 << 12) | (2 << 16) | (2 << 20); // 0010
        //cubes[0, 1, 1].polyhedra = 3 | (3 << 4) | (3 << 8) | (3 << 12) | (3 << 16) | (3 << 20); // 0011
        //cubes[1, 0, 0].polyhedra = 4 | (4 << 4) | (4 << 8) | (4 << 12) | (4 << 16) | (4 << 20); // 0100
        //cubes[1, 0, 1].polyhedra = 5 | (5 << 4) | (5 << 8) | (5 << 12) | (5 << 16) | (5 << 20); // 0101
        //cubes[1, 1, 0].polyhedra = 6 | (6 << 4) | (6 << 8) | (6 << 12) | (6 << 16) | (6 << 20); // 0110
        //cubes[1, 1, 1].polyhedra = 7 | (7 << 4) | (7 << 8) | (7 << 12) | (7 << 16) | (7 << 20); // 0111
    }

    public IEnumerator createMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        
        
        foreach (Cube cube in filledCubes)
        {
            Debug.Log("Cube at (" + cube.pos + ") local, (" + gridToWorld(cube.pos) + ") global");
            for (int polyI = 0; polyI < 6 && !isEmpty(cube); polyI++)
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

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            yield return null;
        }
        

    }

    void Start()
    {
        Load();
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // more vertices
        StartCoroutine("createMesh");
        meshOutput.GetComponent<MeshFilter>().mesh = mesh;
        meshOutput.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
