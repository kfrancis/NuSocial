using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuSocial.Core.Threading
{
    /// <summary>
    /// An <see cref="ICustomDispatcher"/> that uses the <see cref="Application.Current"/>'s dispatcher to execute code on the UI thread.
    /// </summary>
    public class MauiDispatcher : ICustomDispatcher
    {
        /// <summary>
        /// Creates a new <see cref="MauiDispatcher"/>.
        /// </summary>
        public MauiDispatcher()
        {
            DispatcherType = DispatcherType.Main;
        }

        /// <inheritdoc/>
        public DispatcherType DispatcherType { get; }

        public void Run(Action execute)
        {
            MainThread.BeginInvokeOnMainThread(execute);
        }

        /// <inheritdoc/>
        public Task RunAsync(Action execute)
        {
            var tcs = new TaskCompletionSource<object?>();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    execute();
                    tcs.SetResult(null);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });

            return tcs.Task;
        }

        /// <inheritdoc/>
        public Task<T> RunAsync<T>(Func<T> execute)
        {
            return MainThread.InvokeOnMainThreadAsync(execute);
        }

        /// <inheritdoc/>
        public Task RunAsync<T>(Func<Task> execute)
        {
            if (execute is null)
            {
                return Task.CompletedTask;
            }

            return MainThread.InvokeOnMainThreadAsync<Task>(() => execute());
        }

        /// <inheritdoc/>
        public Task<T> RunAsync<T>(Func<Task<T>> execute)
        {
            return MainThread.InvokeOnMainThreadAsync(async () => await execute());
        }

        public Task RunAsync(Func<Task> execute)
        {
            return MainThread.InvokeOnMainThreadAsync<Task>(() => execute());
        }
    }
}
