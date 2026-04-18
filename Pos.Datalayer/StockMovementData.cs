using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using POS.Shared.Responses.DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pos.Datalayer
{
    public interface IStockMovementRepository
    {
        Task<int> AddStockMovement(StockMovementDto dto);
        Task<List<StockMovementResponseDto>> GetByProductId(int productId);
 
    }
    public class StockMovementRepository : IStockMovementRepository
    {
        private readonly string _connectionString;

        public StockMovementRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<int> AddStockMovement(StockMovementDto dto)
        {
            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_AddStockMovement", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@ProductId", SqlDbType.Int).Value = dto.ProductId;
            cmd.Parameters.Add("@Quantity", SqlDbType.Int).Value = dto.Quantity;
            cmd.Parameters.Add("@Type", SqlDbType.Int).Value = dto.Type;
            cmd.Parameters.Add("@Reference", SqlDbType.NVarChar, 100)
                .Value = (object?)dto.Reference ?? DBNull.Value;

            await conn.OpenAsync();

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<StockMovementResponseDto>> GetByProductId(int productId)
        {
            var list = new List<StockMovementResponseDto>();

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_GetStockMovementsByProductId", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@ProductId", SqlDbType.Int).Value = productId;

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new StockMovementResponseDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                    Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                    Type = reader.GetInt32(reader.GetOrdinal("Type")),
                    Reference = reader.IsDBNull(reader.GetOrdinal("Reference"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Reference")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                });
            }

            return list;
        }
    }
}
