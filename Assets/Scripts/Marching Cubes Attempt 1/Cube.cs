using System;
using UnityEngine;

public class Cube
{
    // each 4-byte sequence designates the state of one polyhedron
    //  6    5    4    3    2    1  
    // 0000 0000 0000 0000 0000 0000
    public int polyhedra;
    public Vector3 pos;
    public Vector3[] vertexPositions;

    // maps poly indices to the indices of their vertices on the cube
    public static int[] POLY_TO_VERTEX = { 
        5, 4, 1, 7,
        6, 5, 1, 7,
        4, 0, 1, 7,
        2, 6, 1, 7,
        0, 3, 1, 7,
        3, 2, 1, 7 // to consider later: the last two indices of each polygon is redundant
    };

    public Cube(Vector3 _pos)
    {
        pos = _pos;
        vertexPositions = null;
        
        polyhedra = 0;
    }

    // set vertex position table from grid origin and cell size
    public void setVertexPos(Vector3 origin, Quaternion rot, float cellSize)
    {
        // goes bottom to top, clockwise starting from the upper left looking down from above:
        /* 0->--1   4->--5   
         * | do v   | up v   
         * ^ wn |   ^    |   
         * 3--<-2   7--<-6   
         * */
        vertexPositions = new Vector3[8];
        Vector3 centerInWorld = origin + rot * (pos * cellSize);
        float halfCellSize = cellSize * 0.5f;
        Vector3 xAxis = rot * new Vector3(halfCellSize, 0, 0);
        Vector3 yAxis = rot * new Vector3(0, halfCellSize, 0);
        Vector3 zAxis = rot * new Vector3(0, 0, halfCellSize);

        vertexPositions[0] = centerInWorld - xAxis - yAxis - zAxis;
        vertexPositions[1] = centerInWorld - xAxis - yAxis + zAxis;
        vertexPositions[2] = centerInWorld + xAxis - yAxis + zAxis;
        vertexPositions[3] = centerInWorld + xAxis - yAxis - zAxis;
        vertexPositions[4] = centerInWorld - xAxis + yAxis - zAxis;
        vertexPositions[5] = centerInWorld - xAxis + yAxis + zAxis;
        vertexPositions[6] = centerInWorld + xAxis + yAxis + zAxis;
        vertexPositions[7] = centerInWorld + xAxis + yAxis - zAxis;

    }

    public void setPolyValues(Func<Vector3,bool> valueFunction)
    {
        if (vertexPositions != null)
        {
            for (int polyVertI = 0; polyVertI < 24; polyVertI++)
            {
                Vector3 vertPos = vertexPositions[POLY_TO_VERTEX[polyVertI]];
                if (valueFunction(vertPos))
                    polyhedra |= 1 << polyVertI;
            }
        } else
            Debug.LogError("setPolyValues called before vertex positions have been set!");
        
    }

    public void setPolyDirectly(int polyValue)
    {
        polyhedra = polyValue;
    }


    public Vector3 polyVertexInterp(int polyVert1, int polyVert2)
    {
        return (vertexPositions[POLY_TO_VERTEX[polyVert1]] + vertexPositions[POLY_TO_VERTEX[polyVert2]]) * 0.5f;
    }
}
