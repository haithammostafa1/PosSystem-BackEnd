using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos.BuisnessLayer;
using Pos.Datalayer.Helpers;
using POS.Shared.Responses.DTOs;
using POS.Shared.Responses.Enums;
using Serilog;
using System.Text.Json;

namespace POS.API.Controllers
{
    [Route("api/Products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }
        [HttpGet("GetAllProductsPaged")]
        public async Task<ActionResult> GetAllProductsPaged([FromQuery] PaginationParams pagination)
        {
            Log.Information("Controller: Getting products page {Page}", pagination.PageNumber);

            var result = await _productService.GetAllProductsPaged(pagination);

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

        [HttpGet("GetProductById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> GetProductById(int id)
        {
            Log.Information("Controller: Getting product with ID {Id}", id);

         
            if (id <= 0)
            {
                return BadRequest(new { message = "Invalid product id" });
            }

            var result = await _productService.GetProductById(id);

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
        }//
        [HttpPost("AddNewProduct")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddNewProduct([FromBody] ProductSaveDto dto)
        {
            Log.Information("Controller: Request to add new product {Name}", dto.Name);
            var result = await _productService.AddNewProduct(dto);

            return result.Status switch
            {
                OperationStatus.Success => CreatedAtAction(nameof(GetProductById), new { id = result.Data }, new { Message = result.Message, ProductId = result.Data }),
                OperationStatus.DuplicateRecord => Conflict(new { Message = result.Message }),
                OperationStatus.InvalidData => BadRequest(new { Message = result.Message }),
                _ => StatusCode(500, new { Message = result.Message })
            };
        }//

        [HttpPut("UpdateProduct")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> UpdateProduct([FromBody] ProductSaveDto dto)
        {
            Log.Information("Controller: Updating product {Id}", dto.Id);

            if (dto == null)
            {
                return BadRequest(new { message = "Invalid request data" });
            }

            var result = await _productService.UpdateProduct(dto);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new
                {
                    message = result.Message,
                    productId = result.Data
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
        [HttpDelete("DeleteProduct/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            Log.Information("Controller: Deleting product {Id}", id);

            var result = await _productService.DeleteProduct(id);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new
                {
                    message = result.Message,
                    productId = result.Data
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
        [HttpPost("DecreaseProductQuantity")]
        public async Task<ActionResult> DecreaseProductQuantity(
       int productId,
              int quantity,
                StockMovementType movementType,
                  string? reference = null)
        {
            Log.Information("Controller: Decreasing product {Id}", productId);

            var result = await _productService.DecreaseProductQuantity(
                productId,
                quantity,
                movementType,
                reference);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new
                {
                    message = result.Message,
                    productId = result.Data
                }),

                OperationStatus.InsufficientStock => BadRequest(new
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


        [HttpPost("IncreaseProductQuantity")]
        public async Task<ActionResult> IncreaseProductQuantity(
      int productId,
      int quantity,
      StockMovementType movementType,
      string? reference = null)
        {
            Log.Information("Controller: Increasing product {Id}", productId);

            var result = await _productService.IncreaseProductQuantity(
                productId,
                quantity,
                movementType,
                reference);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new
                {
                    message = result.Message,
                    productId = result.Data
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
