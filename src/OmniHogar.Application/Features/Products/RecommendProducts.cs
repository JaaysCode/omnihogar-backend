using System.Globalization;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Features.Products;

/// <summary>
/// Chatbot product recommendation (HU recomendaciones) — active products related to the
/// keywords the NLU extracted from the customer's request, best match first. A keyword counts
/// when it appears (ignoring case, accents and a plural "s") in the product's name (weight 3),
/// its category name (2) or its description (1). In-stock products win ties. Empty list when
/// nothing relates to the request.
/// </summary>
public record RecommendProductsQuery(IReadOnlyList<string> Keywords) : IRequest<List<ProductDto>>;

public class RecommendProductsQueryHandler : IRequestHandler<RecommendProductsQuery, List<ProductDto>>
{
    public const int MaxRecommendations = 5;
    private const int MaxKeywords = 8;

    private readonly IApplicationDbContext _context;

    public RecommendProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductDto>> Handle(RecommendProductsQuery request, CancellationToken cancellationToken)
    {
        var terms = request.Keywords
            .Select(Normalize)
            .Where(t => t.Length >= 2)
            .Select(t => t.Length > 3 ? t.TrimEnd('s') : t)
            .Distinct()
            .Take(MaxKeywords)
            .ToList();

        if (terms.Count == 0)
        {
            return [];
        }

        // Scored in memory (accent-insensitive matching has no portable SQL translation); the
        // active catalog is small, and only the columns needed are loaded.
        var categoryNames = await _context.ProductCategories
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => Normalize(c.Name), cancellationToken);

        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.Status == "active")
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Description = p.Description,
                CategoryId = p.CategoryId,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                Status = p.Status,
                AvailableQuantity =
                    _context.Inventory.Where(i => i.ProductId == p.Id).Sum(i => (int?)i.AvailableQuantity) ?? 0,
            })
            .ToListAsync(cancellationToken);

        return products
            .Select(p =>
            {
                p.InStock = p.AvailableQuantity > 0;
                var name = Normalize(p.Name);
                var description = Normalize(p.Description ?? string.Empty);
                var category = p.CategoryId is { } id && categoryNames.TryGetValue(id, out var c) ? c : string.Empty;

                var score = terms.Sum(t =>
                    (name.Contains(t) ? 3 : 0) + (category.Contains(t) ? 2 : 0) + (description.Contains(t) ? 1 : 0));
                return (Product: p, Score: score);
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Product.InStock == true)
            .ThenBy(x => x.Product.Name)
            .Take(MaxRecommendations)
            .Select(x => x.Product)
            .ToList();
    }

    private static string Normalize(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
