namespace RecipesAPI.API.Features.Ratings.Common;

public static class CommentsUtil
{
    public static List<Comment> BuildTree(List<Comment> flattenedComments)
    {
        var commentsById = flattenedComments.GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.First());
        var result = new List<Comment>();
        foreach (var comment in flattenedComments)
        {
            if (!string.IsNullOrEmpty(comment.ParentCommentId))
            {
                var parentComment = commentsById.GetValueOrDefault(comment.ParentCommentId);
                parentComment?.Children.Add(comment);
            }
            else
            {
                result.Add(comment);
            }
        }
        return result;
    }
}