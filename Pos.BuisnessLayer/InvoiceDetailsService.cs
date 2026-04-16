using Pos.Datalayer;
using POS.Shared.Responses.Enums;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using POS.Shared.Responses.DTOs;

namespace Pos.BuisnessLayer
{
    public interface IInvoiceDetailsService
    {
        Task<OperationResult<int>> AddInvoiceDetails(List<InvoiceDetailSaveDto> details);
        Task<OperationResult<List<InvoiceDetailResponseDto>>> GetByInvoiceId(int invoiceId);
        Task<OperationResult<int>> DeleteByInvoiceId(int invoiceId);
    }
    public class InvoiceDetailsService : IInvoiceDetailsService
    {
        private readonly IInvoiceDetailsRepository _repo;

        public InvoiceDetailsService(IInvoiceDetailsRepository repo)
        {
            _repo = repo;
        }

        public async Task<OperationResult<int>> AddInvoiceDetails(List<InvoiceDetailSaveDto> details)
        {
            Log.Information("Service: Adding invoice details count {Count}", details?.Count);

            try
            {
                // ✅ Validation
                if (details == null || !details.Any())
                {
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "لا يوجد بيانات تفاصيل");
                }

                foreach (var item in details)
                {
                    if (item.ProductId <= 0 || item.Quantity <= 0 || item.Price <= 0)
                    {
                        return OperationResult<int>.FailureResult(
                            OperationStatus.InvalidData,
                            "بيانات غير صحيحة في تفاصيل الفاتورة");
                    }
                }

                int result = await _repo.AddInvoiceDetails(details);

                return result switch
                {
                    > 0 => OperationResult<int>.SuccessResult(
                        result,
                        "تم إضافة تفاصيل الفاتورة بنجاح"),

                    _ => OperationResult<int>.FailureResult(
                        OperationStatus.Failed,
                        "حدث خطأ أثناء الإضافة")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error adding invoice details");

                return OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ غير متوقع");
            }
        }

        public async Task<OperationResult<List<InvoiceDetailResponseDto>>> GetByInvoiceId(int invoiceId)
        {
            try
            {
                if (invoiceId <= 0)
                {
                    return OperationResult<List<InvoiceDetailResponseDto>>.FailureResult(
                        OperationStatus.InvalidData,
                        "رقم الفاتورة غير صالح");
                }

                var data = await _repo.GetInvoiceDetailsByInvoiceId(invoiceId);

                return OperationResult<List<InvoiceDetailResponseDto>>.SuccessResult(
                    data,
                    "تم جلب التفاصيل بنجاح");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error getting invoice details");

                return OperationResult<List<InvoiceDetailResponseDto>>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ أثناء جلب البيانات");
            }
        }

        public async Task<OperationResult<int>> DeleteByInvoiceId(int invoiceId)
        {
            try
            {
                if (invoiceId <= 0)
                {
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "رقم الفاتورة غير صالح");
                }

                int result = await _repo.DeleteInvoiceDetailsByInvoiceId(invoiceId);

                return result switch
                {
                    > 0 => OperationResult<int>.SuccessResult(
                        invoiceId,
                        "تم حذف التفاصيل بنجاح"),

                    _ => OperationResult<int>.FailureResult(
                        OperationStatus.Failed,
                        "حدث خطأ أثناء الحذف")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error deleting invoice details");

                return OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ غير متوقع");
            }
        }
    }
}

