using System;
using System.Collections.Concurrent;
using System.Threading;

public class SerialDispather
{
    private object _apiLock = new object();
    private Thread _thread;
    private BlockingCollection<Action> _actionQueue = new BlockingCollection<Action>(100);
    private bool _requiresStop = false;
    public bool IsRunning { get; private set; } = false;

    public SerialDispather()
    {
    }

    ~SerialDispather()
    {
        Stop();
    }

    public void Start() {
        bool locked = false;
        if (!Monitor.IsEntered(_apiLock)) {
            Monitor.Enter(_apiLock);
            locked = true;
        }
        try {
            if (IsRunning) {
                return;
            }
            IsRunning = true;
            _requiresStop = false;
            Thread thread = new Thread(() => {
                while (!_requiresStop)
                {
                    foreach (var action in _actionQueue.GetConsumingEnumerable())
                    {
                        action.Invoke();
                        if (_requiresStop)
                        {
                            break;
                        }
                    }
                }
            });
            thread.Start();
            _thread = thread;
        }
        finally {
            if (locked) {
                Monitor.Exit(_apiLock);
            }
        }
    }

    public void Stop() {
        bool locked = false;
        if (!Monitor.IsEntered(_apiLock)) {
            Monitor.Enter(_apiLock);
            locked = true;
        }
        try {
            if (!IsRunning) {
                return;
            }
            Async(() => {
                _requiresStop = true;
            });
            _thread.Join();
            IsRunning = false;
        }
        finally {
            if (locked) {
                Monitor.Exit(_apiLock);
            }
        }
    }

    public void Async(Action action)
    {
        bool locked = false;
        if (Thread.CurrentThread.ManagedThreadId != _thread.ManagedThreadId && !Monitor.IsEntered(_apiLock)) {
            Monitor.Enter(_apiLock);
            locked = true;
        }
        try {
            if (!IsRunning) {
                return;
            }
            _actionQueue.Add(action);
        }
        finally {
            if (locked) {
                Monitor.Exit(_apiLock);
            }
        }
    }

    public void Sync(Action action)
    {
        bool locked = false;
        if (Thread.CurrentThread.ManagedThreadId != _thread.ManagedThreadId && !Monitor.IsEntered(_apiLock)) {
            Monitor.Enter(_apiLock);
            locked = true;
        }
        try {
            if (!IsRunning) {
                return;
            }
            if (Thread.CurrentThread.ManagedThreadId != _thread.ManagedThreadId)
            {
                _actionQueue.Add(action);
                var autoEvent = new AutoResetEvent(false);
                _actionQueue.Add(() => {
                    autoEvent.Set();
                });
                autoEvent.WaitOne();
            }
            else {
                action.Invoke();
            }
        }
        finally {
            if (locked) {
                Monitor.Exit(_apiLock);
            }
        }
    }
}
