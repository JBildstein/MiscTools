using System;
using System.Threading;
using System.Runtime.Remoting.Messaging;

namespace MiscTools
{
    /// <summary>
    /// A timer that fires an event at a given
    /// interval in a non-accumulating manner.
    /// <para>This is not a high-precision timer.</para>
    /// </summary>
    public class NATimer : IDisposable
    {
        #region Variables

        /// <summary>
        /// The event that fires when the time interval has expired
        /// </summary>
        public event EventHandler Tick;
        /// <summary>
        /// The interval for firing the <see cref="Tick"/> event in milliseconds
        /// </summary>
        public int Interval
        {
            get { return _Interval; }
            set { _Interval = Math.Max(value, 10); }
        }
        /// <summary>
        /// States if the timer is currently running
        /// </summary>
        public bool IsRunning
        {
            get { return _IsRunning; }
        }
        /// <summary>
        /// If true, the <see cref="Tick"/> event fires
        /// immediately after calling <see cref="Start"/>.
        /// <para>If false, the <see cref="Tick"/> event will
        /// fire after the first <see cref="Interval"/></para>
        /// </summary>
        public bool FireImmediately
        {
            get;
            set;
        }
        /// <summary>
        /// States if the <see cref="Tick"/> event is fired asynchronous or not.
        /// </summary>
        public bool AsyncEvent
        {
            get;
            set;
        }

        /// <summary>
        /// Field for the <see cref="Interval"/> property
        /// </summary>
        private int _Interval;
        /// <summary>
        /// Field for the <see cref="IsRunning"/> property
        /// </summary
        private bool _IsRunning;

        /// <summary>
        /// States if this instance is disposed or not
        /// </summary>
        protected bool IsDisposed;
        /// <summary>
        /// States if the timer has been canceled or not
        /// </summary>
        private bool IsCanceled;
        /// <summary>
        /// States if the <see cref="TimerThread"/> should be kept alive or not
        /// </summary>
        private bool KeepThreadAlive;
        /// <summary>
        /// The thread where the timer runs on
        /// </summary>
        private Thread TimerThread;
        /// <summary>
        /// The WaitHandle to wait for the start of the timer
        /// </summary>
        private AutoResetEvent PauseResetEvent = new AutoResetEvent(false);
        /// <summary>
        /// The WaitHandle to wait for the timer <see cref="Interval"/>
        /// </summary>
        private AutoResetEvent WaitEvent = new AutoResetEvent(false);

        #endregion

        #region Con/Destructor

        /// <summary>
        /// Creates a new instance of the <see cref="NATimer"/> class
        /// </summary>
        public NATimer()
            : this(1000, true)
        { }

        /// <summary>
        /// Creates a new instance of the <see cref="NATimer"/> class
        /// </summary>
        /// <param name="interval">The interval for firing the <see cref="Tick"/> event in milliseconds</param>
        public NATimer(int interval)
            : this(interval, true)
        { }

        /// <summary>
        /// Creates a new instance of the <see cref="NATimer"/> class
        /// </summary>
        /// <param name="interval">The interval for firing the <see cref="Tick"/> event in milliseconds</param>
        /// <param name="asyncEvent">True to fire the <see cref="Tick"/> event asynchronous, false for synchronous</param>
        public NATimer(int interval, bool asyncEvent)
        {
            Interval = interval;
            AsyncEvent = asyncEvent;
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~NATimer()
        {
            Dispose(false);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the timer
        /// </summary>
        /// <exception cref="ObjectDisposedException">This object has been disposed</exception>
        public void Start()
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(NATimer));

            //If the thread is not created yet or crashed, start it
            if (TimerThread == null || !TimerThread.IsAlive)
            {
                KeepThreadAlive = true;
                TimerThread = new Thread(TimerRoutine);
                TimerThread.Start();
            }

            if (!IsRunning)
            {
                IsCanceled = false;
                PauseResetEvent.Set();
            }
        }

        /// <summary>
        /// Stops the timer
        /// </summary>
        /// <exception cref="ObjectDisposedException">This object has been disposed</exception>
        public void Stop()
        {
            if (IsRunning && !IsDisposed) Cancel();
        }

        /// <summary>
        /// Releases all allocated resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Subroutines

        private void TimerRoutine()
        {
            try
            {
                while (KeepThreadAlive)
                {
                    //Wait for Start
                    PauseResetEvent.WaitOne();
                    if (!KeepThreadAlive) break;
                    _IsRunning = true;
                    //Store starting time
                    long start = DateTime.UtcNow.Ticks;

                    //If FireImmediately is set, fire the first event
                    if (FireImmediately) FireTick();

                    //Enter timer loop
                    while (!IsCanceled)
                    {
                        //Wait for the remaining time until the next interval
                        WaitEvent.WaitOne((int)(Interval - ((DateTime.UtcNow.Ticks - start) / 10000) % Interval));
                        //If not canceled, fire event
                        if (!IsCanceled) FireTick();
                    }

                    _IsRunning = false;
                }
            }
            catch { _IsRunning = false; }
        }

        private void FireTick()
        {
            EventHandler evt = Tick;
            if (evt != null)
            {
                if (AsyncEvent)
                {
                    Delegate[] eventListeners = evt.GetInvocationList();
                    for (int index = 0; index < eventListeners.Length; index++)
                    {
                        var handler = (eventListeners[index] as EventHandler);
                        if (handler != null) handler.BeginInvoke(this, EventArgs.Empty, EndAsyncEvent, null);
                    }
                }
                else evt(this, EventArgs.Empty);
            }
        }

        private void EndAsyncEvent(IAsyncResult iar)
        {
            AsyncResult result = iar as AsyncResult;
            if (result == null) return;
            EventHandler invokedMethod = result.AsyncDelegate as EventHandler;
            if (invokedMethod == null) return;
            try { invokedMethod.EndInvoke(iar); }
            catch { /*Not my problem*/ }
        }

        private void Cancel()
        {
            IsCanceled = true;
            WaitEvent.Set();
        }

        /// <summary>
        /// Releases all allocated resources
        /// </summary>
        /// <param name="managed">True if called from the <see cref="Dispose()"/> method,
        /// false if called from the finalizer</param>
        protected virtual void Dispose(bool managed)
        {
            if (!IsDisposed)
            {
                Cancel();

                KeepThreadAlive = false;
                PauseResetEvent.Set();
                if (TimerThread != null && TimerThread.IsAlive) TimerThread.Join();

                WaitEvent.Close();
                PauseResetEvent.Close();

                Tick = null;

                IsDisposed = true;
            }
        }

        #endregion
    }
}
