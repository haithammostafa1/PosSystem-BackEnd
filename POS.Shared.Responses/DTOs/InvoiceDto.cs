using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Shared.Responses.DTOs
{
    public class InvoiceCreateDto
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public int? CustomerId { get; set; }

        public decimal PaidAmount { get; set; }
        public decimal Discount { get; set; }
        public int PaymentMethod { get; set; }

        public List<InvoiceDetailSaveDto> Details { get; set; } = new();
    }

    public class InvoiceResponseDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int? CustomerId { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Discount { get; set; }
        public int PaymentMethod { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }

       
        public List<InvoiceDetailResponseDto> Details { get; set; } = new();
    }
}

