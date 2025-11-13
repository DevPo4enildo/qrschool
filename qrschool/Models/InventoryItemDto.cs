using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qrschool.Models
{
    public class InventoryItemDto
    {
        public string ObjectType { get; set; } = string.Empty;   // computer / monitor / peripheral
        public string Code { get; set; } = string.Empty;
        public string? InventoryNo { get; set; }
        public string? RoomName { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
