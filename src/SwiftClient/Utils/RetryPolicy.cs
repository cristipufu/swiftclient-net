using System;
using System.Collections.Generic;
using System.Linq;
using SwiftClient.Extensions;
using System.Threading.Tasks;

namespace SwiftClient
{
    public class RetryPolicy
    {
        protected int _iRetryGlobal;
        protected int _nRetryGlobal;

        protected RetryPolicy()
        {
            _iRetryGlobal = 0;
        }

        public RetryPolicy WithCount(int retryCount)
        {
            if (retryCount < 1) throw new ArgumentOutOfRangeException("retryCount");

            _nRetryGlobal = retryCount;

            return this;
        }

        public bool Do(Func<bool> func)
        {
            if (_iRetryGlobal < _nRetryGlobal)
            {
                var success = func();

                if (success) return true;

                _iRetryGlobal++;

                return Do(func);
            }

            return false;
        }

        public async Task<bool> DoAsync(Func<Task<bool>> func)
        {
            if (_iRetryGlobal < _nRetryGlobal)
            {
                var success = await func();

                if (success) return true;

                _iRetryGlobal++;

                return await DoAsync(func);
            }

            return false;
        }

        public static RetryPolicy Create()
        {
            return new RetryPolicy().WithCount(1);
        }
    }

    public class RetryPolicy<T> : RetryPolicy
    {
        protected List<T> _steps;
        protected static object _syncLock = new object();
        protected int _iRetryStep;
        protected int _nRetryPerStep;

        protected RetryPolicy() : base()
        {
            _iRetryStep = 1;
        }

        public new RetryPolicy<T> WithCount(int retryCount)
        {
            if (retryCount < 1) throw new ArgumentOutOfRangeException("retryCount");

            _nRetryGlobal = retryCount;

            return this;
        }

        public RetryPolicy<T> WithSteps(List<T> steps)
        {
            if (steps == null || !steps.Any()) throw new ArgumentOutOfRangeException("steps");

            _steps = steps;

            return this;
        }

        public RetryPolicy<T> WithCountPerStep(int retryCount)
        {
            if (retryCount < 1) throw new ArgumentOutOfRangeException("retryCount");

            _nRetryPerStep = retryCount;

            return this;
        }

        public bool Do(Func<T, bool> func)
        {
            return Do(() =>
            {
                var retrier = RetryPolicy.Create().WithCount(_nRetryPerStep);

                var success = retrier.Do(() =>
                {
                    return func(_steps.FirstOrDefault());
                });

                if (success) return true;

                var count = _steps.Count();

                if (_iRetryStep < count)
                {
                    lock (_syncLock)
                    {
                        _steps.MoveFirstToLast();
                    }

                    _iRetryStep++;

                    return Do(func);
                }

                return false;
            });
        }

        public async Task<bool> DoAsync(Func<T, Task<bool>> func)
        {
            return await DoAsync(async () =>
            {
                var retrier = RetryPolicy.Create().WithCount(_nRetryPerStep);

                var success = await retrier.DoAsync(async () =>
                {
                    return await func(_steps.FirstOrDefault());
                });

                if (success) return true;

                var count = _steps.Count();

                if (_iRetryStep < count)
                {
                    lock (_syncLock)
                    {
                        _steps.MoveFirstToLast();
                    }

                    _iRetryStep++;

                    return await DoAsync(func);
                }

                return false;
            });
        }

        public List<T> GetSteps()
        {
            return _steps;
        }

        public new static RetryPolicy<T> Create()
        {
            return new RetryPolicy<T>().WithCount(1).WithCountPerStep(1);
        }
    }
}
