FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/Ambev.DeveloperEvaluation.Domain/Ambev.DeveloperEvaluation.Domain.csproj", "src/Ambev.DeveloperEvaluation.Domain/"]
COPY ["src/Ambev.DeveloperEvaluation.Application/Ambev.DeveloperEvaluation.Application.csproj", "src/Ambev.DeveloperEvaluation.Application/"]
COPY ["src/Ambev.DeveloperEvaluation.Infrastructure/Ambev.DeveloperEvaluation.Infrastructure.csproj", "src/Ambev.DeveloperEvaluation.Infrastructure/"]
COPY ["src/Ambev.DeveloperEvaluation.Api/Ambev.DeveloperEvaluation.Api.csproj", "src/Ambev.DeveloperEvaluation.Api/"]
RUN dotnet restore "src/Ambev.DeveloperEvaluation.Api/Ambev.DeveloperEvaluation.Api.csproj"

COPY src/ src/
RUN dotnet publish "src/Ambev.DeveloperEvaluation.Api/Ambev.DeveloperEvaluation.Api.csproj" \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
WORKDIR /app
COPY --from=build /app/publish .
USER appuser
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD wget -qO- http://localhost:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "Ambev.DeveloperEvaluation.Api.dll"]
