#if NET20 || NET30 || NET35

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using LinqError = System.Linq.Expressions.Error;

namespace System.Dynamic.Utils
{
    /// <summary>
    /// Provides a wrapper around an IArgumentProvider which exposes the argument providers
    /// members out as an IList of Expression.  This is used to avoid allocating an array
    /// which needs to be stored inside of a ReadOnlyCollection.  Instead this type has
    /// the same amount of overhead as an array without duplicating the storage of the
    /// elements.  This ensures that internally we can avoid creating and copying arrays
    /// while users of the Expression trees also don't pay a size penalty for this internal
    /// optimization.  See IArgumentProvider for more general information on the Expression
    /// tree optimizations being used here.
    /// </summary>
    internal sealed class ListArgumentProvider : ListProvider<Expression>
    {
        private readonly IArgumentProvider _provider;

        internal ListArgumentProvider(IArgumentProvider provider, Expression arg0)
        {
            _provider = provider;
            First = arg0;
        }

        protected override int ElementCount => _provider.ArgumentCount;

        protected override Expression First { get; }

        protected override Expression GetElement(int index) => _provider.GetArgument(index);
    }

    internal abstract class ListProvider<T> : IList<T>
            where T : class
    {
        protected abstract int ElementCount { get; }
        protected abstract T First { get; }

        protected abstract T GetElement(int index);

        #region IList<T> Members

        public T this[int index]
        {
            get
            {
                if (index == 0)
                {
                    return First;
                }

                return GetElement(index);
            }
            set => throw ContractUtils.Unreachable;
        }

        public int IndexOf(T item)
        {
            if (First == item)
            {
                return 0;
            }

            for (int i = 1, n = ElementCount; i < n; i++)
            {
                if (GetElement(i) == item)
                {
                    return i;
                }
            }

            return -1;
        }

        public void Insert(int index, T item)
        {
            throw ContractUtils.Unreachable;
        }

        public void RemoveAt(int index)
        {
            throw ContractUtils.Unreachable;
        }

        #endregion IList<T> Members

        #region ICollection<T> Members

        public int Count => ElementCount;

        public bool IsReadOnly => true;

        public void Add(T item)
        {
            throw ContractUtils.Unreachable;
        }

        public void Clear()
        {
            throw ContractUtils.Unreachable;
        }

        public bool Contains(T item) => IndexOf(item) != -1;

        public void CopyTo(T[] array, int index)
        {
            ContractUtils.RequiresNotNull(array, nameof(array));
            if (index < 0)
            {
                throw LinqError.ArgumentOutOfRange(nameof(index));
            }

            int n = ElementCount;
            Debug.Assert(n > 0);
            if (index + n > array.Length)
            {
                throw new ArgumentException();
            }

            array[index++] = First;
            for (int i = 1; i < n; i++)
            {
                array[index++] = GetElement(i);
            }
        }

        public bool Remove(T item)
        {
            throw ContractUtils.Unreachable;
        }

        #endregion ICollection<T> Members

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            yield return First;

            for (int i = 1, n = ElementCount; i < n; i++)
            {
                yield return GetElement(i);
            }
        }

        #endregion IEnumerable<T> Members

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion IEnumerable Members
    }
}

#endif