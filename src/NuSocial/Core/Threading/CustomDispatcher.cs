using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuSocial.Core.Threading
{
    /// <summary>
    /// Flags describing the type of behavior an <see cref="ICustomDispatcher"/> has.
    /// </summary>
    [Flags]
    public enum DispatcherType
    {
        /// <summary>
        /// The <see cref="ICustomDispatcher"/> is a normal dispatcher service with no special features.
        /// </summary>
        None = 0,

        /// <summary>
        /// The <see cref="ICustomDispatcher"/> is connected to the same thread used for UI updates (or, in the case of apps without UI, the main thread).
        /// </summary>
        Main = 1,

        /// <summary>
        /// The <see cref="ICustomDispatcher"/> runs code on a background (non-blocking) thread of the application.
        /// </summary>
        Background = 1 << 1
    }

    /// <summary>
    /// Represents a service which can delegate and manage the execution of code (e.g. on the UI thread, in the background, etc.).
    /// </summary>
    public interface ICustomDispatcher
    {
        /// <summary>
        /// A set of <see cref="DispatcherType"/> flags indicating whether this <see cref="ICustomDispatcher"/> manages special kinds of threads, which can (and should) be utilized in scenarios such as updating UI elements from code.
        /// </summary>
        DispatcherType DispatcherType { get; }

        void Run(Action execute);

        /// <summary>
        /// Executes an <see cref="Action"/> asynchronously.
        /// </summary>
        /// <param name="execute">The <see cref="Action"/> to run on this <see cref="ICustomDispatcher"/>.</param>
        Task RunAsync(Action execute);

        /// <summary>
        /// Executes a <see cref="Func{TResult}"/> asynchronously.
        /// </summary>
        /// <param name="execute">The <see cref="Func{TResult}"/> to run on this <see cref="ICustomDispatcher"/>.</param>
        /// <returns>The <typeparamref name="T"/> result of the <see cref="Func{TResult}"/>.</returns>
        Task<T> RunAsync<T>(Func<T> execute);

        /// <summary>
        /// Executes a <see cref="Task"/> asynchronously.
        /// </summary>
        /// <param name="execute">The awaitable <see cref="Task"/> to run on this <see cref="ICustomDispatcher"/>.</param>
        Task RunAsync(Func<Task> execute);

        /// <summary>
        /// Executes a <see cref="Task{TResult}"/> asynchronously.
        /// </summary>
        /// <param name="execute">The <see cref="Task{TResult}"/> to run on this <see cref="ICustomDispatcher"/>.</param>
        /// <returns>The <typeparamref name="T"/> result of the <see cref="Task{TResult}"/>.</returns>
        Task<T> RunAsync<T>(Func<Task<T>> execute);
    }

    /// <summary>
    /// Provides extension methods for the <see cref="ICustomDispatcher"/> interface.
    /// </summary>
    public static class DispatcherExtensions
    {
        /// <summary>
        /// Gets the first found <see cref="ICustomDispatcher"/> with the specified <see cref="DispatcherType"/> flag.
        /// </summary>
        /// <param name="dispatchers">The collection of available <see cref="ICustomDispatcher"/>s.</param>
        /// <param name="type">The <see cref="DispatcherType"/> requested from the desired <see cref="ICustomDispatcher"/>.</param>
        /// <returns>The <see cref="ICustomDispatcher"/> managing the background thread.</returns>
        public static ICustomDispatcher GetDispatcher(this IEnumerable<ICustomDispatcher> dispatchers, DispatcherType type)
        {
            if (dispatchers == null || !dispatchers.Any())
            {
                throw new ArgumentException("The collection must contain at least one available IDispatcher.", nameof(dispatchers));
            }

            var background = dispatchers.FirstOrDefault(d => d.DispatcherType.HasFlag(type));
            if (background != null)
            {
                return background;
            }
            else
            {
                throw new ArgumentException($"No IDispatchers of the specified type {type} were avaliable in the provided collection", nameof(dispatchers));
            }
        }

        /// <summary>
        /// Gets the first found <see cref="ICustomDispatcher"/> that manages the UI thread (see <see cref="DispatcherType.Main"/>).
        /// </summary>
        /// <param name="dispatchers">The collection of available <see cref="ICustomDispatcher"/>s.</param>
        /// <returns>The <see cref="ICustomDispatcher"/> managing the UI thread.</returns>
        public static ICustomDispatcher GetUIDispatcher(this IEnumerable<ICustomDispatcher> dispatchers)
            => dispatchers.GetDispatcher(DispatcherType.Main);

        /// <summary>
        /// Runs the given code on the UI thread's <see cref="ICustomDispatcher"/>.
        /// </summary>
        /// <param name="dispatchers">The collection of available <see cref="ICustomDispatcher"/>s.</param>
        /// <param name="execute">The code to execute.</param>
        public static void RunOnUIThread(this IEnumerable<ICustomDispatcher> dispatchers, Action execute)
            => dispatchers.GetUIDispatcher().Run(execute);

        /// <summary>
        /// Runs the given code on the UI thread's <see cref="ICustomDispatcher"/>.
        /// </summary>
        /// <param name="dispatchers">The collection of available <see cref="ICustomDispatcher"/>s.</param>
        /// <param name="execute">The code to execute.</param>
        public static async Task RunOnUIThreadAsync(this IEnumerable<ICustomDispatcher> dispatchers, Action execute)
            => await dispatchers.GetUIDispatcher().RunAsync(execute);

        /// <summary>
        /// Runs the given code on the UI thread's <see cref="ICustomDispatcher"/>.
        /// </summary>
        /// <param name="dispatchers">The collection of available <see cref="ICustomDispatcher"/>s.</param>
        /// <param name="execute">The code to execute.</param>
        public static async Task RunOnUIThreadAsync(this Action execute, IEnumerable<ICustomDispatcher> dispatchers)
            => await dispatchers.GetUIDispatcher().RunAsync(execute);

        /// <summary>
        /// Runs the given code on the UI thread's <see cref="ICustomDispatcher"/>.
        /// </summary>
        /// <param name="dispatchers">The collection of available <see cref="ICustomDispatcher"/>s.</param>
        /// <param name="execute">The code to execute.</param>
        /// <typeparam name="T">The type of output <paramref name="execute"/> produces.</typeparam>
        /// <returns>The <typeparamref name="T"/> return value of <paramref name="execute"/>.</returns>
        public static async Task<T> RunOnUIThreadAsync<T>(this IEnumerable<ICustomDispatcher> dispatchers, Func<T> execute)
            => await dispatchers.GetUIDispatcher().RunAsync(execute);

        /// <summary>
        /// Runs the given code on the UI thread's <see cref="ICustomDispatcher"/>.
        /// </summary>
        /// <param name="dispatchers">The collection of available <see cref="ICustomDispatcher"/>s.</param>
        /// <param name="execute">The code to execute.</param>
        /// <typeparam name="T">The type of output <paramref name="execute"/> produces.</typeparam>
        /// <returns>The <typeparamref name="T"/> return value of <paramref name="execute"/>.</returns>
        public static async Task<T> RunOnUIThreadAsync<T>(this Func<T> execute, IEnumerable<ICustomDispatcher> dispatchers)
            => await dispatchers.GetUIDispatcher().RunAsync(execute);

        /// <summary>
        /// Runs the given code on the UI thread's <see cref="ICustomDispatcher"/>.
        /// </summary>
        /// <param name="dispatchers">The collection of available <see cref="ICustomDispatcher"/>s.</param>
        /// <param name="execute">The code to execute.</param>
        public static async Task RunOnUIThreadAsync(this IEnumerable<ICustomDispatcher> dispatchers, Func<Task> execute)
            => await dispatchers.GetUIDispatcher().RunAsync(execute);

        /// <summary>
        /// Runs the given code on the UI thread's <see cref="ICustomDispatcher"/>.
        /// </summary>
        /// <param name="dispatchers">The collection of available <see cref="ICustomDispatcher"/>s.</param>
        /// <param name="execute">The code to execute.</param>
        public static async Task RunOnUIThreadAsync(this Func<Task> execute, IEnumerable<ICustomDispatcher> dispatchers)
            => await dispatchers.GetUIDispatcher().RunAsync(execute);

        /// <summary>
        /// Runs the given code on the UI thread's <see cref="ICustomDispatcher"/>.
        /// </summary>
        /// <param name="dispatchers">The collection of available <see cref="ICustomDispatcher"/>s.</param>
        /// <param name="execute">The code to execute.</param>
        /// <typeparam name="T">The type of output <paramref name="execute"/> produces.</typeparam>
        /// <returns>The <typeparamref name="T"/> return value of <paramref name="execute"/>.</returns>
        public static async Task<T> RunOnUIThreadAsync<T>(this IEnumerable<ICustomDispatcher> dispatchers, Func<Task<T>> execute)
            => await dispatchers.GetUIDispatcher().RunAsync(execute);

        /// <summary>
        /// Runs the given code on the UI thread's <see cref="ICustomDispatcher"/>.
        /// </summary>
        /// <param name="dispatchers">The collection of available <see cref="ICustomDispatcher"/>s.</param>
        /// <param name="execute">The code to execute.</param>
        /// <typeparam name="T">The type of output <paramref name="execute"/> produces.</typeparam>
        /// <returns>The <typeparamref name="T"/> return value of <paramref name="execute"/>.</returns>
        public static async Task<T> RunOnUIThreadAsync<T>(this Func<Task<T>> execute, IEnumerable<ICustomDispatcher> dispatchers)
            => await dispatchers.GetUIDispatcher().RunAsync(execute);
    }
}
