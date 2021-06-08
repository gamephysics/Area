#if UNITY_EDITOR
//#define SHOW_LOG_LOCAL
#endif

using System;
using System.Threading;
using System.Reflection;

using UnityEngine;

namespace CORE
{
	public abstract class TSingleton<TClass>
		where TClass : class
	{
#if UNITY_EDITOR && UNITY_2019_3_OR_NEWER
		/// <summary>
		/// 스크립트 캐싱정보 날리는 처리를 위한 특성
		/// : ScriptCachesCleaner 클래스 쪽 주석 참조
		/// </summary>
		[CleanupScriptCachesMethod]
		static void CleanupScriptCachesBeforePlayMode()
		{
			m_lzInstance = new Lazy<TClass>(() => CreateInstanceInternal());
		}
#endif
		private static readonly object _LOCK = new object();

		private static Lazy<TClass> m_lzInstance = new Lazy<TClass>(() => CreateInstanceInternal());

		/// <summary>
		/// 싱글턴 인스턴스
		/// - 인스턴스 호출 과정은 thread-safe 하지만, 리턴 이후는 외부에서 알아서 해야한다.
		/// </summary>
		public static TClass Instance => m_lzInstance.Value;

		/// <summary>
		/// 인스턴스가 유효한가?
		/// - 인스턴스 생성과정 없이 순수하게 인스턴스가 있냐 없냐를 판단
		/// </summary>
		public static bool ValidInstance => m_lzInstance.IsValueCreated;

		/// <summary>
		/// 싱글턴 인스턴스 명시적 생성
		/// - 싱글턴 등록 시점에 후처리가 필요할 경우 OnCreateSingleton() 참고
		/// - 당연히... OnCreateSingleton() 내에서 Singleton.Instance 또는 DestroyInstance() 를 호출는 만행은 하지 말 것
		/// </summary>
		public static void CreateInstance()
		{
			if (m_lzInstance.IsValueCreated)
				return;

			var _ = m_lzInstance.Value;
		}

		/// <summary>
		/// 싱글턴 인스턴스 명시적 해제
		/// - 변태적인 방식인데, 런타임에서 기존 싱글턴 정보를 날리고 새로 작업해야 하는 경우 이 함수를 사용
		/// - 싱글턴 해제 시점에 후처리가 필요할 경우 OnDestroySingleton() 참고
		/// - 당연히... OnDestroySingleton() 내에서 Singleton.Instance 를 호출하는 만행은 하지 말 것
		/// </summary>
		public static void DestroyInstance()
		{
			if (!m_lzInstance.IsValueCreated)
				return;

			//	일단 락 걸고
			lock (_LOCK)
			{
				//	이중 확인
				if (m_lzInstance.IsValueCreated)
				{
					var oldInst = m_lzInstance.Value;
					DestroyInstanceInternal(oldInst);
				}
			}
		}

#region 파생 클래스에서 구현할 수 있는 이벤트 함수

		/// <summary>
		/// 싱글턴용 인스턴스 생성됨
		/// - 아직 Instance 가 활성화되지는 않았으며 이 함수 탈출 후 targetInst 가 싱글턴 인스턴스가 된다.
		/// </summary>
		protected virtual void OnCreateSingleton() { }

		/// <summary>
		/// 싱글턴용 인스턴스 해제됨
		/// - 아직 Instance 가 활성화되지는 않았으며 이 함수 탈출 후 targetInst 가 싱글턴 인스턴스가 된다.
		/// </summary>
		protected virtual void OnDestroySingleton() { }

		#endregion


		#region 내부 처리 함수

		static TClass CreateInstanceInternal()
		{
			Type t = typeof(TClass);

			//	생성자 조건 검사
			//	- 파생 클래스에서 public 으로 선언된 생성자들은 존재하면 안된다.
			//	- 파생 클래스에서 protected/private 으로 선언된 매개변수 없는 생성자가 있어야 한다.

			var publicInfos = t.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
			if (publicInfos.Length > 0)
			{
				throw new Exception($"!--[{t.ToString()}.{nameof(CreateInstanceInternal)}()] shouldn't have any public constructor.");
			}
			var nonPublicInfo = t.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
			if (nonPublicInfo == null)
			{
				throw new Exception($"!--[{t.ToString()}.{nameof(CreateInstanceInternal)}()] must have private or protected non-param constructor.");
			}

			TClass newInst = default;
			try
			{
				newInst = nonPublicInfo.Invoke(null) as TClass;
				//	싱글턴 인스턴스 생성 알림
				if (newInst is TSingleton<TClass> inst)
					inst.OnCreateSingleton();
			}
			catch (Exception e)
			{
				throw new Exception($"!--[{t.ToString()}.{nameof(CreateInstanceInternal)}()] catched some exception.", e);
			}
			finally
			{
#if SHOW_LOG_LOCAL
				if(newInst != default)
					Debug.Log($"[{t.ToString()}.{nameof(CreateInstanceInternal)}()] Completed. (TID: {Thread.CurrentThread.ManagedThreadId}).");
				else
					Debug.LogError($"!--[{t.ToString()}.{nameof(CreateInstanceInternal)}()] No Instance. (TID: {Thread.CurrentThread.ManagedThreadId}).");
#endif
			}
			return newInst;
		}
		static void DestroyInstanceInternal(TClass oldInst)
		{
			Type t = typeof(TClass);

			try
			{
				//	싱글턴 인스턴스 해제 알림
				if (oldInst is TSingleton<TClass> inst)
					inst.OnDestroySingleton();
			}
			catch (Exception e)
			{
				throw new Exception($"!--[{t.ToString()}.{nameof(DestroyInstanceInternal)}()] catched some exception.", e);
			}
			finally
			{
				//	새로 할당하면 지워지는 것과 마찬가지
				m_lzInstance = new Lazy<TClass>(() => CreateInstanceInternal());
#if SHOW_LOG_LOCAL
				Debug.Log($"[{t.ToString()}.{nameof(DestroyInstanceInternal)}()] Completed. (TID: {Thread.CurrentThread.ManagedThreadId}).");
#endif
			}
		}

#endregion
	}
}
