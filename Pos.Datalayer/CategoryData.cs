using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Pos.Datalayer.Helpers;
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
    public interface ICategoryRepository
    {
        Task<int> AddNewCategory(CategorySaveDto dto);
        Task<CategoryResponseDto?> GetCategoryById(int id);
        Task<int> UpdateCategory(CategorySaveDto dto);
        Task<int> DeleteCategory(int id);
        Task<(List<CategoryResponseDto> Categories, int TotalCount)> GetAllCategoriesPaged(PaginationParams pagination);
        Task<bool> CheckCategoryNameExists(string name, int? excludeId = null);
        Task<bool> IsCategoryExistById(int id);
    }
    public class CategoryRepository : ICategoryRepository
    {
        private readonly string _connectionString;

        public CategoryRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<(List<CategoryResponseDto> Categories, int TotalCount)> GetAllCategoriesPaged(PaginationParams pagination)
        {
            Log.Information("DAL: Getting categories page {Page}", pagination.PageNumber);

            var categories = new List<CategoryResponseDto>();
            int totalCount = 0;

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_GetAllCategoriesPaged", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pagination.PageNumber;
            cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pagination.PageSize;

            var totalCountParam = cmd.Parameters.Add("@TotalCount", SqlDbType.Int);
            totalCountParam.Direction = ParameterDirection.Output;

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                categories.Add(MapToCategoryDto(reader));
            }

            await reader.CloseAsync();

            if (totalCountParam.Value is int tc)
                totalCount = tc;

            return (categories, totalCount);
        }

        public async Task<CategoryResponseDto?> GetCategoryById(int id)
        {
            Log.Information("DAL: Getting category by ID {Id}", id);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_GetCategoryById", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
                return MapToCategoryDto(reader);

            return null;
        }

        public async Task<int> AddNewCategory(CategorySaveDto dto)
        {
            Log.Information("DAL: Adding new category {Name}", dto.Name);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_AddNewCategory", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 150).Value = dto.Name;

            var newIdParam = cmd.Parameters.Add("@NewId", SqlDbType.Int);
            newIdParam.Direction = ParameterDirection.Output;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return (int)newIdParam.Value;
        }

        public async Task<int> UpdateCategory(CategorySaveDto dto)
        {
            Log.Information("DAL: Updating category {Id}", dto.Id);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_UpdateCategory", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = dto.Id;
            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 150).Value = dto.Name;

            var returnParam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParam.Direction = ParameterDirection.ReturnValue;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return (int)returnParam.Value;
        }

        public async Task<int> DeleteCategory(int id)
        {
            Log.Information("DAL: Deleting category {Id}", id);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_DeleteCategory", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            var returnParam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParam.Direction = ParameterDirection.ReturnValue;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return (int)returnParam.Value;
        }

        private static CategoryResponseDto MapToCategoryDto(SqlDataReader reader)
        {
            return new CategoryResponseDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted"))
            };
        }
        public async Task<bool> CheckCategoryNameExists(string name, int? excludeId = null)
        {
            Log.Information("DAL: Checking category name '{Name}' exists", name);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_CheckCategoryNameExists", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 150).Value = name;
            cmd.Parameters.Add("@ExcludeId", SqlDbType.Int).Value = (object?)excludeId ?? DBNull.Value;

            var existsParam = cmd.Parameters.Add("@Exists", SqlDbType.Bit);
            existsParam.Direction = ParameterDirection.Output;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            bool exists = (bool)existsParam.Value;

            Log.Information("DAL: Category name '{Name}' exists: {Exists}", name, exists);

            return exists;
        }
        public async Task<bool> IsCategoryExistById(int id)
        {
            Log.Information("DAL: Checking category {Id} exists", id);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_IsCategoryExistById", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            var existsParam = cmd.Parameters.Add("@Exists", SqlDbType.Bit);
            existsParam.Direction = ParameterDirection.Output;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            bool exists = (bool)existsParam.Value;

            Log.Information("DAL: Category {Id} exists: {Exists}", id, exists);

            return exists;
        }


    }



}
