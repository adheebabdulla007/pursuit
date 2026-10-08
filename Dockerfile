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

RUN dotnet tool restore && \
    ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=PursuitBuild;User Id=sa;Password=BuildOnly-Password-1!;TrustServerCertificate=True;" \
    dotnet dotnet-ef migrations bundle \
    --project src/Pursuit.Infrastructure \
    --startup-project src/Pursuit.API \
    --configuration Release \
    --target-runtime linux-x64 \
    --output /app/migrations/efbundle \
    --force

RUN dotnet publish src/Pursuit.API/Pursuit.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2 - Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /app/migrations ./migrations
RUN chmod 0755 /app/migrations/efbundle
EXPOSE 8080
ENTRYPOINT ["dotnet", "Pursuit.API.dll"]
