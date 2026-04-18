using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using POS.Shared.Responses.DTOs;

namespace Pos.Datalayer
{
    public interface IInvoiceDetailsRepository
    {
         Task<int> AddInvoiceDetails(int invoiceId, List<InvoiceDetailSaveDto> details);
        Task<List<InvoiceDetailResponseDto>> GetInvoiceDetailsByInvoiceId(int invoiceId);
        Task<int> DeleteInvoiceDetailsByInvoiceId(int invoiceId);
    }
    public class InvoiceDetailsRepository : IInvoiceDetailsRepository
    {
        private readonly string _connectionString;

        public InvoiceDetailsRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<int> AddInvoiceDetails(int invoiceId, List<InvoiceDetailSaveDto> details)
        {
            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_AddInvoiceDetails", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@InvoiceId", SqlDbType.Int).Value = invoiceId;

            var table = new DataTable();
            table.Columns.Add("ProductId", typeof(int));
            table.Columns.Add("Quantity", typeof(int));
            table.Columns.Add("Price", typeof(decimal));

            foreach (var item in details)
            {
                table.Rows.Add(item.ProductId, item.Quantity, item.Price);
            }

            var param = cmd.Parameters.AddWithValue("@Details", table);
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "dbo.InvoiceDetailsType";

            await conn.OpenAsync();

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<InvoiceDetailResponseDto>> GetInvoiceDetailsByInvoiceId(int invoiceId)
        {
            Log.Information("DAL: Getting invoice details for invoice {Id}", invoiceId);

            var list = new List<InvoiceDetailResponseDto>();

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_GetInvoiceDetailsByInvoiceId", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@InvoiceId", SqlDbType.Int).Value = invoiceId;

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new InvoiceDetailResponseDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                    ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                    Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                    Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                    Total = reader.GetDecimal(reader.GetOrdinal("Total"))
                });
            }

            return list;
        }

        public async Task<int> DeleteInvoiceDetailsByInvoiceId(int invoiceId)
        {
            Log.Information("DAL: Deleting invoice details for invoice {Id}", invoiceId);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_DeleteInvoiceDetailsByInvoiceId", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@InvoiceId", SqlDbType.Int).Value = invoiceId;

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }
    }
}
