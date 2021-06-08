using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

using System;
using System.Collections.Generic;
using UnityEngine;

//--------------------------------------------------------------------
// Class: BezierLine
// Desc : Line Mesh 연산 및 생성
//--------------------------------------------------------------------
public static class BezierLine
{
    public struct GenMeshData
    {
        public DynamicBuffer<FieldMeshVertexElement> vertices;
        public DynamicBuffer<FieldMeshUVElement> uv;
        public DynamicBuffer<FieldMeshTriangleElement> triangles;
        public DynamicBuffer<FieldMeshColorElement> colors;

        public bool isLoop;
        public int curveSegment;   // CURVE에서 생성될 Segment 개수
        public float curveOffset;
        public float lineWidth;
        public Color vertexColor;

        // runtime temporal data
        public quaternion qrot;
        public float3 n0;
        public float3 n1;
        public float3 p0;
        public float3 p1;
        public float uvbase;
        public float hw;
        public float curSegLen;

        public float totalLength;
        public float contRatio;

        public NativeList<float3> pathPoints;
        public NativeList<float> pathLengths;

    }

    static public void InitializePath(ref GenMeshData gmd)
    {
        float3 p0 = gmd.pathPoints[0];
        for (int i = 1; i < gmd.pathPoints.Length; ++i)
        {
            float3 p1 = gmd.pathPoints[i];

            gmd.pathLengths.Add(math.length(p1 - p0));
            gmd.totalLength += gmd.pathLengths[i - 1];
            p0 = p1;
        }
        if (gmd.isLoop)
        {
            p0 = gmd.pathPoints[0];
            float3 pL = gmd.pathPoints[gmd.pathPoints.Length - 1];

            gmd.pathLengths.Add(math.length(p0 - pL));
            gmd.totalLength += gmd.pathLengths[gmd.pathLengths.Length - 1];
        }
    }

    static public void GenerateSmoothLine(ref GenMeshData gmd)
    {
        if (gmd.pathPoints.Length < 2) { return; }
        if (gmd.curveOffset <= 0) { GenerateLine(ref gmd); return; }
        if (gmd.curveSegment < 1) { gmd.curveSegment = 1; }

        float difflength = gmd.pathLengths.Length * (gmd.curveOffset * 2 - gmd.curveOffset * math.PI / 2.0f);
        gmd.contRatio = (gmd.totalLength - difflength) / gmd.totalLength;

        gmd.qrot = quaternion.AxisAngle(math.down(), math.radians(-90));
        gmd.n0 = float3.zero;
        gmd.n1 = float3.zero;
        gmd.p0 = gmd.pathPoints[0];
        gmd.p1 = gmd.pathPoints[1];
        gmd.uvbase = 0;
        gmd.hw = gmd.lineWidth * 0.5f;

        _LerpSegment(ref gmd, 0, 0, ref gmd.p0, ref gmd.n0);
        gmd.n0 = math.mul(gmd.qrot, gmd.n0);

        for (int endvidx = 1; endvidx <= gmd.pathPoints.Length; ++endvidx, gmd.p0 = gmd.p1)
        {
            if (endvidx == gmd.pathPoints.Length && !gmd.isLoop)
                break;

            gmd.curSegLen = 0;
            int begvidx = endvidx - 1;
            {
                if (!gmd.isLoop && begvidx == 0)
                    _MakeSmoothLineSegment(ref gmd, gmd.curveOffset, 1, begvidx);
                else
                    _MakeSmoothLineSegment(ref gmd, gmd.curveOffset, gmd.curveSegment, begvidx);
            }
            {
                _MakeSmoothLineSegment(ref gmd, gmd.pathLengths[begvidx] - gmd.curveOffset * 2, 1, begvidx);
            }
            {
                if (!gmd.isLoop && endvidx == gmd.pathPoints.Length - 1)
                    _MakeSmoothLineSegment(ref gmd, gmd.curveOffset, 1, begvidx);
                else
                    _MakeSmoothLineSegment(ref gmd, gmd.curveOffset, gmd.curveSegment, begvidx);
            }
        }
    }
    static public void GenerateLine(ref GenMeshData gmd)
    {
        gmd.qrot = quaternion.AxisAngle(math.down(), math.radians(-90));
        gmd.uvbase = 0;
        gmd.hw = gmd.lineWidth * 0.5f;
        gmd.n0 = float3.zero;
        gmd.n1 = float3.zero;
        gmd.p0 = gmd.pathPoints[0];
        //genMeshData.p1 = pathPoints[1];

        for (int i = 1; i <= gmd.pathPoints.Length; ++i, gmd.p0 = gmd.p1)
        {
            if (i == gmd.pathPoints.Length && !gmd.isLoop)
                break;

            gmd.p1 = gmd.pathPoints[_WrapListPos(ref gmd, i)];
            gmd.n0 = gmd.n1 = math.mul(gmd.qrot, math.normalizesafe(gmd.p1 - gmd.p0));
            _MakeQuadMesh(ref gmd, math.length(gmd.p1 - gmd.p0));
        }
    }

    //-------------------------------------------------------------------------
    static int _WrapListPos(ref GenMeshData gmd, int pos)
    {
        pos %= gmd.pathPoints.Length;
        if (pos < 0)
            pos = gmd.pathPoints.Length + pos;

        return pos;
    }
    static public void _LerpSegment(ref GenMeshData gmd, float cur_len, int idx, ref float3 pos, ref float3 tan)
    {
        float3 pp = gmd.pathPoints[_WrapListPos(ref gmd, idx - 1)];
        float3 p0 = gmd.pathPoints[_WrapListPos(ref gmd, idx)];
        float3 p1 = gmd.pathPoints[_WrapListPos(ref gmd, idx + 1)];
        float3 p2 = gmd.pathPoints[_WrapListPos(ref gmd, idx + 2)];
        //      len1         len2   
        // p0-----x-----------x------p1
        //         <- ^ ->           ^
        //         cur_len        max_len
        float max_len = gmd.pathLengths[idx];
        float len1 = gmd.curveOffset;
        float len2 = max_len - gmd.curveOffset;

        if (cur_len < len1)
        {
            if (!gmd.isLoop && idx == 0)
            {
                //pos = p0;
                pos = math.lerp(p0, p1, cur_len / max_len);
                tan = math.normalizesafe(p1 - p0);
            }
            else
            {
                float3 ep0 = math.normalizesafe(pp - p0) * gmd.curveOffset + p0;
                float3 ep1 = math.normalizesafe(p1 - p0) * gmd.curveOffset + p0;
                float cr = cur_len / gmd.curveOffset * 0.5f + 0.5f;
                pos = GetQuadraticBazierPosition(cr, ep0, p0, ep1);
                tan = GetQuadraticBazierTangent(cr, ep0, p0, ep1);
            }
        }
        else if (cur_len > len2)
        {
            if (!gmd.isLoop && idx >= gmd.pathPoints.Length - 2)
            {
                //pos = p1;
                pos = math.lerp(p0, p1, cur_len / max_len);
                tan = math.normalizesafe(p1 - p0);
            }
            else
            {
                float3 ep0 = math.normalizesafe(p0 - p1) * gmd.curveOffset + p1;
                float3 ep1 = math.normalizesafe(p2 - p1) * gmd.curveOffset + p1;
                float cr = (cur_len - len2) / gmd.curveOffset * 0.5f;
                pos = GetQuadraticBazierPosition(cr, ep0, p1, ep1);
                tan = GetQuadraticBazierTangent(cr, ep0, p1, ep1);
            }
        }
        else
        {
            pos = math.lerp(p0, p1, cur_len / max_len);
            tan = math.normalizesafe(p1 - p0);
            //pos = curPP.pos;
        }
    }

    static private void _MakeSmoothLineSegment(ref GenMeshData gmd, float segLength, int segCount, int vidx)
    {
        float segOffset = segLength / segCount;
        float ratio     = gmd.contRatio;

        bool curr       = gmd.pathLengths[_WrapListPos(ref gmd, vidx)] > 1.0f;
        int segStart    = 0;
        int segEnd      = segCount;

        if (curr == false && gmd.curveOffset >= 0.5f)
        {
            segCount    = 1;
            segEnd      = segCount;
            segOffset   = segLength / segCount;
        }

        for (int seg = segStart; seg < segEnd; ++seg, gmd.p0 = gmd.p1, gmd.n0 = gmd.n1)
        {
            gmd.curSegLen += segOffset;
            _LerpSegment(ref gmd, gmd.curSegLen, vidx, ref gmd.p1, ref gmd.n1);
            gmd.n1 = math.mul(gmd.qrot, gmd.n1);
            float ulen = math.length(gmd.p1 - gmd.p0) * 1.0f / gmd.lineWidth / ratio;
            _MakeQuadMesh(ref gmd, ulen);
            gmd.uvbase += ulen;
        }
    }

    static private void _MakeQuadMesh(ref GenMeshData gmd, float ulen)
    {
        if (gmd.vertices.IsCreated)
        {
            gmd.vertices.Add(gmd.p0 + gmd.n0 * gmd.hw);
            gmd.vertices.Add(gmd.p0 + gmd.n0 * -gmd.hw);
            gmd.vertices.Add(gmd.p1 + gmd.n1 * gmd.hw);
            gmd.vertices.Add(gmd.p1 + gmd.n1 * -gmd.hw);
        }

        if (gmd.colors.IsCreated)
        {
            Color color = gmd.vertexColor;
            color.a = 1f;
            gmd.colors.Add(color);
            gmd.colors.Add(color);
            gmd.colors.Add(color);
            gmd.colors.Add(color);
        }

        if (gmd.uv.IsCreated)
        {
            gmd.uv.Add(new float2(gmd.uvbase, 0));
            gmd.uv.Add(new float2(gmd.uvbase, 1));
            gmd.uv.Add(new float2(gmd.uvbase + ulen, 0));
            gmd.uv.Add(new float2(gmd.uvbase + ulen, 1));
        }

        if (gmd.triangles.IsCreated)
        {
            int idxv = gmd.vertices.Length - 4;
            gmd.triangles.Add(idxv);
            gmd.triangles.Add(idxv + 1);
            gmd.triangles.Add(idxv + 2);
            gmd.triangles.Add(idxv + 2);
            gmd.triangles.Add(idxv + 1);
            gmd.triangles.Add(idxv + 3);
        }
    }
    static public float3 GetQuadraticBazierPosition(float t, float3 p0, float3 p1, float3 p2)
    {
        float u = 1 - t;
        float uu = u * u;
        float tt = t * t;
        float3 p = uu * p0;
        p += 2 * u * t * p1;
        p += tt * p2;
        return p;
    }

    static public float3 GetQuadraticBazierTangent(float t, float3 p0, float3 p1, float3 p2)
    {
        float u = 1 - t;
        float3 pon1 = u * p0 + t * p1;
        float3 pon2 = u * p1 + t * p2;
        return math.normalizesafe(pon2 - pon1);
    }
}
