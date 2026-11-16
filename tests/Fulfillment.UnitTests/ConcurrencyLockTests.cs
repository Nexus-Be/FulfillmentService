namespace Fulfillment.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Fulfillment.Domain.Exceptions;
using Fulfillment.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class ConcurrencyLockTests
{
    private readonly KeyedConcurrencyLockService _locker = new(NullLogger<KeyedConcurrencyLockService>.Instance);

    [Fact]
    public async Task AcquireLockAsync_ShouldSerializeConcurrentAccess_OnSameKey()
    {
        // Arrange
        const int concurrentTasks = 10;
        int sharedCounter = 0;
        var tasks = new List<Task>();

        // Act - Launch concurrent increments on the exact same resource key
        for (int i = 0; i < concurrentTasks; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                using var _ = await _locker.AcquireLockAsync("resource:sku:123", TimeSpan.FromSeconds(10));
                var current = sharedCounter;
                await Task.Delay(10); // Simulate asynchronous database work
                sharedCounter = current + 1;
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - If locking works, counter must be exactly concurrentTasks
        sharedCounter.Should().Be(concurrentTasks);
    }

    [Fact]
    public async Task AcquireLocksAsync_ShouldSortKeys_AndPreventDeadlock()
    {
        // Arrange - Task 1 requests ["key:A", "key:B"], Task 2 requests ["key:B", "key:A"]
        var task1 = Task.Run(async () =>
        {
            using var _ = await _locker.AcquireLocksAsync(new[] { "key:A", "key:B" }, TimeSpan.FromSeconds(5));
            await Task.Delay(20);
        });

        var task2 = Task.Run(async () =>
        {
            using var _ = await _locker.AcquireLocksAsync(new[] { "key:B", "key:A" }, TimeSpan.FromSeconds(5));
            await Task.Delay(20);
        });

        // Act & Assert - Both must complete cleanly without deadlock
        var whenAll = Task.WhenAll(task1, task2);
        await whenAll.WaitAsync(TimeSpan.FromSeconds(10));
        whenAll.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task AcquireLockAsync_ShouldThrowConflict_WhenTimeoutExceeded()
    {
        // Arrange - Hold lock in Task 1
        var barrier = new TaskCompletionSource();
        var lockHolder = Task.Run(async () =>
        {
            using var _ = await _locker.AcquireLockAsync("busy:resource", TimeSpan.FromSeconds(5));
            barrier.SetResult();
            await Task.Delay(500); // Hold for 500ms
        });

        await barrier.Task; // Wait until lock is held

        // Act & Assert - Attempt to acquire with 50ms timeout must throw CONCURRENCY_CONFLICT
        var act = async () =>
        {
            using var _ = await _locker.AcquireLockAsync("busy:resource", TimeSpan.FromMilliseconds(50));
        };

        var ex = await act.Should().ThrowAsync<BusinessRuleValidationException>();
        ex.Which.ErrorCode.Should().Be("CONCURRENCY_CONFLICT");

        await lockHolder;
    }
}
