using Pos.Datalayer;
using Pos.Datalayer.Helpers;
using POS.Shared.Responses;
using POS.Shared.Responses.DTOs;
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
        Task<OperationResult<int>> AddNewProduct(ProductSaveDto dto);
        Task<OperationResult<int>> UpdateProduct(ProductSaveDto dto);
        Task<OperationResult<int>> DeleteProduct(int id);
        Task<OperationResult<ProductResponseDto>> GetProductById(int id);
        Task<OperationResult<PagedList<ProductResponseDto>>> GetAllProductsPaged(PaginationParams pagination);
        Task<OperationResult<int>> DecreaseProductQuantity(int productId, int quantityToSubtract);
        Task<OperationResult<int>> IncreaseProductQuantity(int productId, int quantityToAdd);
        Task<bool> CheckProductNameExists(string name, int? excludeId = null);
        Task<bool> IsProductExistById(int id);
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
               
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "اسم المنتج مطلوب ولا يمكن أن يكون فارغاً");

                if (dto.Price <= 0)
                    return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "سعر البيع يجب أن يكون أكبر من الصفر");

                if (dto.Cost < 0)
                    return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "التكلفة لا يمكن أن تكون بالسالب");

                if (dto.Price < dto.Cost)
                    return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "عفواً، لا يمكن أن يكون سعر البيع أقل من تكلفة الشراء!");

                if (dto.Quantity < 0)
                    return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "الكمية الافتتاحية لا يمكن أن تكون بالسالب");

             
                bool isNameExists = await _productRepository.CheckProductNameExists(dto.Name);
                if (isNameExists)
                    return OperationResult<int>.FailureResult(OperationStatus.DuplicateRecord, "اسم المنتج مسجل مسبقاً في النظام");

              
                return await _productRepository.AddNewProduct(dto);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error adding new product");
                return OperationResult<int>.FailureResult(OperationStatus.Failed, "حدث خطأ غير متوقع أثناء إضافة المنتج");
            }
        }

        public async Task<OperationResult<int>> UpdateProduct(ProductSaveDto dto)
        {
            Log.Information("Service: Validating update for product {Id}", dto.Id);
            try
            {
           
                if (dto.Id <= 0)
                    return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "رقم المنتج غير صحيح");

                if (string.IsNullOrWhiteSpace(dto.Name))
                    return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "اسم المنتج مطلوب");

                if (dto.Price <= 0 || dto.Cost < 0)
                    return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "الأسعار المدخلة غير صحيحة");

              
                bool isNameExists = await _productRepository.CheckProductNameExists(dto.Name, dto.Id);
                if (isNameExists)
                    return OperationResult<int>.FailureResult(OperationStatus.DuplicateRecord, "اسم المنتج مسجل لمنتج آخر في النظام");

                return await _productRepository.UpdateProduct(dto);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error updating product {Id}", dto.Id);
                return OperationResult<int>.FailureResult(OperationStatus.Failed, "حدث خطأ غير متوقع أثناء تعديل المنتج");
            }
        }

        public async Task<OperationResult<int>> DeleteProduct(int id)
        {
            if (id <= 0)
                return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "رقم المنتج غير صحيح");

            return await _productRepository.DeleteProduct(id);
        }

        public async Task<OperationResult<ProductResponseDto>> GetProductById(int id)
        {
            if (id <= 0)
                return OperationResult<ProductResponseDto>.FailureResult(OperationStatus.InvalidData, "رقم المعرف غير صحيح");

            return await _productRepository.GetProductById(id);
        }

        public async Task<OperationResult<PagedList<ProductResponseDto>>> GetAllProductsPaged(PaginationParams pagination)
        {
            try
            {
             

                return await _productRepository.GetAllProductsPaged(pagination);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error getting paged products");
                return OperationResult<PagedList<ProductResponseDto>>.FailureResult(OperationStatus.Failed, "حدث خطأ غير متوقع أثناء جلب المنتجات");
            }
        }

        public async Task<OperationResult<int>> DecreaseProductQuantity(int productId, int quantityToSubtract)
        {
            if (productId <= 0)
                return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "رقم المنتج غير صحيح");

          
            if (quantityToSubtract <= 0)
                return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "الكمية المراد خصمها يجب أن تكون أكبر من الصفر");

            return await _productRepository.DecreaseProductQuantity(productId, quantityToSubtract);
        }

        public async Task<OperationResult<int>> IncreaseProductQuantity(int productId, int quantityToAdd)
        {
            if (productId <= 0)
                return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "رقم المنتج غير صحيح");

            if (quantityToAdd <= 0)
                return OperationResult<int>.FailureResult(OperationStatus.InvalidData, "الكمية المضافة يجب أن تكون أكبر من الصفر");

            return await _productRepository.IncreaseProductQuantity(productId, quantityToAdd);
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