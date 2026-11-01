namespace Fulfillment.Domain.Exceptions;

using System;
using System.Collections.Generic;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with key '{key}' was not found.") { }
}

public class BusinessRuleValidationException : DomainException
{
    public BusinessRuleValidationException(string message) : base(message) { }
}

public class InsufficientInventoryException : DomainException
{
    public Guid SkuId { get; }
    public int RequestedQuantity { get; }
    public int AvailableQuantity { get; }

    public InsufficientInventoryException(Guid skuId, int requestedQuantity, int availableQuantity)
        : base($"Insufficient available quantity for SKU: '{skuId}'. Requested: {requestedQuantity}, Available: {availableQuantity}.")
    {
        SkuId = skuId;
        RequestedQuantity = requestedQuantity;
        AvailableQuantity = availableQuantity;
    }
}

public class ReservationItemFailure
{
    public Guid SkuId { get; }
    public string ErrorCode { get; }
    public int RequestedQuantity { get; }
    public int AvailableQuantity { get; }
    public string Message { get; }

    public ReservationItemFailure(Guid skuId, string errorCode, int requestedQuantity, int availableQuantity, string message)
    {
        SkuId = skuId;
        ErrorCode = errorCode;
        RequestedQuantity = requestedQuantity;
        AvailableQuantity = availableQuantity;
        Message = message;
    }
}

public class InsufficientInventoryBatchException : DomainException
{
    public IReadOnlyList<ReservationItemFailure> Failures { get; }

    public InsufficientInventoryBatchException(IReadOnlyList<ReservationItemFailure> failures)
        : base("One or more items in the reservation request are invalid, unavailable, or have insufficient stock.")
    {
        Failures = failures ?? Array.Empty<ReservationItemFailure>();
    }
}

public class ReservationAlreadyCommittedException : DomainException
{
    public string ReferenceId { get; }
    public DateTimeOffset CommittedAt { get; }
    public object? Reservation { get; }

    public ReservationAlreadyCommittedException(string referenceId, DateTimeOffset committedAt, object? reservation = null)
        : base($"Cannot release reservation for reference '{referenceId}' because it was already committed at {committedAt:O}.")
    {
        ReferenceId = referenceId;
        CommittedAt = committedAt;
        Reservation = reservation;
    }
}
