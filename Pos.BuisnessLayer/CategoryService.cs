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
    public interface ICategoryService
    {
        Task<OperationResult<int>> AddNewCategory(CategorySaveDto dto);
        Task<OperationResult<int>> UpdateCategory(CategorySaveDto dto);
        Task<OperationResult<int>> DeleteCategory(int id);
        Task<OperationResult<CategoryResponseDto>> GetCategoryById(int id);
        Task<OperationResult<PagedList<CategoryResponseDto>>> GetAllCategoriesPaged(PaginationParams pagination);
        Task<bool> CheckCategoryNameExists(string name, int? excludeId = null);
        Task<bool> IsCategoryExistById(int id);
    }

    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<OperationResult<int>> AddNewCategory(CategorySaveDto dto)
        {
            Log.Information("Service: Validating new category {Name}", dto.Name);

            try
            {
              
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "اسم التصنيف مطلوب");

                dto.Name = dto.Name.Trim();

              
                bool isExists = await _categoryRepository.CheckCategoryNameExists(dto.Name);
                if (isExists)
                    return OperationResult<int>.FailureResult(
                        OperationStatus.DuplicateRecord,
                        "اسم التصنيف موجود بالفعل");

                int result = await _categoryRepository.AddNewCategory(dto);

                return result switch
                {
                    > 0 => OperationResult<int>.SuccessResult(result, "تم إضافة التصنيف بنجاح"),

                    -2 => OperationResult<int>.FailureResult(
                        OperationStatus.DuplicateRecord,
                        "اسم التصنيف مستخدم بالفعل"),

                    _ => OperationResult<int>.FailureResult(
                        OperationStatus.Failed,
                        "حدث خطأ أثناء إضافة التصنيف")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error adding new category");

                return OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "حدث خطأ غير متوقع");
            }
        }

        public async Task<OperationResult<int>> UpdateCategory(CategorySaveDto dto)
        {
            try
            {
                if (dto.Id <= 0)
                {
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "Id غير صالح");
                }

                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "اسم التصنيف مطلوب");
                }

                dto.Name = dto.Name.Trim();

                int result = await _categoryRepository.UpdateCategory(dto);

                return result switch
                {
                    1 => OperationResult<int>.SuccessResult(dto.Id, "تم تعديل التصنيف بنجاح"),

                    -2 => OperationResult<int>.FailureResult(
                        OperationStatus.DuplicateRecord,
                        "اسم التصنيف مستخدم بالفعل"),

                    -1 => OperationResult<int>.FailureResult(
                        OperationStatus.NotFound,
                        "التصنيف غير موجود"),

                    _ => OperationResult<int>.FailureResult(
                        OperationStatus.Failed,
                        "حدث خطأ أثناء التعديل")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error updating category {Id}", dto.Id);

                return OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ غير متوقع");
            }
        }

        public async Task<OperationResult<int>> DeleteCategory(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return OperationResult<int>.FailureResult(
                        OperationStatus.InvalidData,
                        "رقم التصنيف غير صالح");
                }

                int result = await _categoryRepository.DeleteCategory(id);

                return result switch
                {
                    1 => OperationResult<int>.SuccessResult(id, "تم حذف التصنيف بنجاح"),

                    -1 => OperationResult<int>.FailureResult(
                        OperationStatus.NotFound,
                        "التصنيف غير موجود"),

                    _ => OperationResult<int>.FailureResult(
                        OperationStatus.Failed,
                        "حدث خطأ أثناء الحذف")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error deleting category {Id}", id);

                return OperationResult<int>.FailureResult(
                    OperationStatus.Failed,
                    "خطأ غير متوقع");
            }
        }

        public async Task<OperationResult<CategoryResponseDto>> GetCategoryById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return OperationResult<CategoryResponseDto>.FailureResult(
                        OperationStatus.InvalidData,
                        "رقم التصنيف غير صالح");
                }

                var category = await _categoryRepository.GetCategoryById(id);

                if (category == null)
                {
                    return OperationResult<CategoryResponseDto>.FailureResult(
                        OperationStatus.NotFound,
                        "التصنيف غير موجود");
                }

                return OperationResult<CategoryResponseDto>.SuccessResult(
                    category,
                    "تم جلب التصنيف بنجاح");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error getting category {Id}", id);

                return OperationResult<CategoryResponseDto>.FailureResult(
                    OperationStatus.Failed,
                    "حدث خطأ أثناء جلب التصنيف");
            }
        }

        public async Task<OperationResult<PagedList<CategoryResponseDto>>> GetAllCategoriesPaged(PaginationParams pagination)
        {
            try
            {
                var (categories, totalCount) =
                    await _categoryRepository.GetAllCategoriesPaged(pagination);

                var pagedData = new PagedList<CategoryResponseDto>(
                    categories,
                    totalCount,
                    pagination.PageNumber,
                    pagination.PageSize
                );

                return OperationResult<PagedList<CategoryResponseDto>>.SuccessResult(
                    pagedData,
                    "تم جلب التصنيفات بنجاح");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service: Error fetching categories");

                return OperationResult<PagedList<CategoryResponseDto>>.FailureResult(
                    OperationStatus.Failed,
                    "حدث خطأ أثناء جلب التصنيفات");
            }
        }

        public async Task<bool> CheckCategoryNameExists(string name, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return await _categoryRepository.CheckCategoryNameExists(name, excludeId);
        }

        public async Task<bool> IsCategoryExistById(int id)
        {
            if (id <= 0)
                return false;

            return await _categoryRepository.IsCategoryExistById(id);
        }
    }
}
