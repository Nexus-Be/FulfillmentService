namespace Fulfillment.Domain.Entities;

using System;
using System.Collections.Generic;
using Fulfillment.Domain.Exceptions;

public class Warehouse
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? SellerId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string Type { get; set; } = "OWN";
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDisabled => !IsActive;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();

    public static Warehouse Create(string code, string name, string? address, Guid? sellerId = null, string type = "OWN")
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new BusinessRuleValidationException("Warehouse code is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BusinessRuleValidationException("Warehouse name is required.");
        }

        return new Warehouse
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Address = address?.Trim(),
            SellerId = sellerId,
            Type = type,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateDetails(string name, string? address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BusinessRuleValidationException("Warehouse name cannot be empty.");
        }

        Name = name.Trim();
        Address = address?.Trim();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}
