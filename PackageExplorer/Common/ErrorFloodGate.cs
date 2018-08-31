using System;
using System.Collections.Generic;

namespace PackageExplorer
{
    public class ErrorFloodGate
    {
        private const double StopLoadingThreshold = 0.50;
        private const int SlidingExpirationInMinutes = 60;
        private const int MinFailuresCount = 5;
        private const int SecondsInOneTick = 5;
        private readonly DateTimeOffset _origin = DateTimeOffset.Now;
        private readonly Queue<int> _attempts = new Queue<int>();
        private readonly Queue<int> _failures = new Queue<int>();

        private DateTimeOffset _lastEvaluate = DateTimeOffset.Now;

        private bool _isOpen;
        public bool IsOpen
        {
            get
            {
                if (GetTicks(_lastEvaluate) > 1)
                {
                    var discardOlderThan1Hour = GetTicks(DateTimeOffset.Now.AddMinutes(-SlidingExpirationInMinutes));

                    ExpireOlderValues(_attempts, discardOlderThan1Hour);
                    ExpireOlderValues(_failures, discardOlderThan1Hour);

                    var attemptsCount = _attempts.Count;
                    var failuresCount = _failures.Count;
                    _isOpen = attemptsCount > 0 && failuresCount > MinFailuresCount && ((double)failuresCount / attemptsCount) > StopLoadingThreshold;
                    _lastEvaluate = DateTimeOffset.Now;
                }
                return _isOpen;
            }
        }

        private void ExpireOlderValues(Queue<int> q, int expirationOffsetInTicks)
        {
            while (q.Count > 0 && q.Peek() < expirationOffsetInTicks)
            {
                q.Dequeue();
            }
        }

        public void ReportAttempt()
        {
            var ticks = GetTicks(_origin);
            _attempts.Enqueue(ticks);
        }

        public void ReportError()
        {
            var ticks = GetTicks(_origin);
            _failures.Enqueue(ticks);
        }

        // Ticks here are of 5sec long
        private int GetTicks(DateTimeOffset origin)
        {
            return (int)((DateTimeOffset.Now - origin).TotalSeconds / SecondsInOneTick);
        }
    }
}
