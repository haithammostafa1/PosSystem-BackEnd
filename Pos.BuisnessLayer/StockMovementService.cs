using Pos.Datalayer;
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
    public interface IStockMovementService
    {
        Task<OperationResult<int>> AddStockMovement(StockMovementDto dto);
        Task<OperationResult<List<StockMovementResponseDto>>> GetByProductId(int productId);
    }
    public class StockMovementService : IStockMovementService
    {
        private readonly IStockMovementRepository _repo;

        public StockMovementService(IStockMovementRepository repo)
        {
            _repo = repo;
        }

        public async Task<OperationResult<int>> AddStockMovement(StockMovementDto dto)
        {
            try
            {
                if (dto.ProductId <= 0 || dto.Quantity <= 0)
                {
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "بيانات غير صحيحة");
                }

                int result = await _repo.AddStockMovement(dto);

                return OperationResult<int>.SuccessResult(
                    result,
                    "تم تسجيل حركة المخزون");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error adding stock movement");

                return OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ أثناء تسجيل الحركة");
            }
        }

        public async Task<OperationResult<List<StockMovementResponseDto>>> GetByProductId(int productId)
        {
            try
            {
                if (productId <= 0)
                {
                    return OperationResult<List<StockMovementResponseDto>>.FailureResult(
                        OperationStatus.InvalidData,
                        "ProductId غير صالح");
                }

                var data = await _repo.GetByProductId(productId);

                return OperationResult<List<StockMovementResponseDto>>.SuccessResult(
                    data,
                    "تم جلب الحركات بنجاح");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error getting stock movements");

                return OperationResult<List<StockMovementResponseDto>>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ أثناء جلب البيانات");
            }
        }
    }
}
