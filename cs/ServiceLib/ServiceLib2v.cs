using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Collections.Concurrent;
using System.Threading;

namespace ServiceLib
{
    class BackgroundService
    {
        private Mutex actionMutex;

        private List<CustomAction> actions;
        private TimeSpan interval;
        private CancellationTokenSource cts;

        // builders
        public BackgroundService()
        {
            actionMutex = new Mutex();

            actions = new List<CustomAction>();
            interval = TimeSpan.FromMilliseconds(1000);
            cts = new CancellationTokenSource();
        }
        public BackgroundService(TimeSpan interval)
        {
            actionMutex = new Mutex();

            actions = new List<CustomAction>();
            this.interval = interval;
            cts = new CancellationTokenSource();
        }

        //getters + setters
        public List<CustomAction> Actions
        {
            get { return actions; }
            set { actions = value; }
        }
        public TimeSpan Interval
        {
            get { return interval; }
            set { interval = value; }
        }
        public CancellationTokenSource Cts
        {
            get { return cts; }
            //set { cts = value; }
        }

        // commends
        public void Start()
        {
            Task.Run(() => ExecuteAsyncTasks(cts.Token));
        }
        public void Stop()
        {
            cts.Cancel();
        }
        public void Add(CustomAction action)
        {
            actions.Add(action);
        }
        public void Remove(CustomAction action)
        {
            actionMutex.WaitOne();
            try
            {
                action.Once = true;
            }
            finally { actionMutex.ReleaseMutex(); }
        }

        // async
        private async Task ExecuteAsyncTasks(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await StartAsyncTasks(interval);
            }
        }
        private async Task StartAsyncTasks(TimeSpan interval)
        {
            foreach (CustomAction action in actions)
            {
                // chack if the action is runing
                if (action.Runing)
                {
                    continue;
                }

                // start the action
                Task newTask = new Task(async () => await TheAsyncTask(action));
                newTask.Start();

                // remove the action if its once action
                if (action.Once)
                {
                    actionMutex.WaitOne();
                    try
                    {
                        actions.Remove(action);
                    }
                    finally { actionMutex.ReleaseMutex(); }
                }

            }

            // wait befor next update
            await Task.Delay(interval);
        }
        private async Task TheAsyncTask(CustomAction action)
        {
            Task newTask;
            TimeSpan newInterval;

            actionMutex.WaitOne();
            try
            {
                action.ToggelRuning();
                newTask = Task.Run(action.action);
                newInterval = action.Interval;
                //Console.WriteLine($"start at {DateTime.Now.Second} with: {actions[index].Runing}");
            }
            finally { actionMutex.ReleaseMutex(); }

            newTask.Wait();
            await Task.Delay(newInterval);

            actionMutex.WaitOne();
            try
            {
                action.ToggelRuning();
                //Console.WriteLine($"end at {DateTime.Now.Second} with: {actions[index].Runing}");
            }
            finally { actionMutex.ReleaseMutex(); }
        }

        // others
        public static void Run(Action action)
        {
            Task.Run(action);
        }
    }
    public class CustomAction
    {
        private bool once;

        private Action _action;
        private TimeSpan interval;
        private bool runing;

        // builders
        public CustomAction(Action action)
        {
            once = true;
            _action = action;
            interval = TimeSpan.FromSeconds(1);

            runing = false;
        }
        public CustomAction(Action action, TimeSpan interval)
        {
            once = false;
            _action = action;
            this.interval = interval;

            runing = false;
        }
        public CustomAction(CustomAction customAction)
        {
            once = customAction.Once;
            _action = customAction.action;
            interval = customAction.Interval;

            runing = customAction.Runing;
        }

        // geters + setters

        public bool Once
        {
            get
            {
                return once;
            }

            set
            {
                once = value;
            }
        }
        public TimeSpan Interval
        {
            get { return interval; }
            set { interval = value; }
        }
        public Action action
        {
            get { return _action; }
            set { _action = value; }
        }
        public bool Runing
        {
            get
            {
                return runing;
            }

            set
            {
                runing = value;
            }
        }

        // other
        public void ToggelRuning()
        {
            runing = !runing;
        }
    }
}
