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

    public interface IProductService
    {
        Task<OperationResult<int>> AddNewProduct(ProductSaveDto dto);//
        Task<OperationResult<int>> UpdateProduct(ProductSaveDto dto);
        Task<OperationResult<int>> DeleteProduct(int id);
        Task<OperationResult<ProductResponseDto>> GetProductById(int id);//
        Task<OperationResult<PagedList<ProductResponseDto>>> GetAllProductsPaged(PaginationParams pagination);
        Task<OperationResult<int>> IncreaseProductQuantity(int productId, int quantityToAdd, StockMovementType movementType, string? reference = null);
        Task<bool> CheckProductNameExists(string name, int? excludeId = null);
        Task<bool> IsProductExistById(int id);
        Task<OperationResult<int>> DecreaseProductQuantity(int productId, int quantityToSubtract, StockMovementType movementType, string? reference = null);
    }





    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }
        public async Task<OperationResult<int>> AddNewProduct(ProductSaveDto dto)
        {
            Log.Information("Service: Validating new product {Name}", dto.Name);

            try
            {
                // ✅ Validation
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "اسم المنتج مطلوب");

                if (dto.Price <= 0)
                    return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "سعر البيع يجب أن يكون أكبر من الصفر");

                if (dto.Cost < 0)
                    return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "التكلفة لا يمكن أن تكون بالسالب");

                if (dto.Price < dto.Cost)
                    return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "لا يمكن أن يكون سعر البيع أقل من التكلفة");

                if (dto.Quantity < 0)
                    return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "الكمية لا يمكن أن تكون بالسالب");

                // ⚠️ Check (اختياري - UX فقط)
                bool isNameExists = await _productRepository.CheckProductNameExists(dto.Name);
                if (isNameExists)
                    return OperationResult<int>.FailureResult(OperationStatus.DuplicateRecord, "اسم المنتج موجود بالفعل");

                // ✅ DAL call
                int result = await _productRepository.AddNewProduct(dto);

                // ✅ تفسير النتيجة
                return result switch
                {
                    > 0 => OperationResult<int>.SuccessResult(result, "تم إضافة المنتج بنجاح"),

                    -1 => OperationResult<int>.FailureResult(
                        OperationStatus.DuplicateRecord,
                        "الباركود مستخدم بالفعل"),

                    _ => OperationResult<int>.FailureResult(
                        OperationStatus.Failed,
                        "حدث خطأ أثناء إضافة المنتج")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error adding new product");

                return OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "حدث خطأ غير متوقع أثناء إضافة المنتج");
            }
        }//

        public async Task<OperationResult<int>> UpdateProduct(ProductSaveDto dto)
        {
            if (dto.Id <= 0)
                return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "Id غير صالح");

            int result = await _productRepository.UpdateProduct(dto);

            return result switch
            {
                1 => OperationResult<int>.SuccessResult(dto.Id, "تم تعديل المنتج بنجاح"),

                -1 => OperationResult<int>.FailureResult(
                    OperationStatus.DuplicateRecord,
                    "الباركود مستخدم بالفعل"),

                0 => OperationResult<int>.FailureResult(
                    OperationStatus.NotFound,
                    "المنتج غير موجود"),

                _ => OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "حدث خطأ أثناء التعديل")
            };
        }

        public async Task<OperationResult<int>> DeleteProduct(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "رقم المنتج غير صالح");
                }

                int result = await _productRepository.DeleteProduct(id);

                return result switch
                {
                    1 => OperationResult<int>.SuccessResult(id, "تم حذف المنتج بنجاح"),

                    0 => OperationResult<int>.FailureResult(
                        OperationStatus.NotFound,
                        "المنتج غير موجود"),

                    _ => OperationResult<int>.FailureResult(
                        OperationStatus.Failed,
                        "حدث خطأ أثناء الحذف")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error deleting product {Id}", id);

                return OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ غير متوقع");
            }
        }

        public async Task<OperationResult<ProductResponseDto>> GetProductById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return OperationResult<ProductResponseDto>.FailureResult(
                        OperationStatus.InvalidData,
                        "رقم المنتج غير صالح");
                }

                var product = await _productRepository.GetProductById(id);

                if (product == null)
                {
                    return OperationResult<ProductResponseDto>.FailureResult(
                        OperationStatus.NotFound,
                        "المنتج غير موجود");
                }

                return OperationResult<ProductResponseDto>.SuccessResult(
                    product,
                    "تم جلب المنتج بنجاح");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error getting product {Id}", id);

                return OperationResult<ProductResponseDto>.FailureResult(
                    OperationStatus.Failed,
                    "حدث خطأ أثناء جلب المنتج");
            }
        }//

        public async Task<OperationResult<PagedList<ProductResponseDto>>> GetAllProductsPaged(PaginationParams pagination)
        {
            try
            {
               

                var (products, totalCount) = await _productRepository.GetAllProductsPaged(pagination);

                var pagedData = new PagedList<ProductResponseDto>(
                    products,
                    totalCount,
                    pagination.PageNumber,
                    pagination.PageSize
                );

                return OperationResult<PagedList<ProductResponseDto>>.SuccessResult(
                    pagedData,
                    "تم جلب المنتجات بنجاح");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error fetching paged products");

                return OperationResult<PagedList<ProductResponseDto>>.FailureResult(
                    OperationStatus.Failed,
                    "حدث خطأ أثناء جلب المنتجات");
            }
        }
        public async Task<OperationResult<int>> DecreaseProductQuantity(
       int productId,
       int quantityToSubtract,
       StockMovementType movementType,
       string? reference = null)
        {
            try
            {
                // ✅ Validation
                if (productId <= 0)
                {
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "رقم المنتج غير صالح");
                }

                if (quantityToSubtract <= 0)
                {
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "الكمية يجب أن تكون أكبر من صفر");
                }

                int result = await _productRepository.DecreaseProductQuantity(
                    productId,
                    quantityToSubtract,
                    movementType,
                    reference);

                return result switch
                {
                    1 => OperationResult<int>.SuccessResult(productId, "تم خصم الكمية بنجاح"),

                    -3 => OperationResult<int>.FailureResult(
                        OperationStatus.InsufficientStock,
                        "الكمية المتاحة غير كافية"),

                    _ => OperationResult<int>.FailureResult(
                        OperationStatus.Failed,
                        "حدث خطأ أثناء الخصم")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error decreasing product quantity {Id}", productId);

                return OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ غير متوقع");
            }
        }

        public async Task<OperationResult<int>> IncreaseProductQuantity(
                  int productId,
                          int quantityToAdd,
                  StockMovementType movementType,
                      string? reference = null)
        {
            try
            {
                // ✅ Validation
                if (productId <= 0)
                {
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "رقم المنتج غير صالح");
                }

                if (quantityToAdd <= 0)
                {
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "الكمية يجب أن تكون أكبر من صفر");
                }

                int result = await _productRepository.IncreaseProductQuantity(
                    productId,
                    quantityToAdd,
                    movementType,
                    reference);

                return result switch
                {
                    1 => OperationResult<int>.SuccessResult(productId, "تم زيادة الكمية بنجاح"),

                    -2 => OperationResult<int>.FailureResult(
                        OperationStatus.NotFound,
                        "المنتج غير موجود"),

                    _ => OperationResult<int>.FailureResult(
                        OperationStatus.Failed,
                        "حدث خطأ أثناء زيادة الكمية")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error increasing product quantity {Id}", productId);

                return OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ غير متوقع");
            }
        }
        public async Task<bool> CheckProductNameExists(string name, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return await _productRepository.CheckProductNameExists(name, excludeId);
        }

        public async Task<bool> IsProductExistById(int id)
        {
            if (id <= 0)
                return false;

            return await _productRepository.IsProductExistById(id);
        }







    }
}