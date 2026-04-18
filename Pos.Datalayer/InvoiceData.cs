using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Pos.Datalayer.Helpers;
using POS.Shared.Responses.DTOs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pos.Datalayer
{
    public interface IInvoiceRepository
    {
        Task<int> AddNewInvoice(InvoiceCreateDto dto);
        Task<InvoiceResponseDto?> GetInvoiceById(int id);
        Task<InvoiceResponseDto?> GetInvoiceByNumber(string invoiceNumber);
        Task<int> DeleteInvoice(int id);
        Task<int> RestoreInvoice(int id);
        Task<(List<InvoiceResponseDto> Invoices, int TotalCount)> GetAllInvoicesPaged(PaginationParams pagination);
        Task<int> UpdateInvoice(InvoiceResponseDto dto);
        Task<bool> CheckInvoiceNumberExists(string invoiceNumber, int? excludeId = null);
        Task<List<InvoiceResponseDto>> GetInvoicesByCustomerId(int customerId);
        Task<decimal> GetTotalSalesByDateRange(DateTime fromDate, DateTime toDate);
        Task<int> CreateInvoiceFullFlow(InvoiceCreateDto dto);
    }

    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly string _connectionString;

        public InvoiceRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                              ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<(List<InvoiceResponseDto> Invoices, int TotalCount)> GetAllInvoicesPaged(PaginationParams pagination)
        {
            Log.Information("DAL: Getting invoices page {Page}", pagination.PageNumber);

            var invoices = new List<InvoiceResponseDto>();
            int totalCount = 0;

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_GetAllInvoicesPaged", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pagination.PageNumber;
            cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pagination.PageSize;

            var totalCountParam = cmd.Parameters.Add("@TotalCount", SqlDbType.Int);
            totalCountParam.Direction = ParameterDirection.Output;

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                invoices.Add(MapToInvoiceDto(reader));
            }

            await reader.CloseAsync();

            if (totalCountParam.Value is int tc)
                totalCount = tc;

            Log.Information("DAL: Retrieved {Count} invoices", invoices.Count);

            return (invoices, totalCount);
        }

        public async Task<InvoiceResponseDto?> GetInvoiceById(int id)
        {
            Log.Information("DAL: Getting invoice by ID {Id}", id);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_GetInvoiceById", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                Log.Information("DAL: Invoice {Id} found", id);
                return MapToInvoiceDto(reader);
            }

            Log.Warning("DAL: Invoice {Id} not found", id);
            return null;
        }

        public async Task<InvoiceResponseDto?> GetInvoiceByNumber(string invoiceNumber)
        {
            Log.Information("DAL: Getting invoice by Number {InvoiceNumber}", invoiceNumber);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_GetInvoiceByNumber", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@InvoiceNumber", SqlDbType.NVarChar, 50).Value = invoiceNumber;

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                Log.Information("DAL: Invoice {InvoiceNumber} found", invoiceNumber);
                return MapToInvoiceDto(reader);
            }

            Log.Warning("DAL: Invoice {InvoiceNumber} not found", invoiceNumber);
            return null;
        }

        public async Task<int> AddNewInvoice(InvoiceCreateDto dto)
        {
            Log.Information("DAL: Adding new invoice {InvoiceNumber}", dto.InvoiceNumber);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_AddNewInvoice", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@InvoiceNumber", SqlDbType.NVarChar, 50).Value = dto.InvoiceNumber;
            cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = (object?)dto.CustomerId ?? DBNull.Value;
         
            cmd.Parameters.Add("@PaidAmount", SqlDbType.Decimal).Value = dto.PaidAmount;
            cmd.Parameters.Add("@Discount", SqlDbType.Decimal).Value = dto.Discount;
            cmd.Parameters.Add("@PaymentMethod", SqlDbType.Int).Value = dto.PaymentMethod;

            var newIdParam = cmd.Parameters.Add("@NewId", SqlDbType.Int);
            newIdParam.Direction = ParameterDirection.Output;

            var returnParam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParam.Direction = ParameterDirection.ReturnValue;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            int result = (int)returnParam.Value;
            int newId = (int)newIdParam.Value;

            // معالجة Race Condition / Duplicate
            if (result == -1)
            {
                Log.Warning("DAL: Invoice number {InvoiceNumber} already exists (Race Condition)", dto.InvoiceNumber);
                throw new InvalidOperationException($"Invoice number '{dto.InvoiceNumber}' already exists.");
            }

            Log.Information("DAL: Invoice created with ID {Id}", newId);
            return newId;
        }

        public async Task<int> UpdateInvoice(InvoiceResponseDto dto)
        {
            Log.Information("DAL: Updating invoice {Id}", dto.Id);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_UpdateInvoice", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = dto.Id;
            cmd.Parameters.Add("@InvoiceNumber", SqlDbType.NVarChar, 50).Value = dto.InvoiceNumber;
            cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = (object?)dto.CustomerId ?? DBNull.Value;
            cmd.Parameters.Add("@TotalAmount", SqlDbType.Decimal).Value = dto.TotalAmount;
            cmd.Parameters.Add("@PaidAmount", SqlDbType.Decimal).Value = dto.PaidAmount;
            cmd.Parameters.Add("@Discount", SqlDbType.Decimal).Value = dto.Discount;
            cmd.Parameters.Add("@PaymentMethod", SqlDbType.Int).Value = dto.PaymentMethod;

            var returnParam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParam.Direction = ParameterDirection.ReturnValue;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            int result = (int)returnParam.Value;

            // معالجة النتائج
            switch (result)
            {
                case 0:
                    Log.Warning("DAL: Invoice {Id} not found", dto.Id);
                    return 0; // Not Found

                case -2:
                    Log.Warning("DAL: Duplicate invoice number {InvoiceNumber} for another invoice", dto.InvoiceNumber);
                    throw new InvalidOperationException($"Invoice number '{dto.InvoiceNumber}' is already used by another invoice.");

                case 1:
                    Log.Information("DAL: Invoice {Id} updated successfully", dto.Id);
                    return 1; // Success

                default:
                    throw new InvalidOperationException("Unknown error occurred while updating invoice.");
            }
        }

        public async Task<int> DeleteInvoice(int id)
        {
            Log.Information("DAL: Deleting (soft) invoice {Id}", id);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_DeleteInvoice", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            var returnParam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParam.Direction = ParameterDirection.ReturnValue;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            int result = (int)returnParam.Value;

            Log.Information("DAL: Delete result for invoice {Id}: {Result}", id, result);

            return result;
        }

        public async Task<int> RestoreInvoice(int id)
        {
            Log.Information("DAL: Restoring invoice {Id}", id);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_RestoreInvoice", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            var returnParam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParam.Direction = ParameterDirection.ReturnValue;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            int result = (int)returnParam.Value;

            Log.Information("DAL: Restore result for invoice {Id}: {Result}", id, result);

            return result;
        }

        public async Task<bool> CheckInvoiceNumberExists(string invoiceNumber, int? excludeId = null)
        {
            Log.Information("DAL: Checking Invoice Number '{InvoiceNumber}' exists", invoiceNumber);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_CheckInvoiceNumberExists", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@InvoiceNumber", SqlDbType.NVarChar, 50).Value = invoiceNumber;
            cmd.Parameters.Add("@ExcludeId", SqlDbType.Int).Value = (object?)excludeId ?? DBNull.Value;

            var existsParam = cmd.Parameters.Add("@Exists", SqlDbType.Bit);
            existsParam.Direction = ParameterDirection.Output;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            bool exists = (bool)existsParam.Value;

            Log.Information("DAL: Invoice Number '{InvoiceNumber}' exists: {Exists}", invoiceNumber, exists);

            return exists;
        }

        public async Task<List<InvoiceResponseDto>> GetInvoicesByCustomerId(int customerId)
        {
            Log.Information("DAL: Getting invoices for customer {CustomerId}", customerId);

            var invoices = new List<InvoiceResponseDto>();

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_GetInvoicesByCustomerId", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customerId;

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                invoices.Add(MapToInvoiceDto(reader));
            }

            Log.Information("DAL: Retrieved {Count} invoices for customer {CustomerId}", invoices.Count, customerId);

            return invoices;
        }

        public async Task<decimal> GetTotalSalesByDateRange(DateTime fromDate, DateTime toDate)
        {
            Log.Information("DAL: Getting total sales from {FromDate} to {ToDate}", fromDate, toDate);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_GetTotalSalesByDateRange", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@FromDate", SqlDbType.DateTime).Value = fromDate;
            cmd.Parameters.Add("@ToDate", SqlDbType.DateTime).Value = toDate;

            var totalParam = cmd.Parameters.Add("@TotalSales", SqlDbType.Decimal);
            totalParam.Direction = ParameterDirection.Output;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            decimal totalSales = totalParam.Value == DBNull.Value ? 0 : (decimal)totalParam.Value;

            Log.Information("DAL: Total sales: {TotalSales}", totalSales);

            return totalSales;
        }

        private static InvoiceResponseDto MapToInvoiceDto(SqlDataReader reader)
        {
            return new InvoiceResponseDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                InvoiceNumber = reader.GetString(reader.GetOrdinal("InvoiceNumber")),
                CustomerId = reader.IsDBNull(reader.GetOrdinal("CustomerId")) ? null : reader.GetInt32(reader.GetOrdinal("CustomerId")),
                TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                PaidAmount = reader.GetDecimal(reader.GetOrdinal("PaidAmount")),
                Discount = reader.GetDecimal(reader.GetOrdinal("Discount")),
                PaymentMethod = reader.GetInt32(reader.GetOrdinal("PaymentMethod")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted"))
            };
        }
        public async Task<int> CreateInvoiceFullFlow(InvoiceCreateDto dto)
        {
            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_CreateInvoiceFullFlow", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            // 🔹 Params
            cmd.Parameters.Add("@InvoiceNumber", SqlDbType.NVarChar, 50).Value = dto.InvoiceNumber;
            cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = (object?)dto.CustomerId ?? DBNull.Value;
            cmd.Parameters.Add("@PaidAmount", SqlDbType.Decimal).Value = dto.PaidAmount;
            cmd.Parameters.Add("@Discount", SqlDbType.Decimal).Value = dto.Discount;
            cmd.Parameters.Add("@PaymentMethod", SqlDbType.Int).Value = dto.PaymentMethod;

            // 🔹 Table Type
            var table = new DataTable();
            table.Columns.Add("ProductId", typeof(int));
            table.Columns.Add("Quantity", typeof(int));
            table.Columns.Add("Price", typeof(decimal));

            foreach (var item in dto.Details)
            {
                table.Rows.Add(item.ProductId, item.Quantity, item.Price);
            }

            var detailsParam = cmd.Parameters.AddWithValue("@Details", table);
            detailsParam.SqlDbType = SqlDbType.Structured;
            detailsParam.TypeName = "dbo.InvoiceDetailsType";

            // 🔹 Output
            var newIdParam = cmd.Parameters.Add("@NewInvoiceId", SqlDbType.Int);
            newIdParam.Direction = ParameterDirection.Output;

            await conn.OpenAsync();

            var result = await cmd.ExecuteNonQueryAsync();

            if (result == -1)
                throw new InvalidOperationException("Invoice number already exists");

            return (int)newIdParam.Value;
        }
    }
}
