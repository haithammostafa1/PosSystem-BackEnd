using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Pos.Datalayer.Connection;
using Pos.Datalayer.Dtos;
using Pos.Datalayer.Helpers;
using POS.Shared.Responses.Enums;
using Serilog;
using System.Data;
using POS.Shared.Responses.DTOs;
namespace Pos.Datalayer
{

   
  

    public interface IUserRepository
    {
        Task<int> AddNewUser(UserCreatedDto dto);
        Task<UserResponseDto?> GetUserById(int id);
        Task<int> DeleteUser(int id);
        Task<(List<UserResponseDto> Users, int TotalCount)> GetAllUsersPaged(PaginationParams pagination);
        Task<UserAuthDto?> GetUserByUsername(string username);
        Task<int> UpdateUser(UserResponseDto dto);
        Task<bool> CheckUsernameExists(string username, int? excludeId = null);
        Task<int> ChangeUserPassword(int userId, string newPasswordHash);
    }
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;


        public UserRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                              ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<(List<UserResponseDto> Users, int TotalCount)> GetAllUsersPaged(PaginationParams pagination)
        {
            Log.Information("DAL: Getting users page {Page}", pagination.PageNumber);

            var users = new List<UserResponseDto>();
            int totalCount = 0;

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_GetAllUsersPaged", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pagination.PageNumber;
            cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pagination.PageSize;

            var totalCountParam = cmd.Parameters.Add("@TotalCount", SqlDbType.Int);
            totalCountParam.Direction = ParameterDirection.Output;

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(MapToUserDto(reader));
            }

            await reader.CloseAsync();

            if (totalCountParam.Value is int tc)
                totalCount = tc;

            Log.Information("DAL: Retrieved {Count} users", users.Count);

            return (users, totalCount);
        }
        public async Task<UserResponseDto?> GetUserById(int id)
        {
            Log.Information("DAL: Getting user by ID {Id}", id);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_GetUserById", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                Log.Information("DAL: User {Id} found", id);
                return MapToUserDto(reader);
            }

            Log.Warning("DAL: User {Id} not found", id);
            return null;
        }

        public async Task<int> AddNewUser(UserCreatedDto dto)
        {
            Log.Information("DAL: Adding new user {FullName}", dto.FullName);

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

            return (int)newIdParam.Value;
        }

        public async Task<int> DeleteUser(int id)
        {
            Log.Information("DAL: Deleting user {Id}", id);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_DeleteUser", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            var returnParam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParam.Direction = ParameterDirection.ReturnValue;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            int result = (int)returnParam.Value;

            Log.Information("DAL: Delete result for user {Id}: {Result}", id, result);

            return result;
        }
        private static UserAuthDto MapToUserAuthDto(SqlDataReader reader)
        {
            return new UserAuthDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                Role = reader.GetString(reader.GetOrdinal("Role")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
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
             
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };
        }
        public async Task<UserAuthDto?> GetUserByUsername(string username)
        {
            Log.Information("DAL: Getting user by Username {Username}", username);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_GetUserByUsername", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 100).Value = username;

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapToUserAuthDto(reader);
            }

            return null;
        }
        public async Task<int> UpdateUser(UserResponseDto dto)
        {
            Log.Information("DAL: Updating user {Id}", dto.Id);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_UpdateUser", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = dto.Id;
            cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 100).Value = dto.Username;
            cmd.Parameters.Add("@FullName", SqlDbType.NVarChar, 150).Value = dto.FullName;
            cmd.Parameters.Add("@Role", SqlDbType.NVarChar, 50).Value = dto.Role;
            cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = dto.IsActive;

            var returnParam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParam.Direction = ParameterDirection.ReturnValue;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            int result = (int)returnParam.Value;

            Log.Information("DAL: Update result for user {Id}: {Result}", dto.Id, result);

            return result;
        }

        public async Task<bool> CheckUsernameExists(string username, int? excludeId = null)
        {
            Log.Information("DAL: Checking Username '{Username}' exists", username);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_CheckUsernameExists", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 100).Value = username;
            cmd.Parameters.Add("@ExcludeId", SqlDbType.Int).Value = (object?)excludeId ?? DBNull.Value;

            var existsParam = cmd.Parameters.Add("@Exists", SqlDbType.Bit);
            existsParam.Direction = ParameterDirection.Output;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            bool exists = (bool)existsParam.Value;

            Log.Information("DAL: Username '{Username}' exists: {Exists}", username, exists);

            return exists;
        }

        public async Task<int> ChangeUserPassword(int userId, string newPasswordHash)
        {
            Log.Information("DAL: Updating password for user {Id}", userId);

            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new("sp_ChangePassword", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
            cmd.Parameters.Add("@NewPasswordHash", SqlDbType.NVarChar, 255).Value = newPasswordHash;

            var returnParam = cmd.Parameters.Add("@Result", SqlDbType.Int);
            returnParam.Direction = ParameterDirection.Output;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            int result = (int)returnParam.Value;

            Log.Information("DAL: Password update result for user {Id}: {Result}", userId, result);

            return result;
        }

    } 
}
