#region 헤더 코멘트
///	<license>
///		<company> FunGrove/ NDREAM </company>
///		<writer> 조현준 (actdoll@ndream.com) </writer>
///		<title> ScriptCacheCleaner </title>
///		<summary>
///			유니티 에디터용 스크립트 캐시 클리너
///		</summary>
///	</license>
#endregion// 헤더 코멘트

#if UNITY_EDITOR && UNITY_2019_3_OR_NEWER
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

namespace CORE
{
	/// <summary>
	/// 스크립트 캐싱정보 날리기 (Editor only/ Unity 2019.3 이상)
	/// - Project Settings -> Editor -> Enter Play Mode Options : Enable & Reload Domain : Disable
	/// - 스크립트 캐싱 옵션 상에서 플레이모드 재진입 시 클래스 내 스태틱 변수를 초기화해줘야 하는 경우에 사용
	/// - CleanupScriptCachesMethodAttribute 와 같이 사용
	/// 
	/// : 유니티 에디터 스크립트 캐싱 기능을 사용하면 플레이 종료 후 재시작해도 스태틱 변수와 이벤트 정보가 살아있다
	/// : 이 경우 구동 과정에서 플래그 역할을 하는 스태틱 정보가 댕글링 상태이므로 문제를 야기할 수 있다. (ex> 싱글턴)
	/// : 때문에 플레이모드 재진입 시 각 클래스 내 스태틱 초기화 처리를 일괄적으로 해 줘야 할 필요가 있는데 이 클래스가 그 역할을 처리한다.
	/// </summary>
	/// <see cref="https://docs.unity3d.com/Manual/DomainReloading.html"/>
	public static class ScriptCachesCleaner
	{
		// 스태틱 변수 정리 시 사용할 스태틱 함수명 명시
		public const string METHOD_NAME_STATIC_CLEANUP = "CleanupScriptCachesBeforePlayMode";

		/// <summary>
		/// 에디터 상 플레이모드 개시 때마다 자동 호출됨
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		static void Cleanup()
		{
			string funcName = $"{nameof(ScriptCachesCleaner)}.{nameof(Cleanup)}()";
			Debug.Log($"<color=white>[{funcName}] Execute...</color>");
			var startTime = DateTimeOffset.UtcNow;

			//	어셈블리에 적재된 모든 타입을 받고
			HashSet<MethodInfo> methods = new HashSet<MethodInfo>();
			foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
			{
				//	클래스나 구조체에서...
				if (t.IsClass || (t.IsValueType && !(t.IsPrimitive || t.IsEnum)))
				{
					if (!IsAllowableType(t))
						continue;

					//	제너릭 선언된 것들은 찾지 말고
					if (t.IsGenericType || t.IsGenericTypeDefinition)
						continue;

					//	자기부터 추적
					SearchStaticMethodsForCleanup(t.BaseType, methods);
				}
			}

			//	메서드 찾은 게 없으면 GG
			if (methods.Count <= 0)
			{
				Debug.Log($"<color=white>[{funcName}] No method found.</color>");
				return;
			}

			//	처리 수행
			var localstr = new StringBuilder(32 * methods.Count);
			localstr.Append($"------ {funcName} Cleanup...").AppendLine();
			foreach (var m in methods)
			{
				m.Invoke(null, null);
				localstr.Append($" {m.ReflectedType?.ToString() ?? "(Global)"}.{m.Name}()").AppendLine();
			}
			var elapsedTime = DateTimeOffset.UtcNow - startTime;
			localstr.Append($"------ {funcName} Finished.");
			Debug.Log(localstr.ToString());

			//	결과 출력
			Debug.Log($"<color=white>[{funcName}] -- Work Complete --  Total {methods.Count} method(s) called. Elapsed {elapsedTime.TotalSeconds.ToString("#,0.###")} second(s).</color>");

			bool IsAllowableType(Type target)
			{
				if (target == null || target.IsAssignableFrom(typeof(System.Object)))   // 시스템 오브젝트 타입이면 끊어
					return false;
				if (!string.IsNullOrEmpty(target.FullName))
				{
					if (target.FullName.StartsWith("System.", StringComparison.InvariantCulture) ||
						target.FullName.StartsWith("UnityEngine.", StringComparison.InvariantCulture))
						return false;
				}
				return true;
			}

			void SearchStaticMethodsForCleanup(Type target, HashSet<MethodInfo> methodList)
			{
				if (!IsAllowableType(target))
					return;

				//	비공개 스태틱 함수 중 코드에서 직접 선언된 것만 찾는다.
				const BindingFlags flag = BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.NonPublic;

				var mtds = target.GetMethods(flag);
				if ((mtds?.Length ?? 0) > 0)
				{
					foreach (var m in mtds)
					{
						//	생성자/소멸자/속성 등은 안받아
						if (m.IsSpecialName)
							continue;

						//	클린업 전용 함수 찾음
						if (m?.IsDefined(typeof(CleanupScriptCachesMethodAttribute)) ?? false)
							methodList.Add(m);
					}
				}

				//	그 다음 부모계층
				SearchStaticMethodsForCleanup(target.BaseType, methodList);
			}
		}
	}

	/// <summary>
	/// 스크립트 캐싱정보 날리는 처리를 위한 특성
	/// - 에디터에서 플레이 모드 진입 시 스태틱 정보를 날리는 처리가 들어있는 함수를 호출하고 싶을 때 사용.
	/// : 호출될 함수는 함수명을 METHOD_NAME_STATIC_CLEANUP 에 정의된 대로 맞추는 것을 권장
	/// : 호출될 함수는 파라미터가 없는 static private void 타입으로 지정
	/// : 클래스 상속 시 파생된 클래스에 스태틱 변수가 있다면 똑같이 추가 (상위 클래스의 메서드 호출이 안됨)
	/// : 호출 함수에는 구동 시점에 반드시 초기화가 되어있어야 하는 것들만 등록하면 됨
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class CleanupScriptCachesMethodAttribute : Attribute
	{
	}
}
#endif