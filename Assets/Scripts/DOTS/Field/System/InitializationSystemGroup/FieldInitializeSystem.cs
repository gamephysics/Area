using System;
using UnityEngine;
using Unity.Entities;

/// <summary>
/// !!! 시스템 최초 기동 위치 !!!
/// </summary>
[UpdateInGroup(typeof(FieldInitializeSystemGroup))]
public partial class FieldInitializeSystem : SystemBase
{
    // 모바일에서 크래쉬 발생하여 DOTS 내부에서 하는 부분을 디파인 정의로 초기화 변경처리
#if UNITY_DISABLE_AUTOMATIC_SYSTEM_BOOTSTRAP_RUNTIME_WORLD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        DefaultWorldInitialization.Initialize("Default World", false);
        // GameObjectSceneUtility.AddGameObjectSceneReferences();
    }
#endif
    protected override void OnUpdate()
    {

    }
}