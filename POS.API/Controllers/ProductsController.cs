using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos.BuisnessLayer;
using Pos.Datalayer.Helpers;
using POS.Shared.Responses;
using POS.Shared.Responses.DTOs;
using Serilog;

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
        [HttpGet("GetAllProducts")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllProductsPaged([FromQuery] PaginationParams pagination)
        {
            Log.Information("Controller: Request to get all products. Page: {Page}", pagination.PageNumber);
            var result = await _productService.GetAllProductsPaged(pagination);

            if (result.Status == OperationStatus.Success)
                return Ok(result.Data);

            return StatusCode(500, new { Message = result.Message });
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProductById(int id)
        {
            Log.Information("Controller: Request to get product {Id}", id);
            var result = await _productService.GetProductById(id);

            return result.Status switch
            {
                OperationStatus.Success => Ok(result.Data),
                OperationStatus.NotFound => NotFound(new { Message = result.Message }),
                OperationStatus.InvalidData => BadRequest(new { Message = result.Message }),
                _ => StatusCode(500, new { Message = result.Message })
            };
        }
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
        }

        [HttpPut("UpdateProduct/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductSaveDto dto)
        {
            Log.Information("Controller: Request to update product {Id}", id);

            if (id != dto.Id)
                return BadRequest(new { Message = "رقم المنتج في الرابط لا يطابق الرقم في البيانات المرسلة" });

            var result = await _productService.UpdateProduct(dto);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new { Message = result.Message, ProductId = result.Data }),
                OperationStatus.NotFound => NotFound(new { Message = result.Message }),
                OperationStatus.DuplicateRecord => Conflict(new { Message = result.Message }),
                OperationStatus.InvalidData => BadRequest(new { Message = result.Message }),
                _ => StatusCode(500, new { Message = result.Message })
            };
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            Log.Information("Controller: Request to delete product {Id}", id);
            var result = await _productService.DeleteProduct(id);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new { Message = result.Message }),
                OperationStatus.NotFound => NotFound(new { Message = result.Message }),
                _ => StatusCode(500, new { Message = result.Message })
            };
        }

    }

}
