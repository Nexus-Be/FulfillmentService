namespace Fulfillment.Infrastructure.Services;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Interfaces;
using Fulfillment.Domain.Exceptions;
using Microsoft.Extensions.Logging;

public sealed class KeyedConcurrencyLockService : IConcurrencyLockService
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);
    private readonly ConcurrentDictionary<string, RefCountedSemaphore> _semaphores = new();
    private readonly ILogger<KeyedConcurrencyLockService> _logger;

    public KeyedConcurrencyLockService(ILogger<KeyedConcurrencyLockService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IDisposable> AcquireLockAsync(string key, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return AcquireLocksAsync(new[] { key }, timeout, cancellationToken);
    }

    public async Task<IDisposable> AcquireLocksAsync(IEnumerable<string> keys, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        // Deduplicate and order keys deterministically to eliminate deadlocks
        var sortedKeys = keys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        if (sortedKeys.Count == 0)
        {
            return EmptyDisposable.Instance;
        }

        var effectiveTimeout = timeout ?? DefaultTimeout;
        var acquiredSemaphores = new List<(string Key, RefCountedSemaphore RefSem)>(sortedKeys.Count);

        try
        {
            foreach (var key in sortedKeys)
            {
                var refSem = GetOrAddSemaphore(key);
                var entered = await refSem.Semaphore.WaitAsync(effectiveTimeout, cancellationToken);

                if (!entered)
                {
                    _logger.LogWarning("Timed out waiting to acquire lock for key '{Key}' after {TimeoutSeconds}s.", key, effectiveTimeout.TotalSeconds);
                    throw new BusinessRuleValidationException(
                        "CONCURRENCY_CONFLICT",
                        $"The requested resource '{key}' is currently busy. Please try again.",
                        409);
                }

                acquiredSemaphores.Add((key, refSem));
            }

            return new Releaser(this, acquiredSemaphores);
        }
        catch
        {
            // If any acquisition fails, release all previously acquired semaphores immediately
            ReleaseSemaphores(acquiredSemaphores);
            throw;
        }
    }

    private RefCountedSemaphore GetOrAddSemaphore(string key)
    {
        lock (_semaphores)
        {
            var refSem = _semaphores.GetOrAdd(key, _ => new RefCountedSemaphore());
            Interlocked.Increment(ref refSem.RefCount);
            return refSem;
        }
    }

    private void ReleaseSemaphores(List<(string Key, RefCountedSemaphore RefSem)> acquired)
    {
        // Release in reverse acquisition order
        for (int i = acquired.Count - 1; i >= 0; i--)
        {
            var (key, refSem) = acquired[i];
            refSem.Semaphore.Release();

            lock (_semaphores)
            {
                if (Interlocked.Decrement(ref refSem.RefCount) <= 0)
                {
                    _semaphores.TryRemove(key, out _);
                }
            }
        }
    }

    private sealed class RefCountedSemaphore
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public int RefCount;
    }

    private sealed class Releaser : IDisposable
    {
        private KeyedConcurrencyLockService? _owner;
        private List<(string Key, RefCountedSemaphore RefSem)>? _acquired;

        public Releaser(KeyedConcurrencyLockService owner, List<(string Key, RefCountedSemaphore RefSem)> acquired)
        {
            _owner = owner;
            _acquired = acquired;
        }

        public void Dispose()
        {
            var owner = Interlocked.Exchange(ref _owner, null);
            var acquired = Interlocked.Exchange(ref _acquired, null);

            if (owner != null && acquired != null)
            {
                owner.ReleaseSemaphores(acquired);
            }
        }
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();
        public void Dispose() { }
    }
}
