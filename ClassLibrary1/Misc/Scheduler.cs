using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ONI_MP.DebugTools;

namespace ONI_MP.Misc
{
    public class Scheduler
    {
        private static Scheduler _instance;
        private static readonly object _lock = new object();
        public static Scheduler Instance 
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new Scheduler();
                    }
                }
                return _instance;
            }
        }

        public enum Pipeline
        {
            ASYNC,
            NETWORK,
            MAIN,
        }

        private class Task
        {
            public int Id;
            public System.Action Callback;
            public double Interval;
            public int Repeat;
            public double LastCall;
            public bool StopFlag = false;
        }

        private int _taskIdCounter = 0;
        private readonly Dictionary<Scheduler.Pipeline, List<Scheduler.Task>> _pipelines = new Dictionary<Scheduler.Pipeline, List<Scheduler.Task>>();
        private readonly Dictionary<Scheduler.Pipeline, Thread> _threads = new Dictionary<Scheduler.Pipeline, Thread>();
        private readonly Dictionary<Scheduler.Pipeline, object> _mutexes = new Dictionary<Scheduler.Pipeline, object>();
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private volatile bool _running = true;

        public Scheduler()
        {
            foreach (Pipeline p in Enum.GetValues(typeof(Pipeline)))
            {
                _pipelines[p] = new List<Task>();
                _mutexes[p] = new object();
            }

            _stopwatch.Start();

            foreach (var pipeline in _pipelines.Keys)
            {
                var thread = new Thread(() => RunPipeline(pipeline));
                thread.IsBackground = true;
                _threads[pipeline] = thread;
                thread.Start();
            }
        }

        public void Shutdown()
        {
            _running = false;
            foreach (var thread in _threads.Values)
            {
                thread.Join();
            }
        }

        private void RunPipeline(Pipeline pipeline)
        {
            while (_running)
            {
                List<Task> snapshot;
                lock (_mutexes[pipeline])
                {
                    snapshot = new List<Task>(_pipelines[pipeline]); // copy to avoid conflict
                }

                double now = _stopwatch.Elapsed.TotalSeconds;

                foreach (var task in snapshot)
                {
                    if (task.StopFlag || task.Repeat == 0)
                    {
                        lock (_mutexes[pipeline])
                            _pipelines[pipeline].Remove(task);
                        continue;
                    }

                    if (now - task.LastCall >= task.Interval)
                    {
                        task.LastCall = now;

                        try { task.Callback.Invoke(); }
                        catch (Exception e) { Console.WriteLine($"Task error: {e}"); }

                        if (task.Repeat > 0)
                        {
                            task.Repeat--;
                            if (task.Repeat == 0)
                            {
                                lock (_mutexes[pipeline])
                                    _pipelines[pipeline].Remove(task);
                            }
                        }
                    }
                }

                //Thread.Sleep(10);
            }
        }

        private int AddTask(System.Action callback, Pipeline pipeline, double delay, int repeat)
        {
            var task = new Task
            {
                Id = Interlocked.Increment(ref _taskIdCounter),
                Callback = callback,
                Interval = delay,
                Repeat = repeat,
                LastCall = _stopwatch.Elapsed.TotalSeconds
            };

            lock (_mutexes[pipeline])
            {
                _pipelines[pipeline].Add(task);
            }

            return task.Id;
        }

        public int Schedule(System.Action callback, Pipeline pipeline = Pipeline.ASYNC, double delay = 0.0)
            => AddTask(callback, pipeline, delay, -1);

        public int Loop(System.Action callback, Pipeline pipeline = Pipeline.ASYNC, double delay = 0.0)
            => AddTask(callback, pipeline, delay, -1);

        public int Repeat(System.Action callback, Pipeline pipeline = Pipeline.ASYNC, double delay = 0.0, int repeatFor = 1)
            => AddTask(callback, pipeline, delay, repeatFor);

        public void Once(System.Action callback, Pipeline pipeline = Pipeline.ASYNC, double delay = 0.0)
            => AddTask(callback, pipeline, delay, 1);

        public void Stop(int taskId, Pipeline pipeline)
        {
            lock (_mutexes[pipeline])
            {
                foreach (var task in _pipelines[pipeline])
                {
                    if (task.Id == taskId)
                    {
                        task.StopFlag = true;
                        break;
                    }
                }
            }
        }

        public void ClearPipeline(Pipeline pipeline)
        {
            lock (_mutexes[pipeline])
            {
                _pipelines[pipeline].Clear();
            }
        }

        public void ClearAllPipelines()
        {
            foreach (var p in _pipelines.Keys)
            {
                ClearPipeline(p);
            }
        }

        // Optional test/demo
        public void Demo()
        {
            Once(() => DebugConsole.Log("This runs once after 2 seconds"), Pipeline.ASYNC, 2.0);

            Loop(() => DebugConsole.Log("This prints every second"), Pipeline.MAIN, 1.0);

            int taskId = Repeat(() => DebugConsole.Log("Repeating task running"), Pipeline.MAIN, 0.5, 5);

            Once(() =>
            {
                Stop(taskId, Pipeline.MAIN);
                DebugConsole.Log("Stopped repeating task");
            }, Pipeline.MAIN, 3.0);
        }
    }

}
