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
    public interface ICustomerRepository
    {
        Task<int> AddNewCustomer(CustomerSaveDto dto);
        Task<CustomerResponseDto?> GetCustomerById(int id);
        Task<int> UpdateCustomer(CustomerSaveDto dto);
        Task<int> DeleteCustomer(int id);
        Task<(List<CustomerResponseDto> Customers, int TotalCount)> GetAllCustomersPaged(PaginationParams pagination);
        Task<bool> CheckCustomerPhoneExists(string phone, int? excludeId = null);
        Task<bool> IsCustomerExistById(int id);
    }
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        public CustomerRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<(List<CustomerResponseDto> Customers, int TotalCount)> GetAllCustomersPaged(PaginationParams pagination)
        {
            Log.Information("DAL: Getting customers page {Page}", pagination.PageNumber);

            var customers = new List<CustomerResponseDto>();
            int totalCount = 0;

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_GetAllCustomersPaged", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pagination.PageNumber;
            cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pagination.PageSize;

            var totalCountParam = cmd.Parameters.Add("@TotalCount", SqlDbType.Int);
            totalCountParam.Direction = ParameterDirection.Output;

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                customers.Add(MapToCustomerDto(reader));
            }

            await reader.CloseAsync();

            if (totalCountParam.Value is int tc)
                totalCount = tc;

            return (customers, totalCount);
        }

        public async Task<CustomerResponseDto?> GetCustomerById(int id)
        {
            Log.Information("DAL: Getting customer {Id}", id);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_GetCustomerById", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
                return MapToCustomerDto(reader);

            return null;
        }

        public async Task<int> AddNewCustomer(CustomerSaveDto dto)
        {
            Log.Information("DAL: Adding new customer {Name}", dto.Name);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_AddNewCustomer", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 150).Value = dto.Name;
            cmd.Parameters.Add("@Phone", SqlDbType.NVarChar, 50).Value = (object?)dto.Phone ?? DBNull.Value;
            cmd.Parameters.Add("@Balance", SqlDbType.Decimal).Value = dto.Balance;

            var newIdParam = cmd.Parameters.Add("@NewId", SqlDbType.Int);
            newIdParam.Direction = ParameterDirection.Output;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return (int)newIdParam.Value;
        }

        public async Task<int> UpdateCustomer(CustomerSaveDto dto)
        {
            Log.Information("DAL: Updating customer {Id}", dto.Id);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_UpdateCustomer", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = dto.Id;
            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 150).Value = dto.Name;
            cmd.Parameters.Add("@Phone", SqlDbType.NVarChar, 50).Value = (object?)dto.Phone ?? DBNull.Value;
            cmd.Parameters.Add("@Balance", SqlDbType.Decimal).Value = dto.Balance;

            var returnParam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParam.Direction = ParameterDirection.ReturnValue;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return (int)returnParam.Value;
        }

        public async Task<int> DeleteCustomer(int id)
        {
            Log.Information("DAL: Deleting customer {Id}", id);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_DeleteCustomer", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            var returnParam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParam.Direction = ParameterDirection.ReturnValue;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return (int)returnParam.Value;
        }

        public async Task<bool> CheckCustomerPhoneExists(string phone, int? excludeId = null)
        {
            Log.Information("DAL: Checking customer phone {Phone}", phone);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_CheckCustomerPhoneExists", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Phone", SqlDbType.NVarChar, 50).Value = phone;
            cmd.Parameters.Add("@ExcludeId", SqlDbType.Int).Value = (object?)excludeId ?? DBNull.Value;

            var existsParam = cmd.Parameters.Add("@Exists", SqlDbType.Bit);
            existsParam.Direction = ParameterDirection.Output;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return (bool)existsParam.Value;
        }

        public async Task<bool> IsCustomerExistById(int id)
        {
            Log.Information("DAL: Checking customer exists {Id}", id);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_IsCustomerExistById", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            var existsParam = cmd.Parameters.Add("@Exists", SqlDbType.Bit);
            existsParam.Direction = ParameterDirection.Output;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return (bool)existsParam.Value;
        }

        private static CustomerResponseDto MapToCustomerDto(SqlDataReader reader)
        {
            return new CustomerResponseDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
                Balance = reader.GetDecimal(reader.GetOrdinal("Balance")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted"))
            };
        }
    }
}
