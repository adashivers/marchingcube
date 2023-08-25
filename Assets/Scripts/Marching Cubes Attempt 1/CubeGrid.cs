using System;
using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


// NEXT STEP: use chunking
public class CubeGrid : MonoBehaviour
{
    public float cellSize = 0.5f;
    public int gridSize = 5;
    public GameObject meshOutput;
    public bool debug = false;
    public bool useGPU = true;

    public int seed = 3432534;
    public float noiseSize = 10;

    private ComputeShader gridComputeShader;
    ComputeBuffer polyStateBuffer = null;
    ComputeBuffer polyToVertexBuffer = null;
    private Cube[, ,] cubes;
    private int[] polys;


    void Awake()
    {
        if (useGPU)
        {
            gridComputeShader = (ComputeShader)Resources.Load("GridComputeShader");
            polyStateBuffer = new ComputeBuffer(gridSize * gridSize * gridSize * 6 * 4, sizeof(int));
            polyToVertexBuffer = new ComputeBuffer(24, sizeof(int));
        }

    }

    private void OnDestroy()
    {
        if (polyStateBuffer != null)
        {
            polyStateBuffer.Release();
        }
        if (polyToVertexBuffer != null)
        {
            polyToVertexBuffer.Release();
        }

    }

    void Start()
    {
        polys = new int[gridSize * gridSize * gridSize * 6 * 4];
        NoiseS3D.seed = seed;
        Load();
        Mesh mesh = createMesh();
        meshOutput.GetComponent<MeshFilter>().mesh = mesh;
        meshOutput.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
        meshOutput.GetComponent<MeshCollider>().sharedMesh = mesh;
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
                        Gizmos.color = Color.white * new Color(1, 1, 1, 0.4f);
                        Gizmos.DrawWireCube(transform.position + transform.rotation * (cubes[i, j, k].pos * cellSize), Vector3.one * cellSize);
                    }
                }
            }
            for (int i = 0; i < polys.Length; i = i + 4)
            {
                byte polyState = (byte)(
                    Convert.ToByte(polys[i])
                    + Convert.ToByte(polys[i + 1]) * 2
                    + Convert.ToByte(polys[i + 2]) * 4
                    + Convert.ToByte(polys[i + 3]) * 8
                );
                int poly1 = polys[i];
                int poly2 = polys[i + 1];
                int poly3 = polys[i + 2];
                int poly4 = polys[i + 3];
                Vector3Int cubeIndex = new Vector3Int(i / 24 / gridSize / gridSize, (i / 24 / gridSize) % gridSize, (i / 24) % gridSize);

                Cube cube = cubes[cubeIndex.x, cubeIndex.y, cubeIndex.z];
                int vertStartI = (i % 24);
                Vector3 polyOffset = cube.polyVertexInterp(vertStartI, vertStartI + 1) + cube.polyVertexInterp(vertStartI + 2, vertStartI + 3);
                polyOffset *= 0.5f;
                //if (polyState != 0 && polyState != 15)
                    Handles.Label(polyOffset, Convert.ToString(poly1) + Convert.ToString(poly2) + Convert.ToString(poly3) + Convert.ToString(poly4));
                
                }
            
        }
        
        
    }


    public bool isInSurface(Vector3 pos)
    {
        Vector3 v = pos * (1 / noiseSize);
        return NoiseS3D.Noise(v.x, v.y, v.z) > 0.5f;
        //return pos.y < Mathf.Sin(pos.x) * 5;
        //return Mathf.Sin(pos.y) > 0.5;
    }

    public void LoadGPU()
    {
        // WIP
        gridComputeShader.SetBuffer(0, "_PolyState", polyStateBuffer);
        polyToVertexBuffer.SetData(Cube.POLY_TO_VERTEX);
        gridComputeShader.SetBuffer(0, "POLY_TO_VERTEX", polyToVertexBuffer);
        gridComputeShader.SetInt("_GridSize", gridSize);
        gridComputeShader.SetMatrix("_Rot", Matrix4x4.Rotate(transform.rotation));
        gridComputeShader.SetVector("_GridOrigin", transform.position);
        gridComputeShader.SetFloat("_CellSize", cellSize);

        // start compute
        gridComputeShader.Dispatch(0, gridSize, gridSize, gridSize);

        // get poly data back
        polyStateBuffer.GetData(polys);
    }

    public Mesh createMeshGPU()
    {
        // WIP
        return null;
    }

    public void Load()
    {
        cubes = new Cube[gridSize, gridSize, gridSize];

        
        if (useGPU)
        {
            // set compute shader data

            gridComputeShader.SetBuffer(0, "_PolyState", polyStateBuffer);
            polyToVertexBuffer.SetData(Cube.POLY_TO_VERTEX);
            gridComputeShader.SetBuffer(0, "POLY_TO_VERTEX", polyToVertexBuffer);
            gridComputeShader.SetInt("_GridSize", gridSize);
            gridComputeShader.SetMatrix("_Rot", Matrix4x4.Rotate(transform.rotation));
            gridComputeShader.SetVector("_GridOrigin", transform.position);
            gridComputeShader.SetFloat("_CellSize", cellSize);

            // start compute
            gridComputeShader.Dispatch(0, gridSize, gridSize, gridSize);

            // get poly data back
            polyStateBuffer.GetData(polys);
        }

        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                for (int k = 0; k < gridSize; k++)
                {
                    int halfGridSize = gridSize / 2;
                    Vector3 pos = new Vector3(i - halfGridSize, j - halfGridSize, k - halfGridSize);
                    cubes[i, j, k] = new Cube(pos);
                    cubes[i, j, k].setVertexPos(transform.position, transform.rotation, cellSize);
                    int vertIndex = (i * gridSize * gridSize + j * gridSize + k) * 6 * 4;
                    if (!useGPU)
                    {
                        cubes[i, j, k].setPolyValues(isInSurface);
                        for (int vertI = 0; vertI < 24; vertI++)
                        {
                            // get only the desired polygon's state
                            int vertState = (cubes[i, j, k].polyhedra >> vertI) & 1;
                            polys[vertIndex + vertI] = vertState;
                        }
                    }
                }
            }
        }
        
    }

    void addPolyToMesh(List<Vector3> vertices, List<int> triangles, Cube cube, byte polyState, int vertStartI)
    {
        // get vertex index offsets from lookup table
        int pToVIndex = (polyState < 8) ? polyState : 15 - polyState;
        int[] pToVArray = MarchingCubeLookupTables.polyStateToVert[pToVIndex];
        for (int i = 0; i < pToVArray.Length; i += 2)
        {
            vertices.Add(cube.polyVertexInterp(vertStartI + pToVArray[i], vertStartI + pToVArray[i + 1]));
        }

        // get triangle index offsets from lookup table
        int[] pToTArray = MarchingCubeLookupTables.polyStateToTri[polyState];
        for (int i = 0; i < pToTArray.Length; i++)
        {
            triangles.Add(vertices.Count - pToTArray[i]);
        }
    }

    // tested
    public Mesh createMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        for (int i = 0; i < polys.Length; i = i + 4)
        {
            byte polyState = (byte)(
                Convert.ToByte(polys[i])
                + Convert.ToByte(polys[i + 1]) * 2
                + Convert.ToByte(polys[i + 2]) * 4
                + Convert.ToByte(polys[i + 3]) * 8
            );
            Vector3Int cubeIndex = new Vector3Int(i / 24 / gridSize / gridSize, (i / 24 / gridSize) % gridSize, (i / 24) % gridSize);

            Cube cube = cubes[cubeIndex.x, cubeIndex.y, cubeIndex.z];
            int vertStartI = (i % 24);

            addPolyToMesh(vertices, triangles, cube, polyState, vertStartI);
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // more vertices
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        Debug.Log("Done in " + Time.realtimeSinceStartup);
        return mesh;
    }

    
}
