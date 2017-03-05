﻿using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt.TypeClasses;
using System.Diagnostics.Contracts;
using static LanguageExt.Prelude;
using System.Threading.Tasks;

namespace LanguageExt.ClassInstances
{
    public struct MTask<A> :
        Optional<Task<A>, A>,
        Monad<Task<A>, A>,
        BiFoldable<Task<A>, Unit, A>
    {
        public static readonly MTask<A> Inst = default(MTask<A>);

        [Pure]
        public Task<A> None => Task.Run<A>(() => raise<A>(new BottomException()));

        [Pure]
        public MB Bind<MONADB, MB, B>(Task<A> ma, Func<A, MB> f) where MONADB : struct, Monad<Unit, Unit, MB, B>
        {
            if (typeof(MB) == typeof(Task<B>) && typeof(MONADB) == typeof(MTask<B>))
            {
                // HACK: This is a hack to get around the type system for async
                //       A better solution is needed

                var mb = from a in ma
                         from b in (Task<B>)(object)f(a)
                         select b;

                return (MB)(object)mb;
            }
            else
            {
                // Synchronous type-safe version
                if (ma.IsFaulted) return default(MONADB).Fail(ma.Exception);
                return f(ma.Result);
            }
        }

        [Pure]
        public Task<A> Fail(object err) =>
            None;

        [Pure]
        public Task<A> Fail(Exception err = null) =>
            Task.Run(() => raise<A>(err ?? new BottomException()));

        [Pure]
        public async Task<A> Plus(Task<A> ma, Task<A> mb)
        {
            // Run in parallel
            for (int i = 0; i < 2; i++)
            {
                // Return first one that completes
                var completed = await Task.WhenAny(ma, mb);
                if (!completed.IsFaulted) return completed.Result;
            }
            throw new BottomException();
        }

        /// <summary>
        /// Monad return
        /// </summary>
        /// <typeparam name="A">Type of the bound monad value</typeparam>
        /// <param name="x">The bound monad value</param>
        /// <returns>Monad of A</returns>
        [Pure]
        public Task<A> Return(Func<Unit, A> f) =>
            Task.Run(() => f(unit));

        [Pure]
        public Task<A> Zero() => 
            None;

        [Pure]
        public bool IsNone(Task<A> ma) =>
            ma.IsFaulted;

        [Pure]
        public bool IsSome(Task<A> ma) =>
            !IsNone(ma);

        [Pure]
        public bool IsUnsafe(Task<A> ma) =>
            true;

        [Pure]
        public B Match<B>(Task<A> ma, Func<A, B> Some, Func<B> None)
        {
            if (ma.IsFaulted)
                return None();
            else
                return Some(ma.Result);
        }

        public Unit Match(Task<A> ma, Action<A> Some, Action None)
        {
            if (ma.IsFaulted) None(); else Some(ma.Result);
            return unit;
        }

        [Pure]
        public B MatchUnsafe<B>(Task<A> ma, Func<A, B> Some, Func<B> None)
        {
            if (ma.IsFaulted)
                return None();
            else
                return Some(ma.Result);
        }

        [Pure]
        public Func<Unit, S> Fold<S>(Task<A> ma, S state, Func<S, A, S> f) => _ =>
        {
            if (ma.IsFaulted) return state;
            return f(state, ma.Result);
        };

        [Pure]
        public Func<Unit, S> FoldBack<S>(Task<A> ma, S state, Func<S, A, S> f) => _ =>
        {
            if (ma.IsFaulted) return state;
            return f(state, ma.Result);
        };

        [Pure]
        public S BiFold<S>(Task<A> ma, S state, Func<S, Unit, S> fa, Func<S, A, S> fb)
        {
            if (ma.IsFaulted)
                return fa(state, unit);
            else
                return fb(state, ma.Result);
        }

        [Pure]
        public S BiFoldBack<S>(Task<A> ma, S state, Func<S, Unit, S> fa, Func<S, A, S> fb)
        {
            if (ma.IsFaulted)
                return fa(state, unit);
            else
                return fb(state, ma.Result);
        }

        [Pure]
        public Func<Unit, int> Count(Task<A> ma) => _ =>
            ma.IsFaulted
                ? 0
                : 1;

        [Pure]
        public Task<A> Some(A value) =>
            Task.FromResult(value);

        [Pure]
        public Task<A> Optional(A value) =>
            Task.FromResult(value);

        [Pure]
        public Task<A> Id(Func<Unit, Task<A>> ma) =>
            ma(unit);

        [Pure]
        public Task<A> BindReturn(Unit _, Task<A> mb) =>
            mb;

        [Pure]
        public Task<A> Return(A x) =>
            Return(_ => x);
    }
}