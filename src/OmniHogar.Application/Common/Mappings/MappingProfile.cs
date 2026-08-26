using AutoMapper;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Product, Features.Products.ProductDto>();
        CreateMap<Role, Features.Employees.RoleDto>();
    }
}
