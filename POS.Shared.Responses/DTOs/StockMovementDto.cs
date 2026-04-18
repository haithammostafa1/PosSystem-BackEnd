using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Shared.Responses.DTOs
{
    public class StockMovementDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int Type { get; set; }
        public string? Reference { get; set; }
    }
    public class StockMovementResponseDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int Type { get; set; }
        public string? Reference { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}

