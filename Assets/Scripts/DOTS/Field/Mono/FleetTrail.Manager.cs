using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

using System;
using System.Collections.Generic;
using UnityEngine;

using CORE;

using GuildID = System.Int64;



//--------------------------------------------------------------------
// Class: CFleet
// Desc : 부대 진출 라인 1개
//--------------------------------------------------------------------
public struct CFleet
{
    // KEY
    public Int64    m_ID;
    // DATA
    public EnumFleetTrait       m_Trait;       
    public EnumFleetRelation    m_Relation;
    public float                m_Speed;
    public List<Vector2>        m_PositionList;
    

    //--------------------------------------------------------------------
    // 생성자
    //--------------------------------------------------------------------
    public CFleet(Int64 fleetID, EnumFleetTrait trait, EnumFleetRelation relation, float speed, List<Vector2> pointlists)
    {
        m_ID            = fleetID;

        // DATA
        m_Trait         = trait;
        m_Relation      = relation;
        m_Speed         = speed;

        m_PositionList  = pointlists;
    }
};


//--------------------------------------------------------------------
// class : CFleetTrail
// Desc  : 부대 진출 라인 
//--------------------------------------------------------------------
public partial class CFleetTrail : TSingleton<CFleetTrail>
{
    // FLEET
    public Dictionary<Int64, CFleet>        m_FleetData;         // KEY : ID

    // 
    public HashSet<Int64>                   m_UpdateHash;       // 갱신해야할 Fleet

    //===============================================================
    // GAME OBJECT
    //===============================================================
    public MultiValueDictionary<Int64, GameObject> m_Cached;

    //--------------------------------------------------------------------
    // Code	: 생성자
    // Desc	: 부대 진출 라인 관리자 
    //--------------------------------------------------------------------
    CFleetTrail()
    {
        m_FleetData     = new Dictionary<Int64, CFleet>();
        m_UpdateHash    = new HashSet<Int64>();

        m_Cached        = new MultiValueDictionary<Int64, GameObject>();
    }

    public void Clear()
    {
        m_FleetData.Clear();
        m_UpdateHash.Clear();

        // DESTROY GAME OBJECTS
        foreach (var c in m_Cached)
        {
            foreach (var gameObject in c.Value)
            {
                GameObject.Destroy(gameObject);
            }
        }
        m_Cached.Clear();
    }

    //--------------------------------------------------------------------
    // Code	: Add()
    // Desc	: 부대 진출 라인 1개 추가
    //--------------------------------------------------------------------
    public bool Add(ref CFleet fleetData)
    {
        if(m_FleetData.ContainsKey(fleetData.m_ID))
        {                                             
            m_FleetData.Remove(fleetData.m_ID);
        }

        m_FleetData.Add(fleetData.m_ID, fleetData);
        _PushUpdateID(fleetData.m_ID);

        return true;
    }

    //--------------------------------------------------------------------
    // Code	: Remove()
    // Desc	: 부대 진출 라인 1개 제거
    //--------------------------------------------------------------------
    public Int64 Remove(Int64 fleetID)
    {
        // AREA 제거
        if (m_FleetData.Remove(fleetID))
        {
            _PushUpdateID(fleetID);
        }

        return 0;
    }

    //--------------------------------------------------------------------
    // Code : DestroyGameObject
    // Desc : Line Mesh Game Objects 제거
    //--------------------------------------------------------------------
    public void DestroyGameObject(Int64 FleetID)
    {
        if (m_Cached == null)
            return;

        foreach (var gameObject in m_Cached.GetValues(FleetID))
        {
            GameObject.Destroy(gameObject);

            m_Cached.Remove(FleetID);

            Debug.Log("[ " + UnityEngine.Time.frameCount + " ]" + " DestroyGameObject() FleetID : " + FleetID);
        }
    }
    //--------------------------------------------------------------------
    // Code : DestroyGameObject
    // Desc : Line Mesh Game Objects 제거
    //--------------------------------------------------------------------
    public void AddGameObject(Int64 FleetID, GameObject obj)
    {
        if (m_Cached == null || obj == null)
            return;

        m_Cached.Add(FleetID, obj);

        Debug.Log("[ " + UnityEngine.Time.frameCount + " ]" + " AddGameObject() FleetID : " + FleetID);
    }

    //--------------------------------------------------------------------
    // Code	: _PushUpdateID()
    // Desc	: Update Fleet
    //--------------------------------------------------------------------
    private void _PushUpdateID(Int64 fleetID)
    {
        // 갱신되어진 GROUP 기록
        if (!m_UpdateHash.Contains(fleetID))
            m_UpdateHash.Add(fleetID);
    }


}

