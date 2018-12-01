// Needed for NET40

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Theraot.Collections.ThreadSafe;
using Theraot.Core;
using Theraot.Threading;

namespace Theraot.Collections
{
    public sealed class Progressor<T> : IObservable<T>, IEnumerable<T>
    {
        private ProxyObservable<T> _proxy;
        private TryTake<T> _tryTake;
        private IDisposable _disposable;

        public Progressor(T[] wrapped)
        {
            if (wrapped == null)
            {
                throw new ArgumentNullException(nameof(wrapped));
            }
            var index = -1;
            _proxy = new ProxyObservable<T>();
            _tryTake = Take;
            bool Take(out T value)
            {
                value = default(T);
                var currentIndex = Interlocked.Increment(ref index);
                if (currentIndex >= wrapped.Length)
                {
                    return false;
                }

                value = wrapped[currentIndex];
                return true;
            }
        }

        public Progressor(IEnumerable<T> wrapped)
        {
            if (wrapped == null)
            {
                throw new ArgumentNullException(nameof(wrapped));
            }
            var enumerator = wrapped.GetEnumerator();
            _disposable = enumerator;
            _proxy = new ProxyObservable<T>();
            _tryTake = Take;
            bool Take(out T value)
            {
                // We need a lock, there is no way around it. IEnumerator is just awful. Use another overload if possible.
                var enumeratorCopy = enumerator;
                if (enumeratorCopy != null)
                {
                    lock (enumeratorCopy)
                    {
                        if (enumeratorCopy.MoveNext())
                        {
                            value = enumeratorCopy.Current;
                            return true;
                        }
                    }
                    Interlocked.Exchange(ref enumerator, null)?.Dispose();
                }
                value = default(T);
                return false;
            }
        }

        public Progressor(IObservable<T> wrapped)
        {
            var buffer = new SafeQueue<T>();
            var semaphore = new SemaphoreSlim(0);
            var source = new CancellationTokenSource();
            var subscription = wrapped.Subscribe
                (
                    new CustomObserver<T>(
                        onCompleted: source.Cancel,
                        onError: exception => source.Cancel(),
                        onNext: OnNext
                    )
                );
            _proxy = new ProxyObservable<T>();
            var tryTake = new TryTake<T>[] { null };
            tryTake[0] = Take;
            _tryTake = tryTake[0];
            void OnNext(T item)
            {
                buffer.Add(item);
                semaphore.Release();
            }
            bool Take(out T value)
            {
                if (source.IsCancellationRequested)
                {
                    if (Interlocked.CompareExchange(ref _tryTake, Replacement, tryTake[0]) == tryTake[0])
                    {
                        Interlocked.Exchange(ref subscription, null)?.Dispose();
                    }
                }
                else
                {
                    try
                    {
                        semaphore.Wait(source.Token);
                    }
                    catch (OperationCanceledException exception)
                    {
                        GC.KeepAlive(exception);
                    }
                }
                return Replacement(out value);
            }
            bool Replacement(out T value)
            {
                if (buffer.TryTake(out value))
                {
                    return true;
                }
                value = default(T);
                return false;
            }
        }

        public bool IsClosed => Volatile.Read(ref _tryTake) == null;

        public void Close()
        {
            Volatile.Write(ref _tryTake, null);
            var subscription = Interlocked.Exchange(ref _disposable, null);
            subscription?.Dispose();
            var proxy = Interlocked.Exchange(ref _proxy, null);
            proxy?.OnCompleted();
        }

        public IEnumerator<T> GetEnumerator()
        {
            while (true)
            {
                if (TryTake(out var item))
                {
                    yield return item;
                }
                else
                {
                    break;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var proxy = Volatile.Read(ref _proxy);
            if (proxy != null)
            {
                return proxy.Subscribe(observer);
            }
            observer.OnCompleted();
            return Disposable.Create(ActionHelper.GetNoopAction());
        }

        public bool TryTake(out T item)
        {
            var tryTake = Volatile.Read(ref _tryTake);
            var proxy = Volatile.Read(ref _proxy);
            if (tryTake != null)
            {
                if (tryTake.Invoke(out item))
                {
                    if (proxy != null)
                    {
                        proxy.OnNext(item);
                    }
                    else
                    {
                        Debugger.Break();
                    }
                    return true;
                }
                Close();
            }
            item = default(T);
            return false;
        }

        public IEnumerable<T> While(Predicate<T> condition)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }
            return WhileExtracted();
            IEnumerable<T> WhileExtracted()
            {
                while (true)
                {
                    var tryTake = Volatile.Read(ref _tryTake);
                    if (tryTake != null && tryTake(out var item) && condition(item))
                    {
                        yield return item;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public IEnumerable<T> While(Func<bool> condition)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }
            return WhileExtracted();
            IEnumerable<T> WhileExtracted()
            {
                while (true)
                {
                    var tryTake = Volatile.Read(ref _tryTake);
                    if (tryTake != null && tryTake(out var item) && condition())
                    {
                        yield return item;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
}