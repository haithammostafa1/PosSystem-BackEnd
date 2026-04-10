using Pos.Datalayer;
using Pos.Datalayer.Dtos;
using POS.Shared.Responses;
using Serilog;
using static Pos.BuisnessLayer.Enums.Users;
namespace Pos.BuisnessLayer
{

    public interface IUserService
    {
        Task<OperationResult<int>> AddNewUser(UserCreatedDto dto);
        Task<OperationResult<int>> DeleteUser(int id);

    }
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepsoitory)
        {
            _userRepository = userRepsoitory;
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

                var result = await _userRepository.AddNewUser(dto);

                return result;

            }
            catch
            (Exception ex)
            {
                return OperationResult<int>.FailureResult(UserOperationResult.Failed, "UserService  حدث خطأ غير متوقع في    ");

            }


        }
        public async Task<OperationResult<int>> DeleteUser(int id)
        {
            try
            {
               
                if (id <= 0)
                {
                    return OperationResult<int>.FailureResult(UserOperationResult.InvalidData, "رقم المعرف غير صحيح، يرجى المحاولة مرة أخرى");
                }

             
                var result = await _userRepository.DeleteUser(id); // تصحيح الإملاء

              
                return result;
            }
            catch (Exception ex)
            {
               
                Log.Error(ex, "Service Layer: Error occurred while deleting user with Id: {Id}", id);

           
                return OperationResult<int>.FailureResult(UserOperationResult.Failed, "حدث خطأ غير متوقع في خدمة المستخدمين");
            }
        }
    }
}