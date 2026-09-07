using AutoMapper;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Product, Features.Products.ProductDto>();
        CreateMap<ProductCategory, Features.Products.CategoryDto>();
        CreateMap<Role, Features.Employees.RoleDto>();

        CreateMap<Order, Features.Orders.OrderDto>()
            .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.User.FirstName + " " + s.User.LastName))
            .ForMember(d => d.ItemCount, opt => opt.MapFrom(s => s.Items.Count));

        CreateMap<Order, Features.Orders.OrderDetailDto>()
            .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.User.FirstName + " " + s.User.LastName))
            .ForMember(d => d.CustomerEmail, opt => opt.MapFrom(s => s.User.Email));

        CreateMap<OrderItem, Features.Orders.OrderItemDto>()
            .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product.Name))
            .ForMember(d => d.Sku, opt => opt.MapFrom(s => s.Product.Sku));
    }
}
