using AutoMapper;
using RecipesAPI.API.Features.Admin.DAL;

namespace RecipesAPI.API.Features.Admin.Common;

public static class AdminMapper
{
    private static IMapper mapper = new MapperConfiguration(cfg =>
    {
        cfg.CreateMap<AffiliateItemReferenceDto, AffiliateItemReference>()
            .ForMember(dest => dest.Provider, opt => opt.MapFrom(src => Enum.Parse<AffiliateProvider>(src.Provider)));
        cfg.CreateMap<AffiliateItemReference, AffiliateItemReferenceDto>()
            .ForMember(dest => dest.Provider, opt => opt.MapFrom(src => src.Provider.ToString()));

        cfg.CreateMap<AdtractionItemReferenceDto, AdtractionItemReference>();
        cfg.CreateMap<AdtractionItemReference, AdtractionItemReferenceDto>();

        cfg.CreateMap<PartnerAdsItemReferenceDto, PartnerAdsItemReference>();
        cfg.CreateMap<PartnerAdsItemReference, PartnerAdsItemReferenceDto>();
    }).CreateMapper();

    public static AffiliateItemReference MapDto(AffiliateItemReferenceDto dto)
    {
        var itemRef = mapper.Map<AffiliateItemReferenceDto, AffiliateItemReference>(dto);
        return itemRef;
    }
    public static List<AffiliateItemReference> MapDto(List<AffiliateItemReferenceDto> dtos) => dtos.Select(MapDto).ToList();

    public static AffiliateItemReferenceDto Map(AffiliateItemReference itemRef)
    {
        var dto = mapper.Map<AffiliateItemReference, AffiliateItemReferenceDto>(itemRef);
        return dto;
    }
    public static List<AffiliateItemReferenceDto> Map(List<AffiliateItemReference> itemRefs) => itemRefs.Select(Map).ToList();
}