using Microsoft.Data.SqlClient;
using Pos.Datalayer.Connection;
using Pos.Datalayer.Dtos;
using System.Data;
using Serilog;
using POS.Shared.Responses;
using Microsoft.Extensions.Configuration;
namespace Pos.Datalayer
{


    public interface IUserRepository
    {
        Task<OperationResult<int>> AddNewUser(UserCreatedDto dto);

    }
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;


        public UserRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                              ?? throw new InvalidOperationException("Connection string not found.");
        }

        public  async Task<OperationResult<int>> AddNewUser(UserCreatedDto dto)
        {
            Log.Information("DAL: Adding new user {FullName}", dto.FullName);
            try
            {
                using SqlConnection conn = new(_connectionString);
                using SqlCommand cmd = new("sp_AddNewUser", conn);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 100).Value = dto.Username;
                cmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 255).Value = dto.PasswordHash;
                cmd.Parameters.Add("@FullName", SqlDbType.NVarChar, 150).Value = dto.FullName;
                cmd.Parameters.Add("@Role", SqlDbType.NVarChar, 50).Value = dto.Role;
                cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = dto.IsActive;


                var newIdParam = cmd.Parameters.Add("@NewId", SqlDbType.Int);
                newIdParam.Direction = ParameterDirection.Output;

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                int newId = (int)newIdParam.Value;
                if (newId == -1)
                {
                    Log.Warning("DAL: Username already exists");
                    return OperationResult<int>.FailureResult(UserOperationResult.DuplicateUsername, "الاسم موجود مسبقا");
                }
                if (newId > 0)
                {
                    Log.Information("DAL: User added successfully. Id: {Id}", newId);
                    return OperationResult<int>.SuccessResult(newId, "تم إضافة المستخدم بنجاح");
                }
                return OperationResult<int>.FailureResult(UserOperationResult.Failed, "حدث خطأ غير معروف أثناء الإضافة");

            }
            catch (Exception ex)
            {
                Log.Error(ex, "DAL: Error adding user");
                throw;
            }
        }
        private static UserAuthDto MapToUserAuthDto(SqlDataReader reader)
        {
            return new UserAuthDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                Role = reader.GetString(reader.GetOrdinal("Role")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),

                RefreshTokenHash = reader.IsDBNull(reader.GetOrdinal("RefreshTokenHash")) ? null : reader.GetString(reader.GetOrdinal("RefreshTokenHash")),
                RefreshTokenExpiresAt = reader.IsDBNull(reader.GetOrdinal("RefreshTokenExpiresAt")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("RefreshTokenExpiresAt")),
                RefreshTokenRevokedAt = reader.IsDBNull(reader.GetOrdinal("RefreshTokenRevokedAt")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("RefreshTokenRevokedAt"))
            };
        }
        private static UserResponseDto MapToUserDto(SqlDataReader reader)
        {
            return new UserResponseDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                Role = reader.GetString(reader.GetOrdinal("Role")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };
        }






    } 
}
