using Pos.Datalayer;
using Pos.Datalayer.Dtos;
using Pos.Datalayer.Helpers;
using POS.Shared.Responses.Enums;
using Serilog;

namespace Pos.BuisnessLayer
{

    public interface IUserService
    {
        Task<OperationResult<int>> AddNewUser(UserCreatedDto dto);
        Task<OperationResult<int>> DeleteUser(int id);
        Task<OperationResult<PagedList<UserResponseDto>>> GetAllUsersPaged(PaginationParams pagination);
        Task<OperationResult<UserResponseDto>> GetUserById(int id);
        Task<OperationResult<UserAuthDto>> GetUserByUsername(string Username);
        Task<OperationResult<int>> UpdateUser(UserResponseDto dto);
        Task<OperationResult<int>> ChangeUserPassword(int userId, string newPassword);



    }
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepsoitory)
        {
            _userRepository = userRepsoitory;
        }

        public async Task<OperationResult<PagedList<UserResponseDto>>> GetAllUsersPaged(PaginationParams pagination)
        {
           
            var (users, totalCount) = await _userRepository.GetAllUsersPaged(pagination);


            var pagedList = new PagedList<UserResponseDto>(
                users,
                totalCount,
                pagination.PageNumber,
                pagination.PageSize
            );

            return OperationResult<PagedList<UserResponseDto>>.SuccessResult(
                pagedList,
                "تم جلب المستخدمين بنجاح"
            );
        }
        public async Task<OperationResult<UserResponseDto>> GetUserById(int id)
        {
            if (id <= 0)
            {
                return OperationResult<UserResponseDto>.FailureResult(
                    OperationStatus.InvalidData,
                    "Id غير صالح");
            }

            var user = await _userRepository.GetUserById(id);

            if (user == null)
            {
                return OperationResult<UserResponseDto>.FailureResult(
                    OperationStatus.NotFound,
                    "المستخدم غير موجود");
            }

            return OperationResult<UserResponseDto>.SuccessResult(
                user,
                "تم جلب البيانات بنجاح");
        }
        public async Task<OperationResult<int>> AddNewUser(UserCreatedDto dto)
        {
          
            if (string.IsNullOrWhiteSpace(dto.Username))
            {
                return OperationResult<int>.FailureResult(
                    OperationStatus.InvalidData,
                    "اسم المستخدم مطلوب");
            }

            if (string.IsNullOrWhiteSpace(dto.PasswordHash))
            {
                return OperationResult<int>.FailureResult(
                    OperationStatus.InvalidData,
                    "كلمة المرور مطلوبة");
            }

            int result = await _userRepository.AddNewUser(dto);

           
            if (result == -1)
            {
                return OperationResult<int>.FailureResult(
                    OperationStatus.DuplicateRecord,
                    "الاسم موجود مسبقا");
            }

            if (result > 0)
            {
                return OperationResult<int>.SuccessResult(
                    result,
                    "تم إضافة المستخدم بنجاح");
            }

            return OperationResult<int>.FailureResult(
                OperationStatus.Failed,
                "حدث خطأ أثناء الإضافة");
        }



        public async Task<OperationResult<int>> DeleteUser(int id)
        {
            try
            {
                // ✅ Validation
                if (id <= 0)
                {
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "رقم المعرف غير صحيح");
                }

                int result = await _userRepository.DeleteUser(id);

              
                return result switch
                {
                    1 => OperationResult<int>.SuccessResult(id, "تم حذف المستخدم بنجاح"),

                    0 => OperationResult<int>.FailureResult(
                        OperationStatus.NotFound,
                        "المستخدم غير موجود"),

                    _ => OperationResult<int>.FailureResult(
                        OperationStatus.Failed,
                        "حدث خطأ أثناء الحذف")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service Layer: Error occurred while deleting user with Id: {Id}", id);

                return OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "حدث خطأ غير متوقع في خدمة المستخدمين");
            }
        }
        public async Task<OperationResult<bool>> CheckUsernameExists(string username, int? excludeId = null)
        {
            try
            {
                // ✅ Validation
                if (string.IsNullOrWhiteSpace(username))
                {
                    return OperationResult<bool>.FailureResult(
                        OperationStatus.InvalidData,
                        "اسم المستخدم مطلوب");
                }

                bool exists = await _userRepository.CheckUsernameExists(username, excludeId);

                return OperationResult<bool>.SuccessResult(
                    exists,
                    exists ? "اسم المستخدم مستخدم بالفعل" : "اسم المستخدم متاح");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service Layer: Error checking username {Username}", username);

                return OperationResult<bool>.FailureResult(
                    OperationStatus.Failed,
                    "حدث خطأ أثناء التحقق من اسم المستخدم");
            }
        }
        public async Task<OperationResult<int>> UpdateUser(UserResponseDto dto)
        {
         
            if (dto.Id <= 0)
            {
                return OperationResult<int>.FailureResult(
                    OperationStatus.InvalidData,
                    "Id غير صالح");
            }

            if (string.IsNullOrWhiteSpace(dto.Username))
            {
                return OperationResult<int>.FailureResult(
                    OperationStatus.InvalidData,
                    "اسم المستخدم مطلوب");
            }

            int result = await _userRepository.UpdateUser(dto);

            return result switch
            {
                1 => OperationResult<int>.SuccessResult(dto.Id, "تم تعديل المستخدم بنجاح"),

                -1 => OperationResult<int>.FailureResult(
                    OperationStatus.DuplicateRecord,
                    "اسم المستخدم مستخدم بالفعل"),

                0 => OperationResult<int>.FailureResult(
                    OperationStatus.NotFound,
                    "المستخدم غير موجود"),

                _ => OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "حدث خطأ أثناء التعديل")
            };
        }
        public async Task<OperationResult<int>> ChangeUserPassword(int userId, string newPassword)
        {
            try
            {
                // ✅ Validation
                if (userId <= 0)
                {
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "رقم المستخدم غير صالح");
                }

                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                {
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "كلمة المرور يجب أن تكون 6 أحرف على الأقل");
                }

                // ✅ Hashing
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);

                // ✅ DAL call
                int result = await _userRepository.ChangeUserPassword(userId, hashedPassword);

                // ✅ Business Logic هنا
                return result switch
                {
                    1 => OperationResult<int>.SuccessResult(userId, "تم تغيير كلمة المرور بنجاح"),

                    0 => OperationResult<int>.FailureResult(
                        OperationStatus.NotFound,
                        "المستخدم غير موجود"),

                    _ => OperationResult<int>.FailureResult(
                        OperationStatus.Failed,
                        "حدث خطأ أثناء تغيير كلمة المرور")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error changing password for user {Id}", userId);

                return OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "حدث خطأ غير متوقع");
            }
        }

        public async Task<OperationResult<UserAuthDto>> GetUserByUsername(string username)
        {
            try
            {
              
                if (string.IsNullOrWhiteSpace(username))
                {
                    return OperationResult<UserAuthDto>.FailureResult(
                        OperationStatus.InvalidData,
                        "اسم المستخدم مطلوب");
                }

           
                var user = await _userRepository.GetUserByUsername(username);

                if (user == null)
                {
                    return OperationResult<UserAuthDto>.FailureResult(
                        OperationStatus.NotFound,
                        "المستخدم غير موجود");
                }

                return OperationResult<UserAuthDto>.SuccessResult(
                    user,
                    "تم جلب بيانات المستخدم بنجاح");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error getting user by username {Username}", username);

                return OperationResult<UserAuthDto>.FailureResult(
                    OperationStatus.Failed,
                    "حدث خطأ غير متوقع");
            }
        }

    }
}