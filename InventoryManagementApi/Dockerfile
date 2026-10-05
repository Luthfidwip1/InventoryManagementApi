FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app

EXPOSE 8080
EXPOSE 5050


FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["InventoryManagementApi/InventoryManagementApi.csproj", "InventoryManagementApi/"]

RUN dotnet restore "InventoryManagementApi/InventoryManagementApi.csproj"

COPY . .

WORKDIR "/src/InventoryManagementApi"

RUN dotnet build "InventoryManagementApi.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build


FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "InventoryManagementApi.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false


FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "InventoryManagementApi.dll"]