# Stage 1 - Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Pursuit.slnx ./
COPY src/Pursuit.Domain/Pursuit.Domain.csproj src/Pursuit.Domain/
COPY src/Pursuit.Application/Pursuit.Application.csproj src/Pursuit.Application/
COPY src/Pursuit.Infrastructure/Pursuit.Infrastructure.csproj src/Pursuit.Infrastructure/
COPY src/Pursuit.API/Pursuit.API.csproj src/Pursuit.API/

RUN dotnet restore src/Pursuit.API/Pursuit.API.csproj

COPY . .

RUN dotnet publish src/Pursuit.API/Pursuit.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2 - Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Pursuit.API.dll"]