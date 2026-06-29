# --- Build aşaması ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Önce csproj'ları kopyala ve restore et (katman önbelleği için).
COPY RealTimeAuction.sln .
COPY AuctionHouse.Core/AuctionHouse.Core.csproj AuctionHouse.Core/
COPY AuctionHouse.Infrastructure/AuctionHouse.Infrastructure.csproj AuctionHouse.Infrastructure/
COPY AuctionHouse.Hubs/AuctionHouse.Hubs.csproj AuctionHouse.Hubs/
COPY AuctionHouse.Api/AuctionHouse.Api.csproj AuctionHouse.Api/
COPY AuctionHouse.Web/AuctionHouse.Web.csproj AuctionHouse.Web/
COPY AuctionHouse.Tests/AuctionHouse.Tests.csproj AuctionHouse.Tests/
RUN dotnet restore AuctionHouse.Web/AuctionHouse.Web.csproj

# Kaynakların tamamını kopyala ve yayınla.
COPY . .
RUN dotnet publish AuctionHouse.Web/AuctionHouse.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# --- Runtime aşaması ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# SQLite veritabanı ve logların yazılacağı dizin (volume ile kalıcı yapılabilir).
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AuctionHouse.Web.dll"]
