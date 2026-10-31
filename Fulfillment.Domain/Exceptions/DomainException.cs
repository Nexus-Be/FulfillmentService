namespace Fulfillment.Domain.Exceptions;

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
