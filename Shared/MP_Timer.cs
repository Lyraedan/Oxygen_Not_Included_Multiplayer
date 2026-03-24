using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Profiling;
using UnityEngine;

namespace Shared
{
	public class MP_Timer : MonoBehaviour
	{
		public static MP_Timer Instance
		{
			get
			{
				if(instance == null)
				{
					instance = Global.Instance.gameObject.AddOrGet<MP_Timer>();
				}
				return instance;
			}
		}

		private static MP_Timer? instance = null;

		System.DateTime targetTime = System.DateTime.MinValue;
		System.Action OnTimerEnd = null;
		public void Update()
		{
			using var _ = Profiler.Scope();

			if (targetTime == System.DateTime.MinValue)
			{
				return;
			}
			if (System.DateTime.Now > targetTime)
			{
				if (OnTimerEnd != null)
					OnTimerEnd();
				targetTime = System.DateTime.MinValue;
			}
		}

		public void StartDelayedAction(int seconds, System.Action action)
		{
			using var _ = Profiler.Scope();

			SetAction(action);
			SetTimer(seconds);
		}
		public void SetTimer(int seconds)
		{
			using var _ = Profiler.Scope();

			targetTime = System.DateTime.Now.AddSeconds(seconds);
		}
		public void SetAction(System.Action action)
		{
			using var _ = Profiler.Scope();

			OnTimerEnd = action;
		}

		public void Abort()
		{
			using var _ = Profiler.Scope();

			targetTime = System.DateTime.MinValue;
		}
	}
}
