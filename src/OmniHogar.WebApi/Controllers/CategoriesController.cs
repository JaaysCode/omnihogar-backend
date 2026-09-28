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

    /// <summary>Public read (HU-17 catalog search filter, HU-4 catalog) — category names aren't
    /// sensitive and are already visible per-product on the public catalog anyway. The controller
    /// -level policy still guards any future write endpoint added here.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<CategoryDto>>> GetAll(CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetCategoriesQuery(), cancellationToken);
    }
}
