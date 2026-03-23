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
			Profiler.Scope();

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
			Profiler.Scope();

			SetAction(action);
			SetTimer(seconds);
		}
		public void SetTimer(int seconds)
		{
			Profiler.Scope();

			targetTime = System.DateTime.Now.AddSeconds(seconds);
		}
		public void SetAction(System.Action action)
		{
			Profiler.Scope();

			OnTimerEnd = action;
		}

		public void Abort()
		{
			Profiler.Scope();

			targetTime = System.DateTime.MinValue;
		}
	}
}
