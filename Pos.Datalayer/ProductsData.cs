using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Pos.Datalayer.Helpers;
using POS.Shared.Responses.DTOs;
using POS.Shared.Responses.Enums;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pos.Datalayer
{
    public interface IProductRepository
    {      
        Task<int> AddNewProduct(ProductSaveDto dto);
        Task<int> UpdateProduct(ProductSaveDto dto);
        Task<int> DeleteProduct(int id);
        Task<ProductResponseDto?> GetProductById(int id);
        Task<(List<ProductResponseDto> Products, int TotalCount)> GetAllProductsPaged(PaginationParams pagination);
        Task<int> DecreaseProductQuantity(
            int productId,
            int quantityToSubtract,
            StockMovementType movementType,
            string? reference = null);
        Task<int> IncreaseProductQuantity(
            int productId,
            int quantityToAdd,
            StockMovementType movementType,
            string? reference = null);

        Task<bool> CheckProductNameExists(string name, int? excludeId = null);
        Task<bool> IsProductExistById(int id);
    }
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<ProductResponseDto?> GetProductById(int id)//
        {
            Log.Information("DAL: Getting product by ID {Id}", id);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_GetProductById", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapToProductResponseDto(reader);
            }

            Log.Warning("DAL: Product {Id} not found", id);
            return null;
        }

        public async Task<int> AddNewProduct(ProductSaveDto dto)
        {
            Log.Information("DAL: Adding new product {Name}", dto.Name);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_AddNewProduct", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 150).Value = dto.Name;
            cmd.Parameters.Add("@Barcode", SqlDbType.NVarChar, 100).Value = (object?)dto.Barcode ?? DBNull.Value;
            cmd.Parameters.Add("@Price", SqlDbType.Decimal).Value = dto.Price;
            cmd.Parameters.Add("@Cost", SqlDbType.Decimal).Value = dto.Cost;
            cmd.Parameters.Add("@Quantity", SqlDbType.Int).Value = dto.Quantity;
            cmd.Parameters.Add("@CategoryId", SqlDbType.Int).Value = dto.CategoryId;

            // ✅ Output parameter
            var newIdParam = cmd.Parameters.Add("@NewId", SqlDbType.Int);
            newIdParam.Direction = ParameterDirection.Output;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            int result = (int)newIdParam.Value;

            Log.Information("DAL: Add product result: {Result}", result);

            return result;
        }//
        public async Task<int> UpdateProduct(ProductSaveDto dto)
        {
            Log.Information("DAL: Updating product {Id}", dto.Id);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_UpdateProduct", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = dto.Id;
            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 150).Value = dto.Name;
            cmd.Parameters.Add("@Barcode", SqlDbType.NVarChar, 100).Value = (object?)dto.Barcode ?? DBNull.Value;
            cmd.Parameters.Add("@Price", SqlDbType.Decimal).Value = dto.Price;
            cmd.Parameters.Add("@Cost", SqlDbType.Decimal).Value = dto.Cost;
            cmd.Parameters.Add("@Quantity", SqlDbType.Int).Value = dto.Quantity;
            cmd.Parameters.Add("@CategoryId", SqlDbType.Int).Value = dto.CategoryId;

            var returnParam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParam.Direction = ParameterDirection.ReturnValue;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            int result = (int)returnParam.Value;

            Log.Information("DAL: Update product result {Result}", result);

            return result;
        }

        public async Task<int> DeleteProduct(int id)
        {
            Log.Information("DAL: Deleting product {Id}", id);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_DeleteProduct", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            var returnParam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParam.Direction = ParameterDirection.ReturnValue;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            int result = (int)returnParam.Value;

            Log.Information("DAL: Delete product result {Result}", result);

            return result;
        }
        public async Task<(List<ProductResponseDto> Products, int TotalCount)> GetAllProductsPaged(PaginationParams pagination)
        {
            Log.Information("DAL: Fetching paged products. Page: {Page}, Size: {Size}",
                pagination.PageNumber, pagination.PageSize);

            var products = new List<ProductResponseDto>();
            int totalCount = 0;

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_GetAllProductsPaged", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pagination.PageNumber;
            cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pagination.PageSize;

            var totalCountParam = cmd.Parameters.Add("@TotalCount", SqlDbType.Int);
            totalCountParam.Direction = ParameterDirection.Output;

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                products.Add(MapToProductResponseDto(reader));
            }

            await reader.CloseAsync();

            if (totalCountParam.Value is int tc)
                totalCount = tc;

            return (products, totalCount);
        }

        public async Task<int> DecreaseProductQuantity(int productId, int quantityToSubtract,
             StockMovementType movementType, string? reference = null)

        {
            Log.Information("DAL: Decreasing quantity for product {Id} by {Quantity}", productId, quantityToSubtract);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_DecreaseProductQuantity", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@ProductId", SqlDbType.Int).Value = productId;
            cmd.Parameters.Add("@QuantityToSubtract", SqlDbType.Int).Value = quantityToSubtract;
            cmd.Parameters.Add("@MovementType", SqlDbType.Int).Value = (int)movementType;
            cmd.Parameters.Add("@Reference", SqlDbType.NVarChar, 100).Value = (object?)reference ?? DBNull.Value;

            var returnParam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParam.Direction = ParameterDirection.ReturnValue;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            int result = (int)returnParam.Value;

            Log.Information("DAL: Decrease quantity result {Result}", result);

            return result;
        }

        public async Task<int> IncreaseProductQuantity(
      int productId,
      int quantityToAdd,
      StockMovementType movementType,
      string? reference = null)
        {
            Log.Information("DAL: Increasing quantity for product {Id} by {Quantity}", productId, quantityToAdd);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_IncreaseProductQuantity", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@ProductId", SqlDbType.Int).Value = productId;
            cmd.Parameters.Add("@QuantityToAdd", SqlDbType.Int).Value = quantityToAdd;
            cmd.Parameters.Add("@MovementType", SqlDbType.Int).Value = (int)movementType;
            cmd.Parameters.Add("@Reference", SqlDbType.NVarChar, 100).Value = (object?)reference ?? DBNull.Value;

            var returnParam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParam.Direction = ParameterDirection.ReturnValue;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            int result = (int)returnParam.Value;

            Log.Information("DAL: Increase quantity result {Result}", result);

            return result;
        }
        public async Task<bool> CheckProductNameExists(string name, int? excludeId = null)
        {
            Log.Information("DAL: Checking if Product Name '{Name}' exists", name);

            try
            {
                using SqlConnection conn = new(_connectionString);
                using SqlCommand cmd = new("sp_CheckProductNameExists", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 150).Value = name;
                cmd.Parameters.Add("@ExcludeId", SqlDbType.Int).Value = (object?)excludeId ?? DBNull.Value;

                var existsParam = cmd.Parameters.Add("@Exists", SqlDbType.Bit);
                existsParam.Direction = ParameterDirection.Output;

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                bool exists = (bool)existsParam.Value;

                if (exists)
                    Log.Warning("DAL: Product Name '{Name}' already exists", name);

                return exists;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DAL: Error checking Product Name");
                throw;
            }
        }

        public async Task<bool> IsProductExistById(int id)
        {
            Log.Information("DAL: Checking if Product ID {Id} exists", id);

            try
            {
                using SqlConnection conn = new(_connectionString);
                using SqlCommand cmd = new("sp_IsProductExistById", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                var existsParam = cmd.Parameters.Add("@Exists", SqlDbType.Bit);
                existsParam.Direction = ParameterDirection.Output;

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                bool exists = (bool)existsParam.Value;
                return exists;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DAL: Error checking if Product exists by ID");
                throw;
            }
        }
        private static ProductResponseDto MapToProductResponseDto(SqlDataReader reader)
        {
            return new ProductResponseDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Barcode = reader.IsDBNull(reader.GetOrdinal("Barcode")) ? null : reader.GetString(reader.GetOrdinal("Barcode")),
                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                Cost = reader.GetDecimal(reader.GetOrdinal("Cost")),
                Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName")) ? "بدون قسم" : reader.GetString(reader.GetOrdinal("CategoryName")),
                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }
    }
}









