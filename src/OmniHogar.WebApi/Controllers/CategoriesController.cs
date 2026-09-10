using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniHogar.Application.Features.Products;
using OmniHogar.Domain.Constants;

namespace OmniHogar.WebApi.Controllers;

/// <summary>Product category catalog — populates the "Categoría" select in the product form.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AppPermissions.ProductosGestionar)]
public class CategoriesController : ControllerBase
{
    private readonly ISender _sender;

    public CategoriesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll(CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetCategoriesQuery(), cancellationToken);
    }
}
