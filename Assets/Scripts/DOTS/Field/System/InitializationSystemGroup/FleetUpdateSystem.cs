using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

using System;
using System.Collections.Generic;
using UnityEngine;

using GuildID = System.Int64;

[AlwaysUpdateSystem]
[UpdateInGroup(typeof(FieldInitializeSystemGroup))]
[UpdateAfter(typeof(AreaPointLineSystem))]
//--------------------------------------------------------------------
// Class: AreaRegionSystem 
// Desc : AreaGruop의 Area 들로 연맹영억의 LINE POINT 생성 (Outline,HoleLines)
//--------------------------------------------------------------------
public class FleetUpdateSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (CFleetTrail.Instance.m_UpdateHash.Count <= 0)
            return;

        //==================================================================================================
        // 
        //==================================================================================================
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var FleetID in CFleetTrail.Instance.m_UpdateHash)
        {
            // CACHED REMOVE
            CFleetTrail.Instance.DestroyGameObject(FleetID);

            // CREATE NEW AREA GROUP ENTITY
            if (CFleetTrail.Instance.m_FleetData.TryGetValue(FleetID, out var fleetTrail) == true)
            {
                AddFleetTrail(ecb, fleetTrail.m_ID, fleetTrail.m_Trait, fleetTrail.m_Relation, fleetTrail.m_Speed, fleetTrail.m_PositionList);
            }
        }
        // CLEAR 꼭 해야한다 중간에 RETURN 하면 안된다.
        CFleetTrail.Instance.m_UpdateHash.Clear();

        ecb.Playback(EntityManager);
    }

    public void AddFleetTrail(EntityCommandBuffer commandBuffer, Int64 fleetID, EnumFleetTrait trait, EnumFleetRelation relation, float speed, List<Vector2> pointList)
    {
        if (pointList.Count <= 0)
            return;

        //==================================================================================================
        // CREATE NEW FLEET ENTITY
        //==================================================================================================
        {
            // CREATE
            Entity entity = commandBuffer.CreateEntity();
            if (entity != Entity.Null)
            {
                // ADD DATA 기본정보
                commandBuffer.AddComponent(entity, new FieldMeshLineData
                {
                    m_ID        = fleetID,
                    m_Type      = EnumdMeshLineType.FleetLine,
                    m_Material  = (Int16)trait,
                    m_Color     = (Int16)relation,
                    m_Speed     = speed
                });

                //===================================================
                // RENDERMESH  RAW DATA
                //===================================================
                var pointBuffer = commandBuffer.AddBuffer<FieldMeshPointElement>(entity);
                foreach (var point in pointList)
                {
                    pointBuffer.Add(point);
                }

                // BUFFER 추가 계산을 위한
                commandBuffer.AddBuffer<FieldMeshVertexElement>(entity);
                commandBuffer.AddBuffer<FieldMeshTriangleElement>(entity);
                commandBuffer.AddBuffer<FieldMeshUVElement>(entity);
                commandBuffer.AddBuffer<FieldMeshColorElement>(entity);

                Debug.Log("CREATED FLEET TRAIL " + entity);
            }
        }
    }

}