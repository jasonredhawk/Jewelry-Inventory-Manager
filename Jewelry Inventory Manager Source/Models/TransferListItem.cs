using System;

namespace Moonglow_DB.Models
{
    public class TransferListItem
    {
        public int ItemId { get; set; }
        public string ItemType { get; set; } // "Product" or "Component"
        public string DisplayName { get; set; }
        public string SKU { get; set; }
        public int Quantity { get; set; }
        public int AvailableStock { get; set; }
        public string Notes { get; set; }
        public bool IsProduct => ItemType == "Product";
        public bool IsComponent => ItemType == "Component";
        
        // For products, this will include the components that will be transferred
        public string ComponentBreakdown { get; set; }
        
        public TransferListItem()
        {
            Notes = string.Empty;
            ComponentBreakdown = string.Empty;
        }
        
        public override string ToString()
        {
            return $"{DisplayName} ({ItemType}) - Qty: {Quantity}";
        }
    }
} 