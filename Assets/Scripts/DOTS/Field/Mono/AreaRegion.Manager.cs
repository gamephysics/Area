using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using CORE;

using GuildID = System.Int64;

//--------------------------------------------------------------------
// class : CAreaRegion 
// Desc  : 연맹영역 관리자
//--------------------------------------------------------------------
// ONE(오션앤 앰파이어), POC(캐리비안의 해적) : 타 연맹과 연맹영역이 서로 겹치지 않는것이 규칙이었다.
//                      (GROUPING 규칙이 GUILD 단위)
// CF (크로스파이어) : 타 연맹 영역끼리 겹쳐질수있고, 해당 CELL의 점유는 먼저 점유한 연맹의 소유로 한다.
//                      (GROUPING 규칙이 서로 겹쳐지는 단위로 만든뒤, 내부적으로 GUILD 별로 연맹라인 MESH LINE을 생성한다.)
//--------------------------------------------------------------------
public partial class CAreaRegion : TSingleton<CAreaRegion>
{
    // AREA
    private Dictionary<Int64,  CArea>       m_AreaData;         // KEY : Area Position
    // AREA GROUP
    public  Dictionary<Int64, CAreaGroup>   m_AreaGroupData;    // KEY : GroupID (Generated)

    //===============================================================
    // GROUP ID
    //===============================================================
    private Int64                           m_GroupIDSerial = 1;    // AreaGroup ID 시리얼    

    public HashSet<Int64>                   m_UpdateHash;           // 갱신해야할 AreaGroup


    //===============================================================
    // GAME OBJECT
    //===============================================================
    public MultiValueDictionary<Int64, GameObject> m_Cached;

    //--------------------------------------------------------------------
    // Code	: 생성자
    // Desc	: 연맹영역 관리자 
    //--------------------------------------------------------------------
    CAreaRegion()
    {
        m_AreaData      = new Dictionary<Int64,  CArea>();
        m_AreaGroupData = new Dictionary<Int64,  CAreaGroup>();

        m_GroupIDSerial = 1;
        m_UpdateHash    = new HashSet<Int64>();

        m_Cached        = new MultiValueDictionary<Int64, GameObject>();
    }

    public void Clear()
    {
        m_AreaData.Clear();
        m_AreaGroupData.Clear();

        m_GroupIDSerial = 1;
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
    // Desc	: 연맹 영역 1개 영역 추가
    //--------------------------------------------------------------------
    public Int64 Add(ref CArea newArea)
    {
        // 예외처리 : 기존좌표에 존재하고 있는 Area 는 제거 하고 추가한다.
        if (m_AreaData.TryGetValue(newArea.m_Key, out var oldArea))
        {
            // 동일한 값이면 무시한다.
            if (oldArea.IsCompare(ref newArea))
                return 0;

            if (m_AreaData.Remove(newArea.m_Key))
            {
                // 해당 AREA 를 소유하고 있는 GROUP 을 찾는다.
                foreach (var key in m_AreaGroupData.Keys.ToList())
                {
                    // 해당 AreaGroup에서 Area 를 제거 하였으면
                    if (m_AreaGroupData[key].RemoveArea(newArea.m_Key))
                    {
                        // GROUP 제거하기전에 임시변수
                        var removeGroup = m_AreaGroupData[key];

                        // GROUP 제거
                        m_AreaGroupData.Remove(key);
                        _PushUpdateID(key);

                        // GROUP에 AREA 들 추가 갱신
                        foreach (var a in removeGroup)
                        {
                            _AddDistinctGroups(a.Value);
                        }
                    }
                }
            }
        }

        // AREA 추가
        m_AreaData.Add(newArea.m_Key, newArea);
        // GROUP에 추가 갱신
        return _AddDistinctGroups(newArea);
    }

    //--------------------------------------------------------------------
    // Code	: Remove()
    // Desc	: 연맹 영역 1개 영역 제거
    //        Position 정보를 기반으로 Area 를 제거하고 갱신한다.
    //--------------------------------------------------------------------
    public Int64 Remove(int2 pos)
    {
        // Data 제거
        Int64 Key = CArea.Make32BitKeyValue(pos.x, pos.y);

        // AREA 제거
        if (m_AreaData.Remove(Key))
        {
            // GROUP에 제거 갱신
            return _DelDistinctGroups(Key);
        }

        return 0;
    }

    //--------------------------------------------------------------------
    // Code : DestroyGameObject
    // Desc : Line Mesh Game Objects 제거
    //--------------------------------------------------------------------
    public void DestroyGameObject(Int64 GroupID)
    {
        if (m_Cached == null)
            return;

        foreach (var gameObject in m_Cached.GetValues(GroupID))
        {
            GameObject.Destroy(gameObject);

            m_Cached.Remove(GroupID);

            Debug.Log("[ " + UnityEngine.Time.frameCount + " ]" + " DestroyGameObject() GroupID : " + GroupID);
        }
    }
    //--------------------------------------------------------------------
    // Code : DestroyGameObject
    // Desc : Line Mesh Game Objects 제거
    //--------------------------------------------------------------------
    public void AddGameObject(Int64 GroupID, GameObject obj)
    {
        if (m_Cached == null || obj == null)
            return;

        m_Cached.Add(GroupID, obj);

        Debug.Log("[ " + UnityEngine.Time.frameCount + " ]" + " AddGameObject() GroupID : " + GroupID);
    }


    //==================================================================================================
    // PRIVAT
    //==================================================================================================
    // AREA GROUP
    //--------------------------------------------------------------------
    // Code	: _DelDistinctGroups()
    // Desc	: Area 제거 후 재계산
    //--------------------------------------------------------------------
    private Int64 _DelDistinctGroups(Int64 AreaKey)
	{
        // 해당 AREA 를 소유하고 있는 GROUP 을 찾는다.
        foreach (var key in m_AreaGroupData.Keys.ToList())
		{
            // 해당 AreaGroup에서 Area 를 제거 하였으면
            if (m_AreaGroupData[key].RemoveArea(AreaKey))
			{
                // GROUP 제거하기전에 임시변수
                var removeGroup = m_AreaGroupData[key];

                // GROUP 제거
                m_AreaGroupData.Remove(key);
                _PushUpdateID(key);

                // GROUP에 AREA 들 추가 갱신
                foreach (var a in removeGroup)
				{
                    _AddDistinctGroups(a.Value);
                }

                return key;
			}
		}

		return 0;
	}

    //--------------------------------------------------------------------
    // Code	: AddDistinctGroups()
    // Desc	: Area 추가 후 재계산
    //--------------------------------------------------------------------
    private Int64 _AddDistinctGroups(CArea newArea)
    {
        // CREATE A NEW GROUP
        CAreaGroup newGroup = new CAreaGroup(newArea);
        Int64 newGroupID    = 0;

        // COLLECT (ADJACENT OR INTERSECTED) COMPLETED AREAS
        if (newArea.IsCompleted())
        {
            // FIND ALL INTERSECTING GROUPS AND MERGE INTO NEW GROUP
            foreach (var key in m_AreaGroupData.Keys.ToList())
            {
                if (m_AreaGroupData[key].IsMergeable(newArea))
                {
                    // GROUP 제거하기전에 임시변수
                    var removeGroup = m_AreaGroupData[key];

                    // GROUP 제거
                    m_AreaGroupData.Remove(key);
                    _PushUpdateID(key);

                    // GROUP에 AREA 들 추가 갱신
                    foreach (var a in removeGroup)
                    {
                        newGroup.AppendArea(a.Value);
                    }
                    if (newGroupID == 0) newGroupID = key;
                }
            }
        }

        // 키가 쓸데없이 커지지 않도록 재사용 
        if (newGroupID == 0) newGroupID = m_GroupIDSerial++;

        // GROUP에 속한 GUILD 정보들을 COLLECT 한다.
        newGroup.CollectGuildIDs(newGroupID);

        // 새로운 GROUP 추가
        m_AreaGroupData.Add(newGroupID, newGroup);
        _PushUpdateID(newGroupID);

        return newGroupID;
    }

    //--------------------------------------------------------------------
    // Code	: _PushUpdateID()
    // Desc	: Update Group
    //--------------------------------------------------------------------
    private void _PushUpdateID(Int64 GroupID)
    {
        // 갱신되어진 GROUP 기록
        if(!m_UpdateHash.Contains(GroupID))
            m_UpdateHash.Add(GroupID);
    }

    
}

