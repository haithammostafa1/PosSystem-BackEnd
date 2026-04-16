using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Shared.Responses.DTOs
{
    public class InvoiceDetailSaveDto
    {
        public int InvoiceId { get; set; }  
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }

      
        public decimal Total => Quantity * Price;
    }
    public class InvoiceDetailResponseDto
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public int ProductId { get; set; }

        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
    }
    public class InvoiceDetailWithProductDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;

        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
    }
    public class InvoiceCreateDto
    {
        public int CustomerId { get; set; }

        public List<InvoiceDetailSaveDto> Details { get; set; } = new();

        // optional
        public decimal Total => Details.Sum(x => x.Total);
    }
}
