using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Pos.Datalayer.Helpers;
using POS.Shared.Responses;
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
    public interface IProductRepository
    {
        Task<OperationResult<int>> AddNewProduct(ProductSaveDto dto);
        Task<OperationResult<int>> UpdateProduct(ProductSaveDto dto);
        Task<OperationResult<int>> DeleteProduct(int id);
        Task<OperationResult<ProductResponseDto>> GetProductById(int id);
        Task<OperationResult<PagedList<ProductResponseDto>>> GetAllProductsPaged(PaginationParams pagination);
        Task<OperationResult<int>> DecreaseProductQuantity(int productId, int quantityToSubtract);
        Task<OperationResult<int>> IncreaseProductQuantity(int productId, int quantityToAdd);
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

        public async Task<OperationResult<ProductResponseDto>> GetProductById(int id)
        {
            Log.Information("DAL: Getting product by ID {Id}", id);
            ProductResponseDto? product = null;

            try
            {
                using SqlConnection conn = new(_connectionString);
                using SqlCommand cmd = new("sp_GetProductById", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    product = MapToProductResponseDto(reader);
                    return OperationResult<ProductResponseDto>.SuccessResult(product, "تم جلب بيانات المنتج بنجاح");
                }

                Log.Warning("DAL: Product {Id} not found", id);
                return OperationResult<ProductResponseDto>.FailureResult(OperationStatus.NotFound, "المنتج غير موجود");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DAL: Error getting product {Id}", id);
                throw;
            }
        }

        public async Task<OperationResult<int>> AddNewProduct(ProductSaveDto dto)
        {
            Log.Information("DAL: Adding new product {Name}", dto.Name);
            try
            {
                using SqlConnection conn = new(_connectionString);
                using SqlCommand cmd = new("sp_AddNewProduct", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 150).Value = dto.Name;
                cmd.Parameters.Add("@Barcode", SqlDbType.NVarChar, 100).Value = (object?)dto.Barcode ?? DBNull.Value;
                cmd.Parameters.Add("@Price", SqlDbType.Decimal).Value = dto.Price;
                cmd.Parameters.Add("@Cost", SqlDbType.Decimal).Value = dto.Cost;
                cmd.Parameters.Add("@Quantity", SqlDbType.Int).Value = dto.Quantity;
                cmd.Parameters.Add("@CategoryId", SqlDbType.Int).Value = dto.CategoryId;

                var newIdParam = cmd.Parameters.Add("@NewId", SqlDbType.Int);
                newIdParam.Direction = ParameterDirection.Output;

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                int newId = (int)newIdParam.Value;

                if (newId == -1)
                {
                    Log.Warning("DAL: Barcode already exists");
                    return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "رقم الباركود مسجل لمنتج آخر");
                }
                if (newId > 0)
                {
                    return OperationResult<int>.SuccessResult(newId, "تم إضافة المنتج بنجاح");
                }

                return OperationResult<int>.FailureResult(OperationStatus.Failed, "حدث خطأ أثناء إضافة المنتج");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DAL: Error adding product");
                throw;
            }
        }

        public async Task<OperationResult<int>> UpdateProduct(ProductSaveDto dto)
        {
            Log.Information("DAL: Updating product {Id}", dto.Id);
            try
            {
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

                if (result == 1)
                    return OperationResult<int>.SuccessResult(dto.Id, "تم تعديل المنتج بنجاح");
                if (result == -1)
                    return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "الباركود مسجل لمنتج آخر");
                if (result == 0)
                    return OperationResult<int>.FailureResult(OperationStatus.NotFound, "المنتج غير موجود");

                return OperationResult<int>.FailureResult(OperationStatus.Failed, "خطأ غير متوقع");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DAL: Error updating product {Id}", dto.Id);
                throw;
            }
        }

        public async Task<OperationResult<int>> DeleteProduct(int id)
        {
            Log.Information("DAL: Deleting product {Id}", id);
            try
            {
                using SqlConnection conn = new(_connectionString);
                using SqlCommand cmd = new("sp_DeleteProduct", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                var returnParam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
                returnParam.Direction = ParameterDirection.ReturnValue;

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                int result = (int)returnParam.Value;

                if (result == 1)
                    return OperationResult<int>.SuccessResult(id, "تم حذف المنتج بنجاح");

                return OperationResult<int>.FailureResult(OperationStatus.NotFound, "المنتج غير موجود أو تم حذفه مسبقاً");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DAL: Error deleting product {Id}", id);
                throw;
            }
        }
        public async Task<OperationResult<PagedList<ProductResponseDto>>> GetAllProductsPaged(PaginationParams pagination)
        {
            Log.Information("DAL: Fetching paged products. Page: {Page}, Size: {Size}",
                pagination.PageNumber, pagination.PageSize);

            var products = new List<ProductResponseDto>();
            int totalCount = 0;

            try
            {
                using SqlConnection conn = new(_connectionString);
                using SqlCommand cmd = new("sp_GetAllProductsPaged", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pagination.PageNumber;
                cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pagination.PageSize;

                // بارامتر المخرجات لجلب العدد الكلي
                var totalCountParam = cmd.Parameters.Add("@TotalCount", SqlDbType.Int);
                totalCountParam.Direction = ParameterDirection.Output;

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    products.Add(MapToProductResponseDto(reader));
                }

                await reader.CloseAsync();

                if (totalCountParam.Value is int tc) totalCount = tc;

                var pagedData = new PagedList<ProductResponseDto>(products, totalCount, pagination.PageNumber, pagination.PageSize);

                return OperationResult<PagedList<ProductResponseDto>>.SuccessResult(pagedData, "تم جلب المنتجات بنجاح");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DAL: Error fetching paged products");
                throw;
            }
        }

        public async Task<OperationResult<int>> DecreaseProductQuantity(int productId, int quantityToSubtract)
        {
            Log.Information("DAL: Decreasing quantity for product {Id} by {Quantity}", productId, quantityToSubtract);

            try
            {
                using SqlConnection conn = new(_connectionString);
                using SqlCommand cmd = new("sp_DecreaseProductQuantity", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@ProductId", SqlDbType.Int).Value = productId;
                cmd.Parameters.Add("@QuantityToSubtract", SqlDbType.Int).Value = quantityToSubtract;

                // استقبال النتيجة المرتجعة من قاعدة البيانات (RETURN)
                var returnParam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
                returnParam.Direction = ParameterDirection.ReturnValue;

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                int result = (int)returnParam.Value;

                // 🌟 تطبيق الـ Result Pattern ببراعة
                return result switch
                {
                    1 => OperationResult<int>.SuccessResult(productId, "تم خصم الكمية بنجاح"),
                    -3 => OperationResult<int>.FailureResult(OperationStatus.InsufficientStock, "الكمية المتاحة في المخزن لا تكفي لإتمام العملية"),
                    _ => OperationResult<int>.FailureResult(OperationStatus.Failed, "حدث خطأ غير متوقع في قاعدة البيانات أثناء الخصم")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DAL: Error decreasing quantity for product {Id}", productId);
                throw;
            }
        }

        public async Task<OperationResult<int>> IncreaseProductQuantity(int productId, int quantityToAdd)
        {
            Log.Information("DAL: Increasing quantity for product {Id} by {Quantity}", productId, quantityToAdd);

            try
            {
                using SqlConnection conn = new(_connectionString);
                using SqlCommand cmd = new("sp_IncreaseProductQuantity", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@ProductId", SqlDbType.Int).Value = productId;
                cmd.Parameters.Add("@QuantityToAdd", SqlDbType.Int).Value = quantityToAdd;

                var returnParam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
                returnParam.Direction = ParameterDirection.ReturnValue;

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                int result = (int)returnParam.Value;

                return result switch
                {
                    1 => OperationResult<int>.SuccessResult(productId, "تم زيادة الكمية بنجاح"),
                    0 => OperationResult<int>.FailureResult(OperationStatus.NotFound, "المنتج غير موجود"),
                    _ => OperationResult<int>.FailureResult(OperationStatus.Failed, "حدث خطأ غير متوقع في قاعدة البيانات أثناء الزيادة")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DAL: Error increasing quantity for product {Id}", productId);
                throw;
            }
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
                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }
    }
}









