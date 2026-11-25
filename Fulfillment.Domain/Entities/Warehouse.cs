namespace Fulfillment.Domain.Entities;

using System;
using System.Collections.Generic;
using Fulfillment.Domain.Base;

public class Warehouse : BaseEntity<Guid>
{
    public Guid? SellerId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Address { get; set; }
    public string? ContactPhone { get; set; }
    public string? ProvinceName { get; set; }
    /// <summary>GHN province id. Set only together with the district and ward, after the three were checked against the GHN lists.</summary>
    public int? ProvinceId { get; set; }
    public int? DistrictId { get; set; }
    public string? WardCode { get; set; }
    public int? GhnShopId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDisabled
    {
        get => !IsActive;
        set => IsActive = !value;
    }

    // Navigation
    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
