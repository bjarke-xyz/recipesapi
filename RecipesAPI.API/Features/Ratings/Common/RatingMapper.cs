using AutoMapper;
using RecipesAPI.API.Features.Ratings.DAL;

namespace RecipesAPI.API.Features.Ratings.Common;

public static class RatingMapper
{
    private readonly static IMapper mapper = new MapperConfiguration(cfg =>
    {
        cfg.CreateMap<RatingDto, Rating>();
        cfg.CreateMap<Rating, RatingDto>();

        cfg.CreateMap<RatingTypeDto, RatingType>();
        cfg.CreateMap<RatingType, RatingTypeDto>();

        cfg.CreateMap<ReactionTypeDto, ReactionType>();
        cfg.CreateMap<ReactionType, ReactionTypeDto>();

        cfg.CreateMap<ReactionDto, Reaction>();
        cfg.CreateMap<Reaction, ReactionDto>();

        cfg.CreateMap<CommentDto, Comment>();
        cfg.CreateMap<Comment, CommentDto>();
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

    public static Reaction MapDto(ReactionDto dto)
    {
        var reaction = mapper.Map<ReactionDto, Reaction>(dto);
        return reaction;
    }

    public static ReactionDto Map(Reaction reaction)
    {
        var dto = mapper.Map<Reaction, ReactionDto>(reaction);
        return dto;
    }

    public static Comment MapDto(CommentDto dto)
    {
        return mapper.Map<CommentDto, Comment>(dto);
    }

    public static CommentDto Map(Comment comment)
    {
        return mapper.Map<Comment, CommentDto>(comment);
    }
}