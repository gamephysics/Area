using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

using System;
using System.Collections.Generic;
using UnityEngine;


using GuildID = System.Int64;

[AlwaysUpdateSystem]
[UpdateInGroup(typeof(FieldInitializeSystemGroup))]
[UpdateAfter(typeof(FieldInitializeSystem))]
//--------------------------------------------------------------------
// Class: AreaUpdateSystem 
// Desc : 연맹영역
//--------------------------------------------------------------------
public partial class AreaUpdateSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (CAreaRegion.Instance.m_UpdateHash.Count <= 0)
            return;

        //==================================================================================================
        // 
        //==================================================================================================
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var GroupID in CAreaRegion.Instance.m_UpdateHash)
        {
            // CACHED REMOVE
            CAreaRegion.Instance.DestroyGameObject(GroupID);

            // CREATE NEW AREA GROUP ENTITY
            if (CAreaRegion.Instance.m_AreaGroupData.TryGetValue(GroupID, out var areaGroup) == true)
            {
                // CREATE
                Entity entity = ecb.CreateEntity();
                if (entity != Entity.Null)
                {
                    // Add AREA DATA In AreaGroup
                    var areaBuffer = ecb.AddBuffer<AreaElement>(entity);
                    foreach (var a in areaGroup)
                    {
                        areaBuffer.Add(new AreaElement(a.Value.m_GuildID, a.Value.m_Priority, a.Value.m_Rect));
                    }
                    // Add Guild ID DATA In AreaGroup
                    var guildBuffer = ecb.AddBuffer<GuildAreaElement>(entity);
                    foreach (var g in areaGroup.m_RepoGuildArea)
                    {
                        guildBuffer.Add(g.Value);
                    }

                    Debug.Log("[ " + UnityEngine.Time.frameCount + " ]" + " CREATED AREA GROUP " + GroupID);
                }
            }
        }
        // CLEAR 꼭 해야한다 중간에 RETURN 하면 안된다.
        CAreaRegion.Instance.m_UpdateHash.Clear();

        ecb.Playback(EntityManager);
    }


}