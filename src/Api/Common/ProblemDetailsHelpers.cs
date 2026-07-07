namespace Api.Common;

public static class ProblemDetailsHelpers
{
    public static IResult ValidationProblem(IDictionary<string, string[]> errors) =>
        TypedResults.ValidationProblem(errors);

    public static IResult Conflict(string detail) =>
        TypedResults.Problem(detail: detail, statusCode: StatusCodes.Status409Conflict);
}
