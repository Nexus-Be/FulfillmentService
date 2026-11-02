namespace Fulfillment.Domain.Exceptions;

using System;
using System.Collections.Generic;

public class DomainException : Exception
{
    public string ErrorCode { get; } = "DOMAIN_ERROR";
    public int StatusCode { get; } = 400;

    public DomainException(string message) : base(message) { }
    public DomainException(string errorCode, string message, int statusCode = 400) : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object key)
        : base("NOT_FOUND", $"Entity '{entityName}' with key '{key}' was not found.", 404) { }

    public EntityNotFoundException(string errorCode, string entityName, object key)
        : base(errorCode, $"Entity '{entityName}' with key '{key}' was not found.", 404) { }
}

public class BusinessRuleValidationException : DomainException
{
    public BusinessRuleValidationException(string message)
        : base("BUSINESS_RULE_VIOLATION", message, 400) { }

    public BusinessRuleValidationException(string errorCode, string message, int statusCode = 400)
        : base(errorCode, message, statusCode) { }
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
        : base("INSUFFICIENT_INVENTORY", "One or more items in the reservation request are invalid, unavailable, or have insufficient stock.", 400)
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
        : base("RESERVATION_ALREADY_COMMITTED", $"Cannot release reservation for reference '{referenceId}' because it was already committed at {committedAt:O}.", 409)
    {
        ReferenceId = referenceId;
        CommittedAt = committedAt;
        Reservation = reservation;
    }
}
