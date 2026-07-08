FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["global.json", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["SafeWoman.API/SafeWoman.API.csproj", "SafeWoman.API/"]
COPY ["SafeWoman.Application/SafeWoman.Application.csproj", "SafeWoman.Application/"]
COPY ["SafeWoman.Domain/SafeWoman.Domain.csproj", "SafeWoman.Domain/"]
COPY ["SafeWoman.Infrastructure/SafeWoman.Infrastructure.csproj", "SafeWoman.Infrastructure/"]
COPY ["SafeWoman.API/packages.lock.json", "SafeWoman.API/"]
COPY ["SafeWoman.Application/packages.lock.json", "SafeWoman.Application/"]
COPY ["SafeWoman.Domain/packages.lock.json", "SafeWoman.Domain/"]
COPY ["SafeWoman.Infrastructure/packages.lock.json", "SafeWoman.Infrastructure/"]

RUN dotnet restore "SafeWoman.API/SafeWoman.API.csproj"

COPY SafeWoman.API/       SafeWoman.API/
COPY SafeWoman.Application/ SafeWoman.Application/
COPY SafeWoman.Domain/    SafeWoman.Domain/
COPY SafeWoman.Infrastructure/ SafeWoman.Infrastructure/

WORKDIR "/src/SafeWoman.API"
RUN dotnet publish "SafeWoman.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "SafeWoman.API.dll"]
