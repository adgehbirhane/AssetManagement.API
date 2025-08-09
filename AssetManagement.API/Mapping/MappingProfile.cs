using AutoMapper;
using AssetManagement.API.Models;
using AssetManagement.API.DTOs;

namespace AssetManagement.API.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));
        
        // Category mappings
        CreateMap<Category, CategoryDto>();
        CreateMap<CreateCategoryDto, Category>();
        CreateMap<UpdateCategoryDto, Category>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        
        // Asset mappings
        CreateMap<Asset, AssetDto>()
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
            .ForMember(dest => dest.AssignedTo, opt => opt.MapFrom(src => src.AssignedTo));
        
        CreateMap<CreateAssetRequest, Asset>();
        CreateMap<UpdateAssetRequest, Asset>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        
        // AssetRequest mappings
        CreateMap<AssetRequest, AssetRequestDto>()
            .ForMember(dest => dest.Asset, opt => opt.MapFrom(src => src.Asset))
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User))
            .ForMember(dest => dest.ProcessedBy, opt => opt.MapFrom(src => src.ProcessedBy));
    }
}


