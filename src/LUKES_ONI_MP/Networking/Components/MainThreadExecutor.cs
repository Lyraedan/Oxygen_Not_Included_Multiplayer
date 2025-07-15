using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking.Components
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using ONI_MP.DebugTools;
    using UnityEngine;

    public class MainThreadExecutor : MonoBehaviour
    {

        public static MainThreadExecutor dispatcher;
        private List<Action> events = new List<Action>();

        private void Awake()
        {
            if (dispatcher == null)
                dispatcher = this;
            else
                Destroy(this);
        }

        private void Start()
        {
            StartCoroutine(Execute());
        }

        public void QueueEvent(bool condition, Action action) => events.Add(() => StartCoroutine(WaitAndExecute(condition, action)));

        public void QueueEvent(Action action) => events.Add(action);

        IEnumerator WaitAndExecute(bool condition, Action action)
        {
            // Wait for condition to be true
            yield return new WaitUntil(() => condition);
            action?.Invoke();
        }

        // I know that this is terrible... Too bad
        IEnumerator Execute()
        {
            yield return new WaitUntil(() => events.Count > 0);
            events[0]?.Invoke();
            DebugConsole.Log("[Main/Thread] Executor executing next event @ " + DateTime.Now.ToString("hh:mm:ss"));
            yield return new WaitForSeconds(0.5f);
            events.RemoveAt(0);
            StartCoroutine(Execute());
        }
    }
}
