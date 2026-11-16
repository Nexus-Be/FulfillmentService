namespace Fulfillment.Application.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IConcurrencyLockService
{
    /// <summary>
    /// Acquires an asynchronous lock on a single key. Released when the returned handle is disposed.
    /// </summary>
    Task<IDisposable> AcquireLockAsync(string key, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquires asynchronous locks on multiple keys in a sorted, deadlock-free order.
    /// Released when the returned handle is disposed.
    /// </summary>
    Task<IDisposable> AcquireLocksAsync(IEnumerable<string> keys, TimeSpan? timeout = null, CancellationToken cancellationToken = default);
}
