﻿#if LESSTHAN_NET35

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Dynamic.Utils;
using Theraot.Collections;

namespace System.Dynamic
{
    /// <summary>
    ///     Represents the dynamic set index operation at the call site, providing the binding semantic and the details about
    ///     the operation.
    /// </summary>
    public abstract class SetIndexBinder : DynamicMetaObjectBinder
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SetIndexBinder" />.
        /// </summary>
        /// <param name="callInfo">The signature of the arguments at the call site.</param>
        protected SetIndexBinder(CallInfo callInfo)
        {
            ContractUtils.RequiresNotNull(callInfo, nameof(callInfo));
            CallInfo = callInfo;
        }

        /// <summary>
        ///     Gets the signature of the arguments at the call site.
        /// </summary>
        public CallInfo CallInfo { get; }

        /// <summary>
        ///     The result type of the operation.
        /// </summary>
        public sealed override Type ReturnType => typeof(object);

        /// <summary>
        ///     Always returns <c>true</c> because this is a standard <see cref="DynamicMetaObjectBinder" />.
        /// </summary>
        internal sealed override bool IsStandardBinder => true;

        /// <summary>
        ///     Performs the binding of the dynamic set index operation.
        /// </summary>
        /// <param name="target">The target of the dynamic set index operation.</param>
        /// <param name="args">An array of arguments of the dynamic set index operation.</param>
        /// <returns>The <see cref="DynamicMetaObject" /> representing the result of the binding.</returns>
        public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            ContractUtils.RequiresNotNull(target, nameof(target));
            ContractUtils.RequiresNotNull(args, nameof(args));
            ContractUtils.Requires(args.Length >= 2, nameof(args));

            var value = args[args.Length - 1];
            var indexes = args.RemoveLast();

            ContractUtils.RequiresNotNull(value, nameof(args));
            ContractUtils.RequiresNotNullItems(indexes, nameof(args));

            return target.BindSetIndex(this, indexes, value);
        }

        /// <summary>
        ///     Performs the binding of the dynamic set index operation if the target dynamic object cannot bind.
        /// </summary>
        /// <param name="target">The target of the dynamic set index operation.</param>
        /// <param name="indexes">The arguments of the dynamic set index operation.</param>
        /// <param name="value">The value to set to the collection.</param>
        /// <returns>The <see cref="DynamicMetaObject" /> representing the result of the binding.</returns>
        public DynamicMetaObject FallbackSetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject value)
        {
            return FallbackSetIndex(target, indexes, value, errorSuggestion: null);
        }

        /// <summary>
        ///     When overridden in the derived class, performs the binding of the dynamic set index operation if the target dynamic
        ///     object cannot bind.
        /// </summary>
        /// <param name="target">The target of the dynamic set index operation.</param>
        /// <param name="indexes">The arguments of the dynamic set index operation.</param>
        /// <param name="value">The value to set to the collection.</param>
        /// <param name="errorSuggestion">The binding result to use if binding fails, or null.</param>
        /// <returns>The <see cref="DynamicMetaObject" /> representing the result of the binding.</returns>
        public abstract DynamicMetaObject FallbackSetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject value, DynamicMetaObject? errorSuggestion);
    }
}

#endif