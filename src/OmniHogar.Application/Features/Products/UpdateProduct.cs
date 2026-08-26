using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Features.Products;

public record UpdateProductCommand(Guid Id, string Sku, string Name, string? Description, Guid? CategoryId, decimal Price, string? ImageUrl, string Status)
    : IRequest;

/// <summary>
/// Validates mandatory fields and SKU uniqueness (excluding the product's own row) for
/// <see cref="UpdateProductCommand"/> (HU-10 AC2/AC4).
/// </summary>
public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Sku)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El SKU es obligatorio.")
            .MaximumLength(20).WithMessage("El SKU debe tener como máximo 20 caracteres.")
            .MustAsync((command, sku, cancellationToken) =>
                context.Products.AllAsync(p => p.Sku != sku || p.Id == command.Id, cancellationToken))
            .WithMessage("Ya existe un producto con ese SKU.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre debe tener como máximo 150 caracteres.");

        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("El precio no puede ser negativo.");

        RuleFor(x => x.Status)
            .Must(s => s is "active" or "discontinued")
            .WithMessage("El estado debe ser 'active' o 'discontinued'.");
    }
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException(nameof(Domain.Entities.Product), request.Id);
        }

        product.Sku = request.Sku;
        product.Name = request.Name;
        product.Description = request.Description;
        product.CategoryId = request.CategoryId;
        product.Price = request.Price;
        product.ImageUrl = request.ImageUrl;
        product.Status = request.Status;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
