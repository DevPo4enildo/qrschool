using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qrschool.Models
{
    public class Computer
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? InventoryNo { get; set; }
        public Guid? RoomId { get; set; }

        public string? Hostname { get; set; }
        public string? Cpu { get; set; }
        public int? RamGb { get; set; }
        public string? Storage { get; set; }
        public string? Gpu { get; set; }
        public string? Os { get; set; }

        public string Status { get; set; } = "in_use";
        public string? Comment { get; set; }
    }
}
