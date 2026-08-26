using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Application.Features.Products;

public record CreateProductCommand(string Sku, string Name, string? Description, Guid? CategoryId, decimal Price, string? ImageUrl)
    : IRequest<Guid>;

/// <summary>
/// Validates mandatory fields and SKU uniqueness for <see cref="CreateProductCommand"/> (HU-10
/// AC2/AC4). Uniqueness is re-checked at the DB level (unique index on products.sku) as a
/// race-condition safety net.
/// </summary>
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Sku)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El SKU es obligatorio.")
            .MaximumLength(20).WithMessage("El SKU debe tener como máximo 20 caracteres.")
            .MustAsync((sku, cancellationToken) => context.Products.AllAsync(p => p.Sku != sku, cancellationToken))
            .WithMessage("Ya existe un producto con ese SKU.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre debe tener como máximo 150 caracteres.");

        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("El precio no puede ser negativo.");
    }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Sku = request.Sku,
            Name = request.Name,
            Description = request.Description,
            CategoryId = request.CategoryId,
            Price = request.Price,
            ImageUrl = request.ImageUrl,
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
