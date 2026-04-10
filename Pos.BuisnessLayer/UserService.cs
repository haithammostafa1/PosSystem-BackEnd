using Pos.Datalayer;
using Pos.Datalayer.Dtos;
using POS.Shared.Responses;
using static Pos.BuisnessLayer.Enums.Users;
namespace Pos.BuisnessLayer
{

    public interface IUserService
    {
        Task<OperationResult<int>> AddNewUser(UserCreatedDto dto);


    }
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepsoitory;

        public UserService(IUserRepository userRepsoitory)
        {
            _userRepsoitory = userRepsoitory;
        }
        public async Task<OperationResult<int>> AddNewUser(UserCreatedDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.PasswordHash))
                    return OperationResult<int>.FailureResult(UserOperationResult.InvalidData, "حقل كلمه المرور مطلوبه");

                string UserPassword = dto.PasswordHash;
                string hashedPaaword = BCrypt.Net.BCrypt.HashPassword(UserPassword);

                dto.PasswordHash = hashedPaaword;

                var result = await _userRepsoitory.AddNewUser(dto);

                return result;

            }
            catch
            (Exception ex)
            {
                return OperationResult<int>.FailureResult(UserOperationResult.Failed, "UserService  حدث خطأ غير متوقع في    ");

            }


        }
    }
}