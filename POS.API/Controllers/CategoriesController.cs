using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos.BuisnessLayer;
using Pos.Datalayer.Helpers;
using POS.Shared.Responses.DTOs;
using Serilog;
using System.Text.Json;
using POS.Shared.Responses.Enums;

namespace POS.API.Controllers
{
    [Route("api/Categories")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet("GetAllCategoriesPaged")]
        public async Task<ActionResult> GetAllCategoriesPaged([FromQuery] PaginationParams pagination)
        {
            Log.Information("Controller: Getting categories page {Page}", pagination.PageNumber);

            var result = await _categoryService.GetAllCategoriesPaged(pagination);

            if (result.Status != OperationStatus.Success || result.Data == null)
            {
                return BadRequest(new { message = result.Message });
            }

            var metaData = new
            {
                result.Data.MetaData.TotalCount,
                result.Data.MetaData.PageSize,
                result.Data.MetaData.CurrentPage,
                result.Data.MetaData.TotalPages,
                result.Data.MetaData.HasNext,
                result.Data.MetaData.HasPrevious
            };

            Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(metaData));

            return Ok(result);
        }

        [HttpGet("GetCategoryById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> GetCategoryById(int id)
        {
            Log.Information("Controller: Getting category with ID {Id}", id);

            if (id <= 0)
            {
                return BadRequest(new { message = "Invalid category id" });
            }

            var result = await _categoryService.GetCategoryById(id);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new
                {
                    message = result.Message,
                    data = result.Data
                }),

                OperationStatus.NotFound => NotFound(new
                {
                    message = result.Message
                }),

                OperationStatus.InvalidData => BadRequest(new
                {
                    message = result.Message
                }),

                _ => StatusCode(500, new
                {
                    message = result.Message
                })
            };
        }

        [HttpPost("AddNewCategory")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddNewCategory([FromBody] CategorySaveDto dto)
        {
            Log.Information("Controller: Request to add new category {Name}", dto.Name);

            var result = await _categoryService.AddNewCategory(dto);

            return result.Status switch
            {
                OperationStatus.Success => CreatedAtAction(
                    nameof(GetCategoryById),
                    new { id = result.Data },
                    new { Message = result.Message, CategoryId = result.Data }),

                OperationStatus.DuplicateRecord => Conflict(new
                {
                    Message = result.Message
                }),

                OperationStatus.InvalidData => BadRequest(new
                {
                    Message = result.Message
                }),

                _ => StatusCode(500, new
                {
                    Message = result.Message
                })
            };
        }

        [HttpPut("UpdateCategory")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> UpdateCategory([FromBody] CategorySaveDto dto)
        {
            Log.Information("Controller: Updating category {Id}", dto.Id);

            if (dto == null)
            {
                return BadRequest(new { message = "Invalid request data" });
            }

            var result = await _categoryService.UpdateCategory(dto);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new
                {
                    message = result.Message,
                    categoryId = result.Data
                }),

                OperationStatus.NotFound => NotFound(new
                {
                    message = result.Message
                }),

                OperationStatus.DuplicateRecord => Conflict(new
                {
                    message = result.Message
                }),

                OperationStatus.InvalidData => BadRequest(new
                {
                    message = result.Message
                }),

                _ => StatusCode(500, new
                {
                    message = result.Message
                })
            };
        }

        [HttpDelete("DeleteCategory/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> DeleteCategory(int id)
        {
            Log.Information("Controller: Deleting category {Id}", id);

            var result = await _categoryService.DeleteCategory(id);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new
                {
                    message = result.Message,
                    categoryId = result.Data
                }),

                OperationStatus.NotFound => NotFound(new
                {
                    message = result.Message
                }),

                OperationStatus.InvalidData => BadRequest(new
                {
                    message = result.Message
                }),

                _ => StatusCode(500, new
                {
                    message = result.Message
                })
            };
        }
    }
}
