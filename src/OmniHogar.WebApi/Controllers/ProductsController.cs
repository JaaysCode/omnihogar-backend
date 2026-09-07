using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniHogar.Application.Features.Products;

namespace OmniHogar.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<ProductDto>>> GetAll(CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetProductsQuery(), cancellationToken);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetProductByIdQuery(id), cancellationToken);
    }

    /// <summary>Admin management list (HU-10) — every product, any status.</summary>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<ProductDto>>> GetAllForAdmin(CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetAdminProductsQuery(), cancellationToken);
    }

    // Requires an authenticated user (admin, jefe de bodega, coordinador de despacho, asesor de
    // tienda all consult inventory today). Fine-grained per-role restriction isn't wired up yet —
    // this repo has no [Authorize(Roles: ...)] usage anywhere else, so [Authorize] alone matches
    // the existing convention until a real permission model lands.
    [HttpGet("{id:guid}/stock")]
    [Authorize]
    public async Task<ActionResult<ProductStockDto>> GetStock(Guid id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetProductStockQuery(id), cancellationToken);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Guid>> Create(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var id = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Manually add units to a product's stock (HU inventario — "Agregar Unidades").</summary>
    [HttpPost("{id:guid}/stock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddStock(Guid id, AddStockRequestDto body, CancellationToken cancellationToken)
    {
        await _sender.Send(new AddProductStockCommand(id, body.Quantity, body.Reason), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateProductCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest("Route id does not match body id.");
        }

        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteProductCommand(id), cancellationToken);
        return NoContent();
    }
}

/// <summary>Request body for <see cref="ProductsController.AddStock"/> — ProductId comes from the route.</summary>
public record AddStockRequestDto(int Quantity, string? Reason);
