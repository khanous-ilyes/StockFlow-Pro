FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/StockFlowPro.API/StockFlowPro.API.csproj", "src/StockFlowPro.API/"]
COPY ["src/StockFlowPro.Application/StockFlowPro.Application.csproj", "src/StockFlowPro.Application/"]
COPY ["src/StockFlowPro.Domain/StockFlowPro.Domain.csproj", "src/StockFlowPro.Domain/"]
COPY ["src/StockFlowPro.Infrastructure/StockFlowPro.Infrastructure.csproj", "src/StockFlowPro.Infrastructure/"]
RUN dotnet restore "src/StockFlowPro.API/StockFlowPro.API.csproj"

COPY . .
WORKDIR "/src/src/StockFlowPro.API"
RUN dotnet build "StockFlowPro.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "StockFlowPro.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "StockFlowPro.API.dll"]
