using Pos.Datalayer;
using Pos.Datalayer.Helpers;
using POS.Shared.Responses.DTOs;
using POS.Shared.Responses.Enums;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pos.BuisnessLayer
{
    public interface ICustomerService
    {
        Task<OperationResult<int>> AddNewCustomer(CustomerSaveDto dto);
        Task<OperationResult<int>> UpdateCustomer(CustomerSaveDto dto);
        Task<OperationResult<int>> DeleteCustomer(int id);
        Task<OperationResult<CustomerResponseDto>> GetCustomerById(int id);
        Task<OperationResult<PagedList<CustomerResponseDto>>> GetAllCustomersPaged(PaginationParams pagination);
        Task<bool> CheckCustomerPhoneExists(string phone, int? excludeId = null);
        Task<bool> IsCustomerExistById(int id);
    }

    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;

        public CustomerService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<OperationResult<int>> AddNewCustomer(CustomerSaveDto dto)
        {
            Log.Information("Service: Validating new customer {Name}", dto.Name);

            try
            {
                // ✅ Validation
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "اسم العميل مطلوب");

                if (dto.Balance < 0)
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "الرصيد لا يمكن أن يكون بالسالب");

                dto.Name = dto.Name.Trim();

                if (!string.IsNullOrWhiteSpace(dto.Phone))
                {
                    bool exists = await _customerRepository.CheckCustomerPhoneExists(dto.Phone);
                    if (exists)
                        return OperationResult<int>.FailureResult(
                            OperationStatus.DuplicateRecord,
                            "رقم الهاتف مستخدم بالفعل");
                }

                int result = await _customerRepository.AddNewCustomer(dto);

                return result switch
                {
                    > 0 => OperationResult<int>.SuccessResult(result, "تم إضافة العميل بنجاح"),

                    -2 => OperationResult<int>.FailureResult(
                        OperationStatus.DuplicateRecord,
                        "رقم الهاتف مستخدم بالفعل"),

                    _ => OperationResult<int>.FailureResult(
                        OperationStatus.Failed,
                        "حدث خطأ أثناء إضافة العميل")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error adding customer");

                return OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ غير متوقع");
            }
        }

        public async Task<OperationResult<int>> UpdateCustomer(CustomerSaveDto dto)
        {
            try
            {
                if (dto.Id <= 0)
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "Id غير صالح");

                if (string.IsNullOrWhiteSpace(dto.Name))
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "اسم العميل مطلوب");

                if (dto.Balance < 0)
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "الرصيد لا يمكن أن يكون بالسالب");

                dto.Name = dto.Name.Trim();

                int result = await _customerRepository.UpdateCustomer(dto);

                return result switch
                {
                    1 => OperationResult<int>.SuccessResult(dto.Id, "تم تعديل العميل بنجاح"),

                    -2 => OperationResult<int>.FailureResult(
                        OperationStatus.DuplicateRecord,
                        "رقم الهاتف مستخدم بالفعل"),

                    -1 => OperationResult<int>.FailureResult(
                        OperationStatus.NotFound,
                        "العميل غير موجود"),

                    _ => OperationResult<int>.FailureResult(
                        OperationStatus.Failed,
                        "حدث خطأ أثناء التعديل")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error updating customer {Id}", dto.Id);

                return OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ غير متوقع");
            }
        }

        public async Task<OperationResult<int>> DeleteCustomer(int id)
        {
            try
            {
                if (id <= 0)
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "رقم العميل غير صالح");

                int result = await _customerRepository.DeleteCustomer(id);

                return result switch
                {
                    1 => OperationResult<int>.SuccessResult(id, "تم حذف العميل بنجاح"),

                    -1 => OperationResult<int>.FailureResult(
                        OperationStatus.NotFound,
                        "العميل غير موجود"),

                    _ => OperationResult<int>.FailureResult(
                        OperationStatus.Failed,
                        "حدث خطأ أثناء الحذف")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error deleting customer {Id}", id);

                return OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ غير متوقع");
            }
        }

        public async Task<OperationResult<CustomerResponseDto>> GetCustomerById(int id)
        {
            try
            {
                if (id <= 0)
                    return OperationResult<CustomerResponseDto>.FailureResult(
                        OperationStatus.InvalidData,
                        "رقم العميل غير صالح");

                var customer = await _customerRepository.GetCustomerById(id);

                if (customer == null)
                    return OperationResult<CustomerResponseDto>.FailureResult(
                        OperationStatus.NotFound,
                        "العميل غير موجود");

                return OperationResult<CustomerResponseDto>.SuccessResult(
                    customer,
                    "تم جلب العميل بنجاح");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error getting customer {Id}", id);

                return OperationResult<CustomerResponseDto>.FailureResult(
                    OperationStatus.Failed,
                    "حدث خطأ أثناء جلب العميل");
            }
        }

        public async Task<OperationResult<PagedList<CustomerResponseDto>>> GetAllCustomersPaged(PaginationParams pagination)
        {
            try
            {
                var (customers, totalCount) =
                    await _customerRepository.GetAllCustomersPaged(pagination);

                var pagedData = new PagedList<CustomerResponseDto>(
                    customers,
                    totalCount,
                    pagination.PageNumber,
                    pagination.PageSize
                );

                return OperationResult<PagedList<CustomerResponseDto>>.SuccessResult(
                    pagedData,
                    "تم جلب العملاء بنجاح");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error fetching customers");

                return OperationResult<PagedList<CustomerResponseDto>>.FailureResult(
                    OperationStatus.Failed,
                    "حدث خطأ أثناء جلب العملاء");
            }
        }

        public async Task<bool> CheckCustomerPhoneExists(string phone, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            return await _customerRepository.CheckCustomerPhoneExists(phone, excludeId);
        }

        public async Task<bool> IsCustomerExistById(int id)
        {
            if (id <= 0)
                return false;

            return await _customerRepository.IsCustomerExistById(id);
        }
    }
}
