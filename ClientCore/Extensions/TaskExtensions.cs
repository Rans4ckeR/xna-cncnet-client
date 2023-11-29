using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if NETFRAMEWORK
using System.Threading;
#endif

namespace ClientCore.Extensions;

public static class TaskExtensions
{
#if NET8_0_OR_GREATER
    /// <summary>
    /// Runs a <see cref="Task"/> and guarantees all exceptions are caught and handled even when the <see cref="Task"/> is not directly awaited.
    /// </summary>
    /// <param name="task">The <see cref="Task"/> whose exceptions will be handled.</param>
    /// <param name="configureAwaitOptions">The <see cref="ConfigureAwaitOptions"/> to use.</param>
    /// <returns>Returns a <see cref="Task"/> that awaited and handled the original <paramref name="task"/>.</returns>
    public static async Task HandleTask(this Task task, ConfigureAwaitOptions configureAwaitOptions = ConfigureAwaitOptions.None)
    {
        try
        {
            await task.ConfigureAwait(configureAwaitOptions);
        }
        catch (Exception ex)
        {
            ProgramConstants.HandleException(ex);
        }
    }

    /// <summary>
    /// Runs a <see cref="Task"/> and guarantees all exceptions are caught and handled even when the <see cref="Task"/> is not directly awaited.
    /// </summary>
    /// <typeparam name="T">The type of <paramref name="task"/>'s return value.</typeparam>
    /// <param name="task">The <see cref="Task"/> whose exceptions will be handled.</param>
    /// <param name="configureAwaitOptions">The <see cref="ConfigureAwaitOptions"/> to use.</param>
    /// <returns>Returns a <see cref="Task"/> that awaited and handled the original <paramref name="task"/>.</returns>
    public static async Task<T> HandleTask<T>(this Task<T> task, ConfigureAwaitOptions configureAwaitOptions = ConfigureAwaitOptions.None)
    {
        try
        {
            return await task.ConfigureAwait(configureAwaitOptions);
        }
        catch (Exception ex)
        {
            ProgramConstants.HandleException(ex);
        }

        return default;
    }

    /// <summary>
    /// Executes a list of tasks and waits for all of them to complete and throws an <see cref="AggregateException"/> containing all exceptions from all tasks.
    /// When using <see cref="Task.WhenAll(IEnumerable{Task})"/> only the first thrown exception from a single <see cref="Task"/> may be observed.
    /// </summary>
    /// <typeparam name="T">The type of <paramref name="tasks"/>'s return value.</typeparam>
    /// <param name="tasks">The list of <see cref="Task"/>s whose exceptions will be handled.</param>
    /// <param name="configureAwaitOptions">The <see cref="ConfigureAwaitOptions"/> to use.</param>
    /// <returns>Returns a <see cref="Task"/> that awaited and handled the original <paramref name="tasks"/>.</returns>
    public static async Task<T[]> WhenAllSafe<T>(IEnumerable<Task<T>> tasks, ConfigureAwaitOptions configureAwaitOptions = ConfigureAwaitOptions.None)
    {
        var whenAllTask = Task.WhenAll(tasks);

        try
        {
            return await whenAllTask.ConfigureAwait(configureAwaitOptions);
        }
        catch
        {
            if (whenAllTask.Exception is null)
                throw;

            throw whenAllTask.Exception;
        }
    }

    /// <summary>
    /// Executes a list of tasks and waits for all of them to complete and throws an <see cref="AggregateException"/> containing all exceptions from all tasks.
    /// When using <see cref="Task.WhenAll(IEnumerable{Task})"/> only the first thrown exception from a single <see cref="Task"/> may be observed.
    /// </summary>
    /// <param name="tasks">The list of <see cref="Task"/>s whose exceptions will be handled.</param>
    /// <param name="configureAwaitOptions">The <see cref="ConfigureAwaitOptions"/> to use.</param>
    /// <returns>Returns a <see cref="Task"/> that awaited and handled the original <paramref name="tasks"/>.</returns>
    public static async Task WhenAllSafe(IEnumerable<Task> tasks, ConfigureAwaitOptions configureAwaitOptions = ConfigureAwaitOptions.None)
    {
        var whenAllTask = Task.WhenAll(tasks);

        try
        {
            await whenAllTask.ConfigureAwait(configureAwaitOptions);
        }
        catch
        {
            if (whenAllTask.Exception is null)
                throw;

            throw whenAllTask.Exception;
        }
    }
#else
    /// <summary>
    /// Runs a <see cref="Task"/> and guarantees all exceptions are caught and handled even when the <see cref="Task"/> is not directly awaited.
    /// </summary>
    /// <param name="task">The <see cref="Task"/> whose exceptions will be handled.</param>
    /// <param name="continueOnCapturedContext">true to attempt to marshal the continuation back to the original context captured; otherwise, false.</param>
    /// <returns>Returns a <see cref="Task"/> that awaited and handled the original <paramref name="task"/>.</returns>
    public static async Task HandleTask(this Task task, bool continueOnCapturedContext = false)
    {
        try
        {
            await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (Exception ex)
        {
            ProgramConstants.HandleException(ex);
        }
    }

    /// <summary>
    /// Runs a <see cref="Task"/> and guarantees all exceptions are caught and handled even when the <see cref="Task"/> is not directly awaited.
    /// </summary>
    /// <typeparam name="T">The type of <paramref name="task"/>'s return value.</typeparam>
    /// <param name="task">The <see cref="Task"/> whose exceptions will be handled.</param>
    /// <param name="continueOnCapturedContext">true to attempt to marshal the continuation back to the original context captured; otherwise, false.</param>
    /// <returns>Returns a <see cref="Task"/> that awaited and handled the original <paramref name="task"/>.</returns>
    public static async Task<T> HandleTask<T>(this Task<T> task, bool continueOnCapturedContext = false)
    {
        try
        {
            return await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (Exception ex)
        {
            ProgramConstants.HandleException(ex);
        }

        return default;
    }

    /// <summary>
    /// Executes a list of tasks and waits for all of them to complete and throws an <see cref="AggregateException"/> containing all exceptions from all tasks.
    /// When using <see cref="Task.WhenAll(IEnumerable{Task})"/> only the first thrown exception from a single <see cref="Task"/> may be observed.
    /// </summary>
    /// <typeparam name="T">The type of <paramref name="tasks"/>'s return value.</typeparam>
    /// <param name="tasks">The list of <see cref="Task"/>s whose exceptions will be handled.</param>
    /// <param name="continueOnCapturedContext">true to attempt to marshal the continuation back to the original context captured; otherwise, false.</param>
    /// <returns>Returns a <see cref="Task"/> that awaited and handled the original <paramref name="tasks"/>.</returns>
    public static async Task<T[]> WhenAllSafe<T>(IEnumerable<Task<T>> tasks, bool continueOnCapturedContext = false)
    {
        var whenAllTask = Task.WhenAll(tasks);

        try
        {
            return await whenAllTask.ConfigureAwait(continueOnCapturedContext);
        }
        catch
        {
            if (whenAllTask.Exception is null)
                throw;

            throw whenAllTask.Exception;
        }
    }

    /// <summary>
    /// Executes a list of tasks and waits for all of them to complete and throws an <see cref="AggregateException"/> containing all exceptions from all tasks.
    /// When using <see cref="Task.WhenAll(IEnumerable{Task})"/> only the first thrown exception from a single <see cref="Task"/> may be observed.
    /// </summary>
    /// <param name="tasks">The list of <see cref="Task"/>s whose exceptions will be handled.</param>
    /// <param name="continueOnCapturedContext">true to attempt to marshal the continuation back to the original context captured; otherwise, false.</param>
    /// <returns>Returns a <see cref="Task"/> that awaited and handled the original <paramref name="tasks"/>.</returns>
    public static async Task WhenAllSafe(IEnumerable<Task> tasks, bool continueOnCapturedContext = false)
    {
        var whenAllTask = Task.WhenAll(tasks);

        try
        {
            await whenAllTask.ConfigureAwait(continueOnCapturedContext);
        }
        catch
        {
            if (whenAllTask.Exception is null)
                throw;

            throw whenAllTask.Exception;
        }
    }
#endif
#if NETFRAMEWORK

    /// <summary>
    /// Runs a <see cref="Task"/> which is cancelled with the given <paramref name="cancellationToken"/>.
    /// </summary>
    /// <param name="task">The <see cref="Task"/> whose exceptions will be handled.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to cancel <paramref name="task"/>.</param>
    public static async Task WithCancellation(this Task task, CancellationToken cancellationToken)
    {
        var timeOutTask = Task.Delay(TimeSpan.FromMilliseconds(-1), cancellationToken);

        await Task.WhenAny([task, timeOutTask]).ConfigureAwait(false);

        if (task.IsCompleted)
            return;

        if (task.IsFaulted)
            throw task.Exception;

        throw new OperationCanceledException();
    }

    /// <summary>
    /// Runs a <see cref="Task"/> which is cancelled with the given <paramref name="cancellationToken"/>.
    /// </summary>
    /// <param name="task">The <see cref="Task"/> whose exceptions will be handled.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to cancel <paramref name="task"/>.</param>
    public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
    {
        var timeOutTask = Task.Delay(TimeSpan.FromMilliseconds(-1), cancellationToken);

        await Task.WhenAny([task, timeOutTask]).ConfigureAwait(false);

        if (task.IsCompleted)
            return await task.ConfigureAwait(false);

        if (task.IsFaulted)
            throw task.Exception;

        throw new OperationCanceledException();
    }
#endif

    /// <summary>
    /// Runs a <see cref="ValueTask"/> and guarantees all exceptions are caught and handled even when the <see cref="ValueTask"/> is not directly awaited.
    /// </summary>
    /// <param name="task">The <see cref="ValueTask"/> whose exceptions will be handled.</param>
    /// <param name="continueOnCapturedContext">true to attempt to marshal the continuation back to the original context captured; otherwise, false.</param>
    public static async void HandleTask(this ValueTask task, bool continueOnCapturedContext = false)
    {
        try
        {
            await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (Exception ex)
        {
            ProgramConstants.HandleException(ex);
        }
    }
}