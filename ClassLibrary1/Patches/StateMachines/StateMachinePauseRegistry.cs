using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Patches.StateMachines
{
    public static class StateMachinePauseRegistry
    {
        private static readonly ConditionalWeakTable<StateMachine.Instance, PauseData> table = new ConditionalWeakTable<StateMachine.Instance, PauseData>();

        public class PauseData
        {
            public bool Paused;
        }

        public static bool IsPaused(StateMachine.Instance smi) => table.TryGetValue(smi, out var data) && data.Paused;

        public static void SetPaused(StateMachine.Instance smi, bool paused)
        {
            var data = table.GetOrCreateValue(smi);
            data.Paused = paused;
        }

        public static void PauseSMI(this StateMachine.Instance smi)
        {
            SetPaused(smi, true);
        }

        public static void ResumeSMI(this StateMachine.Instance smi)
        {
            SetPaused(smi, false);
        }

        public static bool IsSMIPaused(this StateMachine.Instance smi)
        {
            return IsPaused(smi);
        }
    }
}
