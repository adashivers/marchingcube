using System;
using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


// NEXT STEP: add jobs
public class CubeGrid : MonoBehaviour
{
    [Header("Sizes")]
    public float cellSize = 0.5f;
    public int gridSize = 5;
    [Header("Output Mesh")]
    public GameObject meshOutput;
    // public bool debug = false;
    [Header("Use GPU? (usually faster)")]
    public bool useGPU = true;

    [Header("Other Settings")]
    public int seed = 3432534;
    public float noiseSize = 10;

    private ComputeShader gridComputeShader;
    ComputeBuffer polyStateBuffer = null;
    ComputeBuffer polyToVertexBuffer = null;
    ComputeBuffer vertexPositionBuffer = null;
    private int[] polys; // each int is the state of one vertex. ordered in order cube x, y, z, and then 6 polyhedra * 4 vertex per poly
    private Vector3[] vertexPositions; // each vector is the world position of one vertex. ordered the same way as polys
    private Vector3[] axes; // local grid axes in world space

    void Awake()
    {
        // set global values
        polys = new int[gridSize * gridSize * gridSize * 6 * 4];
        vertexPositions = new Vector3[gridSize * gridSize * gridSize * 6 * 4];
        axes = new Vector3[]
        {
            transform.rotation * new Vector3(cellSize, 0, 0),
            transform.rotation * new Vector3(0, cellSize, 0),
            transform.rotation * new Vector3(0, 0, cellSize)
        };
        NoiseS3D.seed = seed;

        if (useGPU)
        {
            gridComputeShader = (ComputeShader)Resources.Load("GridComputeShader");
            polyStateBuffer = new ComputeBuffer(gridSize * gridSize * gridSize * 6 * 4, sizeof(int));
            polyToVertexBuffer = new ComputeBuffer(24, sizeof(int));
            vertexPositionBuffer = new ComputeBuffer(gridSize * gridSize * gridSize * 6 * 4, sizeof(float) * 3);
        }

    }

    private void OnDestroy()
    {
        // release compute shader buffers
        if (polyStateBuffer != null)
        {
            polyStateBuffer.Release();
        }
        if (polyToVertexBuffer != null)
        {
            polyToVertexBuffer.Release();
        }
        if (vertexPositionBuffer != null)
        {
            vertexPositionBuffer.Release();
        }

    }

    void Start()
    {
        if (useGPU)
            LoadGPU();
        else
            LoadCPU();
        Mesh mesh = createMesh();
        meshOutput.GetComponent<MeshFilter>().mesh = mesh;
        meshOutput.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
        meshOutput.GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    private bool isEmpty(byte polyState)
    {
        return polyState == 0 || polyState == 15; // either completely inside or outside
    }

 
    private void OnDrawGizmos()
    {
        /* WIP
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
        */
    }


    private bool isInSurface(Vector3 pos)
    {
        Vector3 v = pos * (1 / noiseSize);
        return NoiseS3D.Noise(v.x, v.y, v.z) > 0.5f;
        //return pos.y < Mathf.Sin(pos.x) * 5;
        //return Mathf.Sin(pos.y) > 0.5;
    }

    private void LoadGPU()
    {
        // set all values and buffers
        gridComputeShader.SetBuffer(0, "_PolyState", polyStateBuffer);
        gridComputeShader.SetBuffer(0, "_VertPos", vertexPositionBuffer);
        polyToVertexBuffer.SetData(MarchingCubeLookupTables.POLY_TO_CUBE_VERTEX);
        gridComputeShader.SetBuffer(0, "POLY_TO_VERTEX", polyToVertexBuffer);
        gridComputeShader.SetInt("_GridSize", gridSize);
        gridComputeShader.SetMatrix("_Rot", Matrix4x4.Rotate(transform.rotation));
        gridComputeShader.SetVector("_GridOrigin", transform.position);
        gridComputeShader.SetFloat("_CellSize", cellSize);

        // start compute
        gridComputeShader.Dispatch(0, gridSize, gridSize, gridSize);

        // get poly data back
        polyStateBuffer.GetData(polys);
        vertexPositionBuffer.GetData(vertexPositions);
    }

    private Vector3 getVertexGlobalPos(Vector3 localPos, int indexInPoly)
    {
        Vector3 centerInWorld = transform.position + transform.rotation * (localPos * cellSize);

        int vertexIndexInCube = MarchingCubeLookupTables.POLY_TO_CUBE_VERTEX[indexInPoly];
        int[] offsetArray = MarchingCubeLookupTables.vertexPositionOffsets[vertexIndexInCube];
        return centerInWorld + (
            offsetArray[0] * axes[0] * 0.5f
            + offsetArray[1] * axes[1] * 0.5f
            + offsetArray[2] * axes[2] * 0.5f
        );

    }

    private void LoadCPU()
    {

        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                for (int k = 0; k < gridSize; k++)
                {
                    int halfGridSize = gridSize / 2;
                    Vector3 pos = new Vector3(i - halfGridSize, j - halfGridSize, k - halfGridSize);
                    int vertIndex = (i * gridSize * gridSize + j * gridSize + k) * 6 * 4;

                    for (int vertI = 0; vertI < 24; vertI++)
                    {
                        vertexPositions[vertIndex + vertI] = getVertexGlobalPos(pos, vertI);
                        polys[vertIndex + vertI] = isInSurface(vertexPositions[vertIndex + vertI]) ? 1 : 0;
                    }
                }
            }
        }
    }

    private Mesh createMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        for (int i = 0; i < polys.Length; i = i + 4)
        {
            // get the whole poly state (all 4 bits) into one byte
            byte polyState = (byte)(
                Convert.ToByte(polys[i])
                + Convert.ToByte(polys[i + 1]) * 2
                + Convert.ToByte(polys[i + 2]) * 4
                + Convert.ToByte(polys[i + 3]) * 8
            );

            // get vertex index offsets from lookup table
            int pToVIndex = (polyState < 8) ? polyState : 15 - polyState;
            int[] pToVArray = MarchingCubeLookupTables.polyStateToVert[pToVIndex];

            // enter new vertices
            for (int j = 0; j < pToVArray.Length; j += 2)
            {
                vertices.Add(
                    (
                        vertexPositions[i + pToVArray[j]] 
                        + vertexPositions[i + pToVArray[j + 1]]
                    ) * 0.5f
                );
            }

            // get triangle index offsets from lookup table
            int[] pToTArray = MarchingCubeLookupTables.polyStateToTri[polyState];
            
            // enter new triangles
            for (int j = 0; j < pToTArray.Length; j++)
            {
                triangles.Add(vertices.Count - pToTArray[j]);
            }
        }

        // bake mesh
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // more vertices
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        Debug.Log("Done in " + Time.realtimeSinceStartup);
        return mesh;
    }

    
}
