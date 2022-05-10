using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

using System;
using System.Collections.Generic;
using UnityEngine;

using CORE;

[UpdateInGroup(typeof(FieldPresentationSystemGroup))]
//--------------------------------------------------------------------
// Class: FieldMeshRenderSystem 
// Desc : Line Mesh Game Object 생성 관리 
//--------------------------------------------------------------------
public partial class FieldMeshRenderSystem : SystemBase
{
    //=======================================================================================
    // GRAPHIC
    //=======================================================================================
    GameObject[]                            m_LineObject = null;


    protected override void OnCreate()
    {
        m_LineObject        = new GameObject[2];
        m_LineObject[0]     = Resources.Load<GameObject>("Prefabs/FieldMeshLine/FX_F_BattleLine");  // 필드 진출라인 Prefab
        m_LineObject[1]     = Resources.Load<GameObject>("Prefabs/FieldMeshLine/FX_F_AreaLine");    // 연맹영역 Prefab
    }

    protected override void OnUpdate()
    {
        if (m_LineObject == null)
            return;

        //==================================================================================================
        // MAKE RENDER MESH
        //==================================================================================================
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        Entities
            .WithName("RenderMesh")
            .WithoutBurst()
            .WithNone<FieldMeshPointElement>()
            .ForEach((
                Entity entity,
                in FieldMeshLineData                        meshLine,
                in DynamicBuffer<FieldMeshVertexElement>    vertices,
                in DynamicBuffer<FieldMeshTriangleElement>  triangles,
                in DynamicBuffer<FieldMeshUVElement>        UVs,
                in DynamicBuffer<FieldMeshColorElement>     colors
                ) =>
            {
                var mesh = new Mesh();

                // BEZIER에서 연산된 정보를 MESH에 설정한다.
                if (vertices.Length > 0)
                {
                    var meshVertex = new Vector3[vertices.Length];
                    for (int i = 0; i < vertices.Length; ++i)
                    {
                        meshVertex[i] = vertices[i];
                    }
                    mesh.vertices = meshVertex;
                }
                if(triangles.Length > 0)
                {
                    var meshTriangle = new int[triangles.Length];
                    for (int i = 0; i < triangles.Length; ++i)
                    {
                        meshTriangle[i] = triangles[i];
                    }
                    mesh.triangles = meshTriangle;
                }
                if (UVs.Length > 0)
                {
                    var meshUVs = new Vector2[UVs.Length];
                    for (int i = 0; i < UVs.Length; ++i)
                    {
                        meshUVs[i] = UVs[i];
                    }
                    mesh.uv = meshUVs;
                }
                if (colors.Length > 0)
                {
                    var meshColors = new Color[colors.Length];
                    for (int i = 0; i < colors.Length; ++i)
                    {
                        meshColors[i] = colors[i];
                    }
                    mesh.colors = meshColors;
                }

                mesh.RecalculateBounds();

                // CREAT GAME OBJECT
                CreateGameObject(in meshLine, mesh);

                // 제거
                ecb.DestroyEntity(entity);

            })
            .Run();


        ecb.Playback(EntityManager);
    }


    //--------------------------------------------------------------------
    // Code : CreateGameObject
    // Desc : Line Mesh Game Object 생성
    //--------------------------------------------------------------------
    public void CreateGameObject(in FieldMeshLineData meshLine, Mesh mesh)
    {
        // Prefab NOT LOADED
        if (m_LineObject == null || m_LineObject.Length <= (Int32)meshLine.m_Type || m_LineObject[(Int32)meshLine.m_Type] == null)
            return;

        //===============================================================
        // GameObject Instantiate 및 등록관리 
        //===============================================================
        var gameObject = GameObject.Instantiate<GameObject>(m_LineObject[(Int32)meshLine.m_Type]);
        if (gameObject == null)
            return;

        switch (meshLine.m_Type)
        {
            case EnumdMeshLineType.FleetLine:
                // CACHED
                CFleetTrail.Instance.AddGameObject(meshLine.m_ID, gameObject);
                break;
            case EnumdMeshLineType.AreaRegion:
                // CACHED
                CAreaRegion.Instance.AddGameObject(meshLine.m_ID, gameObject);
                break;
        }
        //===============================================================

        // Prefab 설정
        var meshMaterials = gameObject.GetComponent<BezierPathMaterial>();
        if (meshMaterials != null)
        {
            meshMaterials.SetMaterialColor(meshLine.m_Material, meshLine.m_Color, meshLine.m_Speed);
        }

        var meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            // MESH
            meshFilter.mesh = mesh;
        }
    }

}