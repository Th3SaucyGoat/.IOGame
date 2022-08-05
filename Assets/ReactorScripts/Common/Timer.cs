using System;
using System.Collections;
using System.Collections.Generic;


namespace Example
{
    public class Timer
    {
        private bool IsStopped = false;
        public bool Repeat = false;

        public float RemainingSeconds { get; private set; }
        private float SetDuration;
        public Action OnTimerEnd;
        public Timer(float duration, Action function, bool repeat)
        {
            SetDuration = duration;
            OnTimerEnd += function;
            Repeat = repeat;

        }

        public void Tick(float deltaTime)
        {
            if (IsStopped) { return; }
            if (RemainingSeconds == 0f) { return; }
            RemainingSeconds -= deltaTime;
            if (RemainingSeconds > 0f) { return; }
            RemainingSeconds = 0f;
            OnTimerEnd?.Invoke();
            if (Repeat == true)
            {
                Start(SetDuration);
            }
        }
        
        public void Start()
        {
            IsStopped = false;
            RemainingSeconds = SetDuration;
        }
        public void Start(float duration)
        {
            IsStopped = false;
            RemainingSeconds = duration;
        }

        public void Stop()
        {
            IsStopped = true;
        }
    }
}
