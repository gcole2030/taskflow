namespace Api.Common;

public static class ProblemDetailsHelpers
{
    public static IResult ValidationProblem(IDictionary<string, string[]> errors) =>
        Results.ValidationProblem(errors);

    public static IResult Conflict(string detail) =>
        Results.Problem(detail: detail, statusCode: StatusCodes.Status409Conflict);
}
