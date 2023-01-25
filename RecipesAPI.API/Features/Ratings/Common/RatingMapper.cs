using AutoMapper;
using RecipesAPI.API.Features.Ratings.DAL;

namespace RecipesAPI.API.Features.Ratings.Common;

public static class RatingMapper
{
    private static IMapper mapper = new MapperConfiguration(cfg =>
    {
        cfg.CreateMap<RatingDto, Rating>();
        cfg.CreateMap<Rating, RatingDto>();
        cfg.CreateMap<RatingTypeDto, RatingType>();
        cfg.CreateMap<RatingType, RatingTypeDto>();
    }).CreateMapper();

    public static Rating MapDto(RatingDto dto)
    {
        var rating = mapper.Map<RatingDto, Rating>(dto);
        return rating;
    }

    public static RatingDto Map(Rating rating)
    {
        var dto = mapper.Map<Rating, RatingDto>(rating);
        return dto;
    }
}