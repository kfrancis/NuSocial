using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace System.Threading.Tasks
{
    /// <remarks>
    /// This code is largely based on this blog post:
    /// https://olegkarasik.wordpress.com/2019/04/16/code-tip-how-to-work-with-asynchronous-event-handlers-in-c/
    /// and this gist:
    /// https://gist.github.com/OlegKarasik/90c2355e3e170a0885bd06874183428a
    ///
    /// Honestly, full credit to Oleg Karasik for being able to figure all of
    /// this out. I've just gone ahead and polished a bit of it up to get the
    /// additional functionality I would expect to have.
    /// </remarks>
    public static class MulticastDelegateExtensions
    {
        /// <summary>
        /// Invokes a <see cref="MulticastDelegate"/> assuming the signature
        /// of a typical <see cref="EventHandler{TEventArgs}"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the <see cref="EventArgs"/>.
        /// </typeparam>
        /// <param name="this">
        /// The <see cref="MulticastDelegate"/> to invoke.
        /// </param>
        /// <param name="sender">
        /// The sender of the event.
        /// </param>
        /// <param name="eventArgs">
        /// The <see cref="EventArgs"/> to pass into the event.
        /// </param>
        /// <param name="forceOrdering">
        /// <c>true</c> to force the invocation to be in order of registration,
        /// Otherwise, <c>false</c> to allow execution out of order.
        /// </param>
        /// <param name="stopOnFirstError">
        /// <c>true</c> to stop invocation after the first error; Otherwise,
        /// <c>false</c> to continue invocation after the first exception is
        /// caught.
        /// </param>
        /// <returns>
        /// An instance of a <see cref="Task"/>.
        /// </returns>
        /// <exception cref="TargetParameterCountException">
        /// Thrown when the <see cref="MulticastDelegate"/> does not match the
        /// typical <see cref="EventHandler{TEventArgs}"/> syntax.
        /// </exception>
        public static Task MulticastInvokeAsync<T>(
            this MulticastDelegate @this,
            object sender,
            T eventArgs,
            bool forceOrdering,
            bool stopOnFirstError)
            where T : EventArgs => MulticastInvokeAsync(
                @this,
                forceOrdering,
                stopOnFirstError,
                sender,
                eventArgs);

        /// <summary>
        /// Invokes a <see cref="MulticastDelegate"/> assuming the signature
        /// of a typical <see cref="EventHandler{TEventArgs}"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the <see cref="EventArgs"/>.
        /// </typeparam>
        /// <param name="this">
        /// The <see cref="MulticastDelegate"/> to invoke.
        /// </param>
        /// <param name="forceOrdering">
        /// <c>true</c> to force the invocation to be in order of registration,
        /// Otherwise, <c>false</c> to allow execution out of order.
        /// </param>
        /// <param name="stopOnFirstError">
        /// <c>true</c> to stop invocation after the first error; Otherwise,
        /// <c>false</c> to continue invocation after the first exception is
        /// caught.
        /// </param>
        /// <param name="args">
        /// The collection of arguments to pass into the <see cref="MulticastDelegate"/>.
        /// </param>
        /// <returns>
        /// An instance of a <see cref="Task"/>.
        /// </returns>
        /// <exception cref="TargetParameterCountException">
        /// Thrown when the <see cref="MulticastDelegate"/> does not match the
        /// the provided parameter types.
        /// </exception>
        /// <remarks>
        /// This method is discouraged unless you are very confident you know
        /// what you are doing. There is a risk you will break at compile
        /// time because your delegate signature is not checked at compile
        /// time versus your params.
        /// </remarks>
        public static async Task MulticastInvokeAsync(
            this MulticastDelegate @this,
            bool forceOrdering,
            bool stopOnFirstError,
            params object[] args)
        {
            if (@this is null)
            {
                return;
            }

            var taskCompletionSource = new TaskCompletionSource<bool>();

            // this is used to try and ensure we do not try and set more
            // information on the TaskCompletionSource after it is complete
            // due to some out-of-ordering issues
            bool taskCompletionSourceCompleted = false;

            var delegates = @this.GetInvocationList();
            var countOfDelegates = delegates.Length;

            // keep track of exceptions along the way and a separate collection
            // for exceptions we have assigned to the TCS
            var assignedExceptions = new List<Exception>();
            var trackedExceptions = new ConcurrentQueue<Exception>();

            foreach (var @delegate in @this.GetInvocationList())
            {
                var async = @delegate.Method
                    .GetCustomAttributes(typeof(AsyncStateMachineAttribute), false)
                    .Any();

                bool waitFlag = false;
                var completed = new Action(() =>
                {
                    if (Interlocked.Decrement(ref countOfDelegates) == 0)
                    {
                        lock (taskCompletionSource)
                        {
                            if (taskCompletionSourceCompleted)
                            {
                                return;
                            }

                            assignedExceptions.AddRange(trackedExceptions);

                            if (!trackedExceptions.Any())
                            {
                                taskCompletionSource.SetResult(true);
                            }
                            else if (trackedExceptions.Count == 1)
                            {
                                taskCompletionSource.SetException(assignedExceptions[0]);
                            }
                            else
                            {
                                taskCompletionSource.SetException(new AggregateException(assignedExceptions));
                            }

                            taskCompletionSourceCompleted = true;
                        }
                    }

                    waitFlag = true;
                });
                var failed = new Action<Exception>(e =>
                {
                    trackedExceptions.Enqueue(e);
                });

                if (async)
                {
                    var context = new EventHandlerSynchronizationContext(completed, failed);
                    SynchronizationContext.SetSynchronizationContext(context);
                }

                try
                {
                    @delegate.DynamicInvoke(args);
                }
                catch (TargetParameterCountException e)
                {
                    throw;
                }
                catch (TargetInvocationException e) when (e.InnerException != null)
                {
                    // When exception occured inside Delegate.Invoke method all exceptions are wrapped in
                    // TargetInvocationException.
                    failed(e.InnerException);
                }
                catch (Exception e)
                {
                    failed(e);
                }

                if (!async)
                {
                    completed();
                }

                while (forceOrdering && !waitFlag)
                {
                    await Task.Yield();
                }

                if (stopOnFirstError && trackedExceptions.Any() && !taskCompletionSourceCompleted)
                {
                    lock (taskCompletionSource)
                    {
                        if (!taskCompletionSourceCompleted && !assignedExceptions.Any())
                        {
                            assignedExceptions.AddRange(trackedExceptions);
                            if (trackedExceptions.Count == 1)
                            {
                                taskCompletionSource.SetException(assignedExceptions[0]);
                            }
                            else
                            {
                                taskCompletionSource.SetException(new AggregateException(assignedExceptions));
                            }

                            taskCompletionSourceCompleted = true;
                        }
                    }

                    break;
                }
            }

            await taskCompletionSource.Task;
        }

        /// <summary>
        /// Invokes a <see cref="MulticastDelegate"/> assuming the signature
        /// of a typical <see cref="EventHandler{TEventArgs}"/> with a
        /// guarantee of the order the invocation list was created.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the <see cref="EventArgs"/>.
        /// </typeparam>
        /// <param name="this">
        /// The <see cref="MulticastDelegate"/> to invoke.
        /// </param>
        /// <param name="sender">
        /// The sender of the event.
        /// </param>
        /// <param name="eventArgs">
        /// The <see cref="EventArgs"/> to pass into the event.
        /// </param>
        /// <returns>
        /// An instance of a <see cref="Task"/>.
        /// </returns>
        /// <exception cref="TargetParameterCountException">
        /// Thrown when the <see cref="MulticastDelegate"/> does not match the
        /// typical <see cref="EventHandler{TEventArgs}"/> syntax.
        /// </exception>
        public static Task MulticastInvokeOrderedAsync<T>(
            this MulticastDelegate @this,
            object sender,
            T eventArgs)
            where T : EventArgs => MulticastInvokeOrderedAsync<T>(
                 @this,
                 sender,
                 eventArgs,
                 true);

        /// <summary>
        /// Invokes a <see cref="MulticastDelegate"/> assuming the signature
        /// of a typical <see cref="EventHandler{TEventArgs}"/> with a
        /// guarantee of the order the invocation list was created.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the <see cref="EventArgs"/>.
        /// </typeparam>
        /// <param name="this">
        /// The <see cref="MulticastDelegate"/> to invoke.
        /// </param>
        /// <param name="sender">
        /// The sender of the event.
        /// </param>
        /// <param name="eventArgs">
        /// The <see cref="EventArgs"/> to pass into the event.
        /// </param>
        /// <param name="stopOnFirstError">
        /// <c>true</c> to stop invocation after the first error; Otherwise,
        /// <c>false</c> to continue invocation after the first exception is
        /// caught.
        /// </param>
        /// <returns>
        /// An instance of a <see cref="Task"/>.
        /// </returns>
        /// <exception cref="TargetParameterCountException">
        /// Thrown when the <see cref="MulticastDelegate"/> does not match the
        /// typical <see cref="EventHandler{TEventArgs}"/> syntax.
        /// </exception>
        public static Task MulticastInvokeOrderedAsync<T>(
            this MulticastDelegate @this,
            object sender,
            T eventArgs,
            bool stopOnFirstError)
            where T : EventArgs => MulticastInvokeAsync<T>(
                @this,
                sender,
                eventArgs,
                true,
                stopOnFirstError);

        /// <summary>
        /// Invokes a <see cref="MulticastDelegate"/> assuming the signature
        /// of a typical <see cref="EventHandler{TEventArgs}"/> without any
        /// guarantee of ordering.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the <see cref="EventArgs"/>.
        /// </typeparam>
        /// <param name="this">
        /// The <see cref="MulticastDelegate"/> to invoke.
        /// </param>
        /// <param name="sender">
        /// The sender of the event.
        /// </param>
        /// <param name="eventArgs">
        /// The <see cref="EventArgs"/> to pass into the event.
        /// </param>
        /// <returns>
        /// An instance of a <see cref="Task"/>.
        /// </returns>
        /// <exception cref="TargetParameterCountException">
        /// Thrown when the <see cref="MulticastDelegate"/> does not match the
        /// typical <see cref="EventHandler{TEventArgs}"/> syntax.
        /// </exception>
        public static Task MulticastInvokeUnorderedAsync<T>(
            MulticastDelegate @this,
            object sender,
            T eventArgs)
            where T : EventArgs => MulticastInvokeUnorderedAsync<T>(
                @this,
                sender,
                eventArgs,
                true);

        /// <summary>
        /// Invokes a <see cref="MulticastDelegate"/> assuming the signature
        /// of a typical <see cref="EventHandler{TEventArgs}"/> without any
        /// guarantee of ordering.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the <see cref="EventArgs"/>.
        /// </typeparam>
        /// <param name="this">
        /// The <see cref="MulticastDelegate"/> to invoke.
        /// </param>
        /// <param name="sender">
        /// The sender of the event.
        /// </param>
        /// <param name="eventArgs">
        /// The <see cref="EventArgs"/> to pass into the event.
        /// </param>
        /// <param name="stopOnFirstError">
        /// <c>true</c> to stop invocation after the first error; Otherwise,
        /// <c>false</c> to continue invocation after the first exception is
        /// caught.
        /// </param>
        /// <returns>
        /// An instance of a <see cref="Task"/>.
        /// </returns>
        /// <exception cref="TargetParameterCountException">
        /// Thrown when the <see cref="MulticastDelegate"/> does not match the
        /// typical <see cref="EventHandler{TEventArgs}"/> syntax.
        /// </exception>
        public static Task MulticastInvokeUnorderedAsync<T>(
            this MulticastDelegate @this,
            object sender,
            T eventArgs,
            bool stopOnFirstError)
            where T : EventArgs => MulticastInvokeAsync<T>(
                @this,
                sender,
                eventArgs,
                false,
                stopOnFirstError);

        private sealed class EventHandlerSynchronizationContext : SynchronizationContext
        {
            private readonly Action _completed;
            private readonly Action<Exception> _failed;

            public EventHandlerSynchronizationContext(
                Action completed,
                Action<Exception> failed)
            {
                _completed = completed;
                _failed = failed;
            }

            public override SynchronizationContext CreateCopy()
            {
                return new EventHandlerSynchronizationContext(
                    _completed,
                    _failed);
            }

            public override void OperationCompleted() => _completed();

            public override void Post(SendOrPostCallback d, object state)
            {
                if (state is ExceptionDispatchInfo edi)
                {
                    _failed(edi.SourceException);
                }
                else
                {
                    base.Post(d, state);
                }
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                if (state is ExceptionDispatchInfo edi)
                {
                    _failed(edi.SourceException);
                }
                else
                {
                    base.Send(d, state);
                }
            }
        }
    }
}