using System;
using System.Collections.Generic;
using UnityEngine;

using Unity.Mathematics;

using CORE;

public class Sample : TSingleton<Sample>
{
    private int m_SampleState = 0;

    private Sample()
    {
        m_SampleState = 0;
    }

    //==================================================================================================
    // SAMPLE METHOD
    //==================================================================================================    
    public void UpdateSample()
    {
        switch (m_SampleState)
        {
            case 0:
                CreateAreaRegion();
                m_SampleState++;
                break;
            case 1:
                DestroyAreaRegion();
                m_SampleState++;
                break;
        }
    }

    private void CreateAreaRegion()
    {
        //====================================================================
        // 연맹 영역 추가
        //====================================================================
        CArea   areaData;
        Int64   GroupID = 0;
        Int64   guildID = 100;

        // 100 번 GUILD
        areaData = new CArea(guildID, new int2(25,  3), 1, 6, EnumAreaStatus.Completed, EnumAreaOwn.Ally, 0);
        GroupID = CAreaRegion.Instance.Add(ref areaData);

        areaData = new CArea(guildID, new int2(21,  6), 1, 2, EnumAreaStatus.Completed, EnumAreaOwn.Ally, 0);
        GroupID = CAreaRegion.Instance.Add(ref areaData);

        areaData = new CArea(guildID, new int2(29,  6), 1, 2, EnumAreaStatus.Completed, EnumAreaOwn.Ally, 0);
        GroupID = CAreaRegion.Instance.Add(ref areaData);

        areaData = new CArea(guildID, new int2(25, 10), 1, 4, EnumAreaStatus.Completed, EnumAreaOwn.Ally, 0);
        GroupID = CAreaRegion.Instance.Add(ref areaData);


        Debug.Log("[ " + UnityEngine.Time.frameCount + " ]" + " CREATED GroupID " + GroupID);

        // 200 번 GUILD
        guildID = 200;
        areaData = new CArea(guildID, new int2(24, 34), 3, 8, EnumAreaStatus.Completed, EnumAreaOwn.Enemy, 0);
        GroupID = CAreaRegion.Instance.Add(ref areaData);

        Debug.Log("[ " + UnityEngine.Time.frameCount + " ]" + " CREATED GroupID " + GroupID);

        //====================================================================
        // 필드 진출 라인 추가
        //====================================================================
        var fleetData = new CFleet(1, EnumFleetTrait.Joint, EnumFleetRelation.MyFleet, 10f, new List<Vector2> { new Vector2(0f, 0f), new Vector2(3f, 4f), new Vector2(6f, 20f) });
        CFleetTrail.Instance.Add(ref fleetData);
    }
    private void DestroyAreaRegion()
    {
        //====================================================================
        // 연맹 영역 제거
        //====================================================================
        var GroupID = CAreaRegion.Instance.Remove(new int2(24, 34));
        Debug.Log("[ " + UnityEngine.Time.frameCount + " ]" + " REMOVED GroupID " + GroupID);
    }
}
