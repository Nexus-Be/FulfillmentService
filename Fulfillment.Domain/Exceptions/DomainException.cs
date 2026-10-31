namespace Fulfillment.Domain.Exceptions;

using System;

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
