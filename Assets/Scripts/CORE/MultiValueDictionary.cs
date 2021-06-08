#region 헤더 코멘트
/// <license>
///		<copyright> A_Library (for C# & Unity) </copyright>
///		<author> 조현준(actdoll.2001~/ 2013~2018). </author>
///		<studio> CF팀.GNSS팀.POC팀.ONE팀.이데아개발실.스마트개발실. </studio>
///		<company> NDREAM - FunGrove / Netmarble NPark (AniPark) </company>
///		<summary> 자유로이 사용하셔도 좋습니다만, 원 저작자 출처 정도는 남겨주는 센스~ </summary>
/// </license>
#endregion// 헤더 코멘트

using System;
using System.Collections;
using System.Collections.Generic;

//	<see cref="https://stackoverflow.com/questions/380595/multimap-in-net?answertab=active#tab-top"/>
namespace CORE
{
	/// <summary>
	/// 복수값 딕셔너리
	/// - 키 값이 중복되어 아이템을 적재할 수 있는 컨테이너
	/// - 아이템 컨테이너 내 값은 중복될 수 있다. (즉, 동일한 Key-Value 페어가 계속 삽입될 수 있다.)
	/// - C++ 의 multimap 과 (로직은 틀리지만) 동일한 형태이다.
	/// </summary>
	/// <typeparam name="TKey">키 형식</typeparam>
	/// <typeparam name="TValue">값 형식</typeparam>
	public class MultiValueDictionary<TKey, TValue> : Dictionary<TKey, List<TValue>>
	{
		static readonly List<TValue> EmptyValues = new List<TValue>();
		List<TKey> tmp_keys = new List<TKey>(16);

		public MultiValueDictionary()
			: base()
		{
		}
		public MultiValueDictionary(IDictionary<TKey, List<TValue>> dictionary)
			: base(dictionary)
		{
		}
		public MultiValueDictionary(IDictionary<TKey, List<TValue>> dictionary, IEqualityComparer<TKey> keycomparer)
			: base(dictionary, keycomparer)
		{
		}
		public MultiValueDictionary(IEqualityComparer<TKey> keycomparer)
			: base(keycomparer)
		{
		}
		public MultiValueDictionary(int capacity)
			: base(capacity)
		{
		}
		public MultiValueDictionary(int capacity, IEqualityComparer<TKey> keycomparer)
			: base(capacity, keycomparer)
		{
		}


		/// <summary>
		/// 저장소 내 모든 아이템에 대한 열거자 확보
		/// - Key/Value 형식
		/// </summary>
		/// <example>
		/// foreach(var pair in repo.Repository)
		/// {
		///		TKey key = pair.Key;
		///		TValue value = pair.Value;
		///		...
		/// }
		/// </example>
		public RepoEnumerator.Enumerable Repository => new RepoEnumerator.Enumerable(this);


		/// <summary>
		/// 등록된 아이템 총 갯수
		/// </summary>
		public int ValueCount
		{
			get
			{
				int count = 0;
				foreach(var pair in this)
					count += pair.Value?.Count ?? 0;
				return count;
			}
		}


		/// <summary>
		/// 대상 키로 아이템 추가
		/// - 아이템 중복이 가능
		/// </summary>
		public void Add(in TKey key, in TValue value)
		{
			if(!this.TryGetValue(key, out var container))
			{
				container = MakeValueContainer();
				base.Add(key, container);
			}
			else if(container == null)
			{
				container = MakeValueContainer();
				base[key] = container;
			}
			container.Add(value);
		}


		/// <summary>
		/// 대상 키로 아이템배열 추가
		/// - 기존 아이템배열에 중복되는 아이템은 제외하고 추가됨
		/// </summary>
		public void AddRange(in TKey key, IEnumerable<TValue> collection)
		{
			if(!this.TryGetValue(key, out var container))
			{
				base.Add(key, MakeValueContainer(collection));
				return;
			}
			else if(container == null)
			{
				base[key] = MakeValueContainer(collection);
				return;
			}
			container.AddRange(collection);
		}


		/// <summary>
		/// 대상 키의 해당 아이템이 존재하는가?
		/// </summary>
		public bool ContainsValue(in TKey key, in TValue value)
		{
			bool toReturn = false;
			if(this.TryGetValue(key, out var container) && container != null)
			{
				toReturn = container.Contains(value);
			}
			return toReturn;
		}


		/// <summary>
		/// 해당 아이템이 존재하는가?
		/// </summary>
		public bool ContainsValue(in TValue value)
		{
			foreach(var pair in this)
			{
				if(ContainsValue(pair.Key, value))
					return true;
			}
			return false;
		}


		/// <summary>
		/// 대상 키의 해당 아이템들 중 조건에 만족하는 놈이 있는가?
		/// </summary>
		public TValue FindValue(in TKey key, Predicate<TValue> pred)
		{
			if(pred == null)
				return default;

			if(this.TryGetValue(key, out var container) && container != null)
			{
				return container.Find(pred);
			}
			return default;
		}


		/// <summary>
		/// 대상 키의 해당 아이템을 삭제
		/// - 똑같은 아이템이 있을 경우 제일 먼저 발견된 것이 삭제됨
		/// - 삭제 후 아이템이 없으면 키 컨테이너 제거됨
		/// </summary>
		/// <returns>찾아서 지웠다면 true. 못찾았으면 false</returns>
		public bool Remove(in TKey key, in TValue value)
		{
			if(this.TryGetValue(key, out var container) && container != null)
			{
				var ret = container.Remove(value);
				if(container.Count <= 0)
				{
					this.Remove(key);
				}
				return ret;
			}
			return false;
		}


		/// <summary>
		/// 대상 키의 해당 조건에 맞는 아이템들을 모두 삭제
		/// - 삭제 후 아이템이 없으면 키 컨테이너 제거됨
		/// </summary>
		/// <returns>지운 항목의 갯수</returns>
		public int RemoveAll(in TKey key, Predicate<TValue> pred)
		{
			if(pred == null)
				return 0;

			if(this.TryGetValue(key, out var container) && container != null)
			{
				var ret = container.RemoveAll(pred);
				if(container.Count <= 0)
				{
					this.Remove(key);
				}
				return ret;
			}
			return 0;
		}


		/// <summary>
		/// 모든 키의 해당 조건에 맞는 아이템들을 모두 삭제
		/// - 삭제 후 아이템이 없으면 키 컨테이너 제거됨
		/// </summary>
		/// <returns>지운 항목의 갯수</returns>
		public int RemoveAll(Predicate<TValue> pred)
		{
			if(pred == null)
				return 0;

			var ret = 0;
			foreach(var pair in this)
			{
				ret += pair.Value.RemoveAll(pred);
				if(pair.Value.Count <= 0)
					tmp_keys.Add(pair.Key);
			}

			foreach(var e in tmp_keys)
				this.Remove(e);

			tmp_keys.Clear();

			return ret;
		}


		/// <summary>
		/// 대상 컨테이너의 모든 아이템 정보를 이 컨테이너로 병합
		/// - 아이템들의 중복검사 안함. 
		/// - 대상 컨테이너는 원본 그대로 유지
		/// </summary>
		public void Merge(MultiValueDictionary<TKey, TValue> toMergeWith)
		{
			if(toMergeWith == null)
			{
				return;
			}

			foreach(var pair in toMergeWith)
			{
				this.AddRange(pair.Key, pair.Value);
			}
		}


		/// <summary>
		/// 해당 키의 아이템 갯수 리턴
		/// </summary>
		/// <param name="key"></param>
		public int GetValuesCount(in TKey key)
		{
			return (this.TryGetValue(key, out var container) && container != null) ? container.Count : 0;
		}


		/// <summary>
		/// 대상 키의 원본 아이템 컨테이너 확보
		/// - for 문 등의 순환처리 시에는 가급적 GetValues()/GetRepository() 를 사용할 것을 권장함.
		/// </summary>
		/// <param name="key">키 값</param>
		/// <param name="returnEmptySet">아이템 컨테이너가 없을 경우 true 이면 빈 컨테이너 생성해서 리턴. false 면 null 리턴</param>
		/// <returns>
		/// 키의 아이템 컨테이너 인스턴스. 키가 없으면 returnEmptySet 의 상태에 따라 빈 컨테이너 또는 null 리턴.
		/// </returns>
		public List<TValue> GetValueContainer(in TKey key, bool returnEmptySet = false)
		{
			if((!this.TryGetValue(key, out var container) || container == null) && returnEmptySet)
			{
				container = MakeValueContainer();
			}
			return container;
		}


		/// <summary>
		/// 대상 키의 아이템 컨테이너에 대한 열거자 확보
		/// - Value 형식
		/// </summary>
		/// <param name="key">키 값</param>
		/// <example>
		/// foreach(TValue value in repo.GetValues(key))
		/// {
		///		...
		/// }
		/// </example>
		public ValueEnumerable GetValues(in TKey key)
		{
			return (this.TryGetValue(key, out var container) && container != null) ? 
				new ValueEnumerable(container) : default;
		}


		/// <summary>
		/// 대상 키의 아이템에 대한 열거자 확보
		/// - Key/Value 형식
		/// </summary>
		/// <example>
		/// foreach(var pair in repo.GetRepository(key))
		/// {
		///		TKey key = pair.Key;
		///		TValue value = pair.Value;
		///		...
		/// }
		/// </example>
		public RepoEnumerator.Enumerable GetRepository(in TKey key)
		{
			return (this.TryGetValue(key, out var container) && container != null) ?
				new RepoEnumerator.Enumerable(key, container) : default;
		}

		//--------------------------------------------------------------------

		//	밸류 컨테이너 생성기
		List<TValue> MakeValueContainer(IEnumerable<TValue> collection = null)
		{
			List<TValue> container;
			if(collection != null)
			{
				container = new List<TValue>(collection);
			}
			else
			{
				container = new List<TValue>(8);
			}
			return container;
		}

		//============================================================================================================================================================

		#region 열거자 구현부

		//	열거가능 클래스 - Value
		public struct ValueEnumerable : IEnumerable<TValue>, IEnumerable
		{
			List<TValue> m_repo;

			public ValueEnumerable(List<TValue> list) => m_repo = list;

			/// <summary>
			/// 기본 열거자
			/// </summary>
			public List<TValue>.Enumerator GetEnumerator() => (m_repo ?? EmptyValues).GetEnumerator();

			IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => this.GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		}

		//	열거자 - Key/Value
		public struct RepoEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IEnumerator, IDisposable
		{
			static readonly MultiValueDictionary<TKey, TValue> EmptyRepo = new MultiValueDictionary<TKey, TValue>();

			//	열거가능 클래스 - Key/Value
			public struct Enumerable : IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
			{
				MultiValueDictionary<TKey, TValue> m_repo;
				List<TValue> m_list;
				TKey m_key;

				public Enumerable(MultiValueDictionary<TKey, TValue> repo)
				{
					m_repo = repo;
					m_list = null;
					m_key = default;
				}

				public Enumerable(in TKey key, List<TValue> list)
				{
					m_repo = null;
					m_list = list ?? EmptyValues;
					m_key = key;
				}

				/// <summary>
				/// 기본 열거자
				/// </summary>
				public RepoEnumerator GetEnumerator() => m_repo != null ? 
					new RepoEnumerator(m_repo ?? EmptyRepo) :			// 전체순환
					new RepoEnumerator(m_key, m_list ?? EmptyValues);	// 범위순환

				IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => this.GetEnumerator();
				IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
			}
			//--------------------------------------------------------------------

			Enumerator m_enumRepo;
			List<TValue>.Enumerator m_enumList;
			TKey m_key;
			bool m_range;
			//
			public KeyValuePair<TKey, TValue> Current => m_range ?
				new KeyValuePair<TKey, TValue>(m_key, m_enumList.Current) :
				new KeyValuePair<TKey, TValue>(m_enumRepo.Current.Key, m_enumList.Current);

			RepoEnumerator(Dictionary<TKey, List<TValue>> repo)
			{
				m_enumRepo = repo.GetEnumerator();
				m_enumList = EmptyValues.GetEnumerator();
				m_key = default;
				m_range = false;
			}
			RepoEnumerator(TKey key, List<TValue> list)
			{
				m_enumRepo = EmptyRepo.GetEnumerator();
				m_enumList = list.GetEnumerator();
				m_key = key;
				m_range = true;
			}

			public void Dispose()
			{
				m_enumRepo.Dispose();
				m_enumList.Dispose();
			}

			public bool MoveNext()
			{
				while(true)
				{
					if(MoveNextValue())
						return true;

					if(MoveNextKey())
						continue;

					break;
				}
				return false;
			}
			bool MoveNextValue() => m_enumList.MoveNext();
			bool MoveNextKey()
			{
				if(m_range)
					return false;

				while(m_enumRepo.MoveNext())
				{
					//	체크된 현재 열거자의 아이템 체크 (비어 있으면 다음 것을 체크한다.
					var list = m_enumRepo.Current.Value;
					if(list != null && list.Count > 0)
					{
						m_enumList = list.GetEnumerator();
						return true;
					}
				}
				m_enumList = EmptyValues.GetEnumerator();
				return false;   // 더 이상 넘어갈 것이 없다.
			}

			void IEnumerator.Reset()
			{
				(m_enumRepo as IEnumerator).Reset();
				if(m_range)
					(m_enumList as IEnumerator).Reset();
				else
					m_enumList = EmptyValues.GetEnumerator();
			}
			object IEnumerator.Current => this.Current;
		}
		#endregion
	}
}