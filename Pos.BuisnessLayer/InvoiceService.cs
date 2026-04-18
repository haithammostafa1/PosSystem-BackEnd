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
    public interface IInvoiceService
    {
        Task<OperationResult<int>> AddNewInvoice(InvoiceCreateDto dto);
        Task<OperationResult<int>> UpdateInvoice(InvoiceResponseDto dto);
        Task<OperationResult<int>> DeleteInvoice(int id);
        Task<OperationResult<int>> RestoreInvoice(int id);
        Task<OperationResult<InvoiceResponseDto>> GetInvoiceById(int id);
        Task<OperationResult<InvoiceResponseDto>> GetInvoiceByNumber(string invoiceNumber);
        Task<OperationResult<PagedList<InvoiceResponseDto>>> GetAllInvoicesPaged(PaginationParams pagination);
        Task<OperationResult<List<InvoiceResponseDto>>> GetInvoicesByCustomerId(int customerId);
        Task<OperationResult<decimal>> GetTotalSales(DateTime from, DateTime to);
        Task<OperationResult<int>> CreateInvoiceFullFlow(InvoiceCreateDto dto);
    }
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _repo;

        public InvoiceService(IInvoiceRepository repo)
        {
            _repo = repo;
        }
        public async Task<OperationResult<int>> CreateInvoiceFullFlow(InvoiceCreateDto dto)
        {
            Log.Information("Service: Creating full invoice {Number}", dto.InvoiceNumber);

            try
            {
                // ✅ 1. Basic Validation
                if (string.IsNullOrWhiteSpace(dto.InvoiceNumber))
                {
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "رقم الفاتورة مطلوب");
                }

                if (dto.Details == null || !dto.Details.Any())
                {
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "لا يوجد منتجات في الفاتورة");
                }


                if (dto.PaidAmount < 0)
                {
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "قيمة الدفع غير صحيحة");
                }

                // ✅ 2. Validate Details
                if (dto.Details.Any(x => x.ProductId <= 0 || x.Quantity <= 0 || x.Price <= 0))
                {
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "بيانات المنتجات غير صحيحة");
                }

                // ✅ 3. Optional: Validate Discount
                if (dto.Discount < 0)
                {
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "قيمة الخصم غير صحيحة");
                }

                // 🔥 4. Call Repository (SP)
                int result = await _repo.CreateInvoiceFullFlow(dto);

                // ✅ 5. Map Result
                return result switch
                {
                    > 0 => OperationResult<int>.SuccessResult(
                        result,
                        "تم إنشاء الفاتورة بنجاح"),

                    -1 => OperationResult<int>.FailureResult(
                        OperationStatus.DuplicateRecord,
                        "رقم الفاتورة موجود بالفعل"),

                    -3 => OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "الكمية غير متوفرة في المخزون"),

                    _ => OperationResult<int>.FailureResult(
                        OperationStatus.Failed,
                        "حدث خطأ أثناء إنشاء الفاتورة")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error creating full invoice");

                return OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ غير متوقع");
            }
        }

        public async Task<OperationResult<int>> AddNewInvoice(InvoiceCreateDto dto)
        {
            Log.Information("Service: Creating invoice {Number}", dto.InvoiceNumber);

            try
            {
              
                if (string.IsNullOrWhiteSpace(dto.InvoiceNumber))
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "رقم الفاتورة مطلوب");

                if (dto.Details.Any(x => x.Quantity <= 0 || x.Price <= 0))
                {
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "بيانات المنتجات غير صحيحة");
                }


                bool exists = await _repo.CheckInvoiceNumberExists(dto.InvoiceNumber);
                if (exists)
                    return OperationResult<int>.FailureResult(
                        OperationStatus.DuplicateRecord,
                        "رقم الفاتورة موجود بالفعل");

                int newId = await _repo.AddNewInvoice(dto);

                return OperationResult<int>.SuccessResult(
                    newId,
                    "تم إنشاء الفاتورة بنجاح");
            }
            catch (InvalidOperationException ex)
            {
                // Duplicate من DB
                return OperationResult<int>.FailureResult(
                    OperationStatus.DuplicateRecord,
                    ex.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error creating invoice");

                return OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ أثناء إنشاء الفاتورة");
            }
        }

        public async Task<OperationResult<int>> UpdateInvoice(InvoiceResponseDto dto)
        {
            try
            {
                if (dto.Id <= 0)
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "Id غير صالح");

                int result = await _repo.UpdateInvoice(dto);

                return result switch
                {
                    1 => OperationResult<int>.SuccessResult(dto.Id, "تم تعديل الفاتورة"),

                    0 => OperationResult<int>.FailureResult(
                        OperationStatus.NotFound,
                        "الفاتورة غير موجودة"),

                    _ => OperationResult<int>.FailureResult(
                        OperationStatus.Failed,
                        "خطأ أثناء التعديل")
                };
            }
            catch (InvalidOperationException ex)
            {
                return OperationResult<int>.FailureResult(
                    OperationStatus.DuplicateRecord,
                    ex.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error updating invoice");

                return OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ غير متوقع");
            }
        }

        public async Task<OperationResult<int>> DeleteInvoice(int id)
        {
            try
            {
                if (id <= 0)
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "رقم الفاتورة غير صالح");

                int result = await _repo.DeleteInvoice(id);

                return result switch
                {
                    1 => OperationResult<int>.SuccessResult(id, "تم حذف الفاتورة"),

                    0 => OperationResult<int>.FailureResult(
                        OperationStatus.NotFound,
                        "الفاتورة غير موجودة"),

                    _ => OperationResult<int>.FailureResult(
                        OperationStatus.Failed,
                        "خطأ أثناء الحذف")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error deleting invoice");

                return OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ غير متوقع");
            }
        }

        public async Task<OperationResult<int>> RestoreInvoice(int id)
        {
            try
            {
                if (id <= 0)
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "رقم الفاتورة غير صالح");

                int result = await _repo.RestoreInvoice(id);

                return result switch
                {
                    1 => OperationResult<int>.SuccessResult(id, "تم استرجاع الفاتورة"),

                    0 => OperationResult<int>.FailureResult(
                        OperationStatus.NotFound,
                        "الفاتورة غير موجودة"),

                    _ => OperationResult<int>.FailureResult(
                        OperationStatus.Failed,
                        "خطأ أثناء الاسترجاع")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error restoring invoice");

                return OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ غير متوقع");
            }
        }

        public async Task<OperationResult<InvoiceResponseDto>> GetInvoiceById(int id)
        {
            try
            {
                if (id <= 0)
                    return OperationResult<InvoiceResponseDto>.FailureResult(
                        OperationStatus.InvalidData,
                        "رقم الفاتورة غير صالح");

                var invoice = await _repo.GetInvoiceById(id);

                if (invoice == null)
                    return OperationResult<InvoiceResponseDto>.FailureResult(
                        OperationStatus.NotFound,
                        "الفاتورة غير موجودة");

                return OperationResult<InvoiceResponseDto>.SuccessResult(
                    invoice,
                    "تم جلب الفاتورة");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error getting invoice");

                return OperationResult<InvoiceResponseDto>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ أثناء جلب البيانات");
            }
        }

        public async Task<OperationResult<InvoiceResponseDto>> GetInvoiceByNumber(string invoiceNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(invoiceNumber))
                    return OperationResult<InvoiceResponseDto>.FailureResult(
                        OperationStatus.InvalidData,
                        "رقم الفاتورة غير صالح");

                var invoice = await _repo.GetInvoiceByNumber(invoiceNumber);

                if (invoice == null)
                    return OperationResult<InvoiceResponseDto>.FailureResult(
                        OperationStatus.NotFound,
                        "الفاتورة غير موجودة");

                return OperationResult<InvoiceResponseDto>.SuccessResult(
                    invoice,
                    "تم جلب الفاتورة");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error getting invoice by number");

                return OperationResult<InvoiceResponseDto>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ أثناء جلب البيانات");
            }
        }

        public async Task<OperationResult<PagedList<InvoiceResponseDto>>> GetAllInvoicesPaged(PaginationParams pagination)
        {
            try
            {
                var (data, totalCount) = await _repo.GetAllInvoicesPaged(pagination);

                var paged = new PagedList<InvoiceResponseDto>(
                    data,
                    totalCount,
                    pagination.PageNumber,
                    pagination.PageSize
                );

                return OperationResult<PagedList<InvoiceResponseDto>>.SuccessResult(
                    paged,
                    "تم جلب الفواتير");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error fetching invoices");

                return OperationResult<PagedList<InvoiceResponseDto>>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ أثناء جلب البيانات");
            }
        }

        public async Task<OperationResult<List<InvoiceResponseDto>>> GetInvoicesByCustomerId(int customerId)
        {
            try
            {
                if (customerId <= 0)
                    return OperationResult<List<InvoiceResponseDto>>.FailureResult(
                        OperationStatus.InvalidData,
                        "رقم العميل غير صالح");

                var list = await _repo.GetInvoicesByCustomerId(customerId);

                return OperationResult<List<InvoiceResponseDto>>.SuccessResult(
                    list,
                    "تم جلب الفواتير");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error fetching customer invoices");

                return OperationResult<List<InvoiceResponseDto>>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ أثناء جلب البيانات");
            }
        }

        public async Task<OperationResult<decimal>> GetTotalSales(DateTime from, DateTime to)
        {
            try
            {
                if (from > to)
                    return OperationResult<decimal>.FailureResult(
                        OperationStatus.InvalidData,
                        "تاريخ غير صالح");

                var total = await _repo.GetTotalSalesByDateRange(from, to);

                return OperationResult<decimal>.SuccessResult(
                    total,
                    "تم حساب الإجمالي");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error calculating sales");

                return OperationResult<decimal>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ أثناء الحساب");
            }
        }
    }
}
