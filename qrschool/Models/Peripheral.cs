using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qrschool.Models
{
    public class Peripheral
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? InventoryNo { get; set; }

        public Guid? RoomId { get; set; }
        public Guid? ComputerId { get; set; }

        public string Type { get; set; } = string.Empty;    // keyboard / mouse / printer...
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }

        public string Status { get; set; } = "in_use";
        public string? Comment { get; set; }
    }
}
