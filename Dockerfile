# Stage 1 - Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Pursuit.slnx ./
COPY src/Pursuit.Domain/Pursuit.Domain.csproj src/Pursuit.Domain/
COPY src/Pursuit.Application/Pursuit.Application.csproj src/Pursuit.Application/
COPY src/Pursuit.Infrastructure/Pursuit.Infrastructure.csproj src/Pursuit.Infrastructure/
COPY src/Pursuit.API/Pursuit.API.csproj src/Pursuit.API/
COPY src/Pursuit.Migrations/Pursuit.Migrations.csproj src/Pursuit.Migrations/

RUN dotnet restore src/Pursuit.API/Pursuit.API.csproj && \
    dotnet restore src/Pursuit.Migrations/Pursuit.Migrations.csproj --runtime linux-x64

COPY . .

ARG PURSUIT_ALLOW_INSECURE_LOCAL_MIGRATIONS=false
RUN dotnet publish src/Pursuit.Migrations/Pursuit.Migrations.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    -p:PublishSingleFile=true \
    -p:AllowInsecureLocalMigrations=${PURSUIT_ALLOW_INSECURE_LOCAL_MIGRATIONS} \
    -o /app/migrations \
    --no-restore

RUN dotnet publish src/Pursuit.API/Pursuit.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2 - Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /app/migrations ./migrations
RUN chmod 0755 /app/migrations/Pursuit.Migrations
EXPOSE 8080
ENTRYPOINT ["dotnet", "Pursuit.API.dll"]
