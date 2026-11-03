namespace Fulfillment.Domain.Entities;

using System;
using System.Collections.Generic;

public class Warehouse
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? SellerId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string Type { get; set; } = "OWN";
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDisabled
    {
        get => !IsActive;
        set => IsActive = !value;
    }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
