using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

using System;
using System.Collections.Generic;
using UnityEngine;

[UpdateInGroup(typeof(FieldSimulationSystemGroup))]
//--------------------------------------------------------------------
// Class: FieldMeshLineSystem 
// Desc : 연맹영역 관리자
//--------------------------------------------------------------------
public partial class FieldMeshLineSystem : SystemBase
{
    private     EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    private     NativeArray<BezierLineConst> m_BezierLineConsts;

    // BEZIER 연산 함수에 넘겨주기위한 상수 값들
    private struct BezierLineConst
    {
        public bool     isLooping;      // CLOSED LINE 
        public bool     isFadeInOut;    // LINE의 시작지점과 끝지점이 서서히 안보인다.
        public float    lineWidth;      // LINE 의 두깨 (실제는 절반으로 보여진다)
        public int      curveSegment;   // 모서리를 부드럽게 라운드 형태로 구성하기위한 조각개수
        public float    curveOffset;    // 모서리의 곡률 반경
        public float    begOffset;      // 시작 지점의 강제 OFFSET
        public float    endOffset;      // 종료 지점의 강제 OFFSET
        public Color    lineColor;      // MESH LINE의 COLOR 
    }

    protected override void OnCreate()
    {
        //==================================================================================================
        // Command Buffer
        //==================================================================================================
        m_EntityCommandBufferSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();


        // 연맹영역, 필드진출 Bezier 라인 구성하기위한 정보
        m_BezierLineConsts = new NativeArray<BezierLineConst>((Int32)EnumdMeshLineType.MAX, Allocator.Persistent);

        m_BezierLineConsts[(Int32)EnumdMeshLineType.FleetLine] = new BezierLineConst
        {
            isLooping       = false,            // CLOSED LINE 
            isFadeInOut     = true,             // LINE의 시작지점과 끝지점이 서서히 안보인다.
            lineWidth       = 1f,               // LINE 의 두깨 (실제는 절반으로 보여진다)
            curveSegment    = 2,                // 모서리를 부드럽게 라운드 형태로 구성하기위한 조각개수
            curveOffset     = 0.2f,             // 모서리의 곡률 반경
            begOffset       = 0.1f,             // 시작 지점의 강제 OFFSET
            endOffset       = 0.1f,             // 종료 지점의 강제 OFFSET
            lineColor       = Color.white       // MESH LINE의 COLOR 
        };
        m_BezierLineConsts[(Int32)EnumdMeshLineType.AreaRegion] = new BezierLineConst
        {
            isLooping       = true,             // CLOSED LINE 
            isFadeInOut     = false,            // LINE의 시작지점과 끝지점이 서서히 안보인다.
            lineWidth       = 1.5f,             // LINE 의 두깨 (실제는 절반으로 보여진다)
            curveSegment    = 4,                // 모서리를 부드럽게 라운드 형태로 구성하기위한 조각개수
            curveOffset     = 0.75f,            // 모서리의 곡률 반경
            begOffset       = 0.0f,             // 시작 지점의 강제 OFFSET
            endOffset       = 0.0f,             // 종료 지점의 강제 OFFSET
            lineColor       = Color.white       // MESH LINE의 COLOR 
        };

    }
    protected override void OnDestroy()
    {
        if (m_BezierLineConsts.IsCreated)
            m_BezierLineConsts.Dispose();
    }

    protected override void OnUpdate()
    {
        //==================================================================================================
        // MESH CALCULATION
        //==================================================================================================
        var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer();

        var pathPoints  = new NativeList<float3>(10, Allocator.TempJob);
        var pathLengths = new NativeList<float>(10, Allocator.TempJob);
        var bezierConsts= m_BezierLineConsts;

        var MeshLineUpdateJobHandle = Entities
          .WithName("MeshLineUpdate")
          .ForEach((
              Entity entity,

              ref DynamicBuffer<FieldMeshVertexElement>     vertices,
              ref DynamicBuffer<FieldMeshTriangleElement>   triangles,
              ref DynamicBuffer<FieldMeshUVElement>         UVs,
              ref DynamicBuffer<FieldMeshColorElement>      colors,

              in FieldMeshLineData                      lineData,
              in DynamicBuffer<FieldMeshPointElement>   linePoints  ) =>
          {
              // 이중 연산을 방지하고 불필요해진 BUFFER 제거
              commandBuffer.RemoveComponent(entity, ComponentType.ReadOnly<FieldMeshPointElement>());

              var BezierConst = bezierConsts[(Int32)lineData.m_Type];

              // VALUE 설정
              BezierLine.GenMeshData genMeshData = new BezierLine.GenMeshData();
              genMeshData.vertices      = vertices;
              genMeshData.triangles     = triangles;
              genMeshData.uv            = UVs;
              genMeshData.colors        = colors;
              genMeshData.isLoop        = BezierConst.isLooping;        // 상수값을 설정
              genMeshData.curveSegment  = BezierConst.curveSegment;
              genMeshData.curveOffset   = BezierConst.curveOffset;
              genMeshData.lineWidth     = BezierConst.lineWidth;
              genMeshData.vertexColor   = BezierConst.lineColor;
              genMeshData.totalLength   = 0.0f;
              genMeshData.contRatio     = 1.0f;

              genMeshData.pathPoints    = pathPoints;       // POINT BUFFER
              genMeshData.pathLengths   = pathLengths;      // LINE  BUFFER
              genMeshData.pathPoints.Clear();
              genMeshData.pathLengths.Clear();

              // LINE POINT 복사
              for (int i = 0; i < linePoints.Length; ++i)
              {
                  genMeshData.pathPoints.Add(new float3(linePoints[i].Value.x, 0.01f, linePoints[i].Value.y));
              }

              if (BezierConst.begOffset > 0) { _AdjustOffset(BezierConst.begOffset, false, ref genMeshData.pathPoints); }
              if (BezierConst.endOffset > 0) { _AdjustOffset(BezierConst.endOffset, true,  ref genMeshData.pathPoints); }

              // BEZIER RENDER MESH 연산
              BezierLine.InitializePath(ref genMeshData);
              BezierLine.GenerateSmoothLine(ref genMeshData);
              
              // FADE IN OUT 설정
              if (!BezierConst.isLooping && BezierConst.isFadeInOut && genMeshData.colors.Length > 2)
              {
                  genMeshData.colors[0] = new Color(BezierConst.lineColor.r, BezierConst.lineColor.g, BezierConst.lineColor.b, 0);
                  genMeshData.colors[1] = genMeshData.colors[0];
                  genMeshData.colors[genMeshData.colors.Length - 1] = genMeshData.colors[0];
                  genMeshData.colors[genMeshData.colors.Length - 2] = genMeshData.colors[0];
              }
          })
          .WithDisposeOnCompletion(pathPoints)
          .WithDisposeOnCompletion(pathLengths)
          .Schedule(this.Dependency);

        //==================================================================================================
        // ADD JOB BARRIER FOR USING
        //==================================================================================================
        m_EntityCommandBufferSystem.AddJobHandleForProducer(MeshLineUpdateJobHandle);

        //==================================================================================================
        // Dependency = LAST JOB
        //==================================================================================================
        this.Dependency = MeshLineUpdateJobHandle;
    }

    private static void _AdjustOffset(float offset, bool reverse, ref NativeList<float3> paths)
    {
        float   lenght  = 0;
        int     index0  = 0;
        int     index1  = 1;

        while (lenght < offset && paths.Length > 1)
        {
            if (reverse)
            {
                index0 = paths.Length - 1;
                index1 = paths.Length - 2;
            }
            lenght = math.distance(paths[index0], paths[index1]);
            if (lenght < offset)
                paths.RemoveAt(index1);
            else
                paths[index0] = paths[index0] + math.normalizesafe(paths[index1] - paths[index0]) * offset;
        }
    }

}