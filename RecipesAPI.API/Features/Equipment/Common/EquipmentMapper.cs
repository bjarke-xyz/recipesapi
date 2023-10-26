using AutoMapper;
using RecipesAPI.API.Features.Admin.Common;
using RecipesAPI.API.Features.Admin.DAL;
using RecipesAPI.API.Features.Equipment.DAL;
using RecipesAPI.API.Features.Equipment.Graph;

namespace RecipesAPI.API.Features.Equipment.Common;

public static class EquipmentMapper
{
    private static readonly IMapper mapper = new MapperConfiguration(cfg =>
    {
        cfg.CreateMap<EquipmentItemDto, EquipmentItem>()
            .ForMember(e => e.Links, opt => opt.MapFrom(src => src.Links ?? new List<EquipmentLinkDto>()))
            ;
        cfg.CreateMap<EquipmentLinkDto, EquipmentLink>();

        cfg.CreateMap<EquipmentItem, EquipmentItemDto>()
            .ForMember(e => e.Links, opt => opt.MapFrom(src => src.Links ?? new List<EquipmentLink>()))
            ;
        cfg.CreateMap<EquipmentLink, EquipmentLinkDto>();

        cfg.CreateMap<EquipmentInput, EquipmentItem>();
    }).CreateMapper();

    public static EquipmentItem MapDto(EquipmentItemDto dto)
    {
        var equipment = mapper.Map<EquipmentItemDto, EquipmentItem>(dto);
        return equipment;
    }

    public static EquipmentItemDto Map(EquipmentItem equipment)
    {
        var dto = mapper.Map<EquipmentItem, EquipmentItemDto>(equipment);
        return dto;
    }

    public static EquipmentItem MapInput(EquipmentInput input)
    {
        var equipment = mapper.Map<EquipmentInput, EquipmentItem>(input);
        return equipment;
    }
}