using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MarchingCubeLookupTables
{
    // maps vertex indices of tetrahedron to their indices on the cube
    // each pair of 4 indices is one tetrahedron
    public static int[] POLY_TO_CUBE_VERTEX = {
        5, 4, 1, 7,
        6, 5, 1, 7,
        4, 0, 1, 7,
        2, 6, 1, 7,
        0, 3, 1, 7,
        3, 2, 1, 7
    };

    public static int[][] vertexPositionOffsets =
        /// offsets in x, y, z to get from the center of a cube to the vertex at the index's position
    {
        new int[] { -1, -1, -1 }, // 0
        new int[] { -1, -1, 1 }, // 1
        new int[] { 1, -1, 1 }, // 2
        new int[] { 1, -1, -1 }, // 3
        new int[] { -1, 1, -1 }, // 4
        new int[] { -1, 1, 1 }, // 5
        new int[] { 1, 1, 1 }, // 6
        new int[] { 1, 1, -1 }, // 7
    };

    public static int[][] polyStateToVert =
        /// ith array in this array represents the pairs of index offsets to interpolate between for the state associated with that index
    {
        new int[] {}, // 0, 15
        new int[] { 0, 1, 0, 2, 0, 3 }, // 1, 14
        new int[] { 1, 0, 1, 2, 1, 3 }, // 2, 13
        new int[] { 1, 2, 1, 3, 0, 3, 0, 2 }, // 3, 12
        new int[] { 0, 2, 2, 3, 1, 2 }, // 4, 11
        new int[] { 0, 1, 1, 2, 0, 3, 2, 3 }, // 5, 10
        new int[] { 0, 1, 2, 3, 1, 3, 0, 2 }, // 6, 9
        new int[] { 0, 3, 1, 3, 2, 3 } // 7, 8
    };

    public static int[][] polyStateToTri =
        /// ith array in this array is the offset from the vertices array count for each poly in the triangle array to add
    {
        new int[] {}, // 0
        new int[] {1, 2, 3}, // 1
        new int[] {3, 2, 1}, // 2
        new int[] {4, 3, 2, 1, 4, 2}, //3
        new int[] {3, 2, 1}, // 4
        new int[] {2, 3, 4, 1, 3, 2}, // 5
        new int[] {4, 3, 2, 3, 4, 1}, // 6
        new int[] {1, 2, 3}, // 7

        new int[] {3, 2, 1}, // 8
        new int[] {2, 3, 4, 1, 4, 3}, // 9
        new int[] {4, 3, 2, 2, 3, 1}, // 10
        new int[] {1, 2, 3}, // 11
        new int[] {2, 3, 4, 2, 4, 1}, // 12
        new int[] {1, 2, 3}, // 13
        new int[] {3, 2, 1}, // 14
        new int[] {} // 15
    };
}

/*
 * old code before lookup tables:
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
        */
