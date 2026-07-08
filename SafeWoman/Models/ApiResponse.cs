namespace SafeWoman.Models;

public record ApiResponse<T>(bool Success, T? Data, string? Error);
