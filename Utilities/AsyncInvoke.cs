using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SeamlessClient.Utilities
{
    public static class AsyncInvoke
    {
        public static Task InvokeAsync(Action action, [CallerMemberName] string caller = "SeamlessClient")
        {


            //Jimm thank you. This is the best
            var ctx = new TaskCompletionSource<object>();
            MySandboxGame.Static.Invoke(() =>
            {
                try
                {
                    action.Invoke();
                    ctx.SetResult(null);
                    ctx.Task.ContinueWith(task => task.Dispose());
                }
                catch (Exception e)
                {
                    ctx.SetException(e);
                }

            }, caller);
            return ctx.Task;
        }

        public static Task<T> InvokeAsync<T>(Func<T> action, [CallerMemberName] string caller = "SeamlessClient")
        {


            //Jimm thank you. This is the best
            var ctx = new TaskCompletionSource<T>();
            MySandboxGame.Static.Invoke(() =>
            {
                try
                {
                    ctx.SetResult(action.Invoke());
                    ctx.Task.ContinueWith(task => task.Dispose());
                }
                catch (Exception e)
                {
                    ctx.SetException(e);
                }

            }, caller);
            return ctx.Task;
        }

        public static Task<T2> InvokeAsync<T1, T2>(Func<T1, T2> action, T1 arg, [CallerMemberName] string caller = "SeamlessClient")
        {
            //Jimm thank you. This is the best
            var ctx = new TaskCompletionSource<T2>();
            MySandboxGame.Static.Invoke(() =>
            {
                try
                {
                    ctx.SetResult(action.Invoke(arg));
                    ctx.Task.ContinueWith(task => task.Dispose());
                }
                catch (Exception e)
                {
                    ctx.SetException(e);
                }

            }, caller);
            return ctx.Task;
        }

        public static Task<T3> InvokeAsync<T1, T2, T3>(Func<T1, T2, T3> action, T1 arg, T2 arg2, [CallerMemberName] string caller = "SeamlessClient")
        {
            //Jimm thank you. This is the best
            var ctx = new TaskCompletionSource<T3>();
            MySandboxGame.Static.Invoke(() =>
            {
                try
                {
                    ctx.SetResult(action.Invoke(arg, arg2));
                    ctx.Task.ContinueWith(task => task.Dispose());
                }
                catch (Exception e)
                {
                    ctx.SetException(e);
                }

            }, caller);
            return ctx.Task;
        }

        public static Task<T4> InvokeAsync<T1, T2, T3, T4>(Func<T1, T2, T3, T4> action, T1 arg, T2 arg2, T3 arg3, [CallerMemberName] string caller = "SeamlessClient")
        {
            //Jimm thank you. This is the best
            var ctx = new TaskCompletionSource<T4>();
            MySandboxGame.Static.Invoke(() =>
            {
                try
                {
                    ctx.SetResult(action.Invoke(arg, arg2, arg3));
                    ctx.Task.ContinueWith(task => task.Dispose());
                }
                catch (Exception e)
                {
                    ctx.SetException(e);
                }

            }, caller);
            return ctx.Task;
        }
    }
}
