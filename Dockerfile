FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8006
ENV ASPNETCORE_URLS=http://+:8006

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Fulfillment.Api/Fulfillment.Api.csproj", "Fulfillment.Api/"]
COPY ["Fulfillment.Application/Fulfillment.Application.csproj", "Fulfillment.Application/"]
COPY ["Fulfillment.Domain/Fulfillment.Domain.csproj", "Fulfillment.Domain/"]
COPY ["Fulfillment.Infrastructure/Fulfillment.Infrastructure.csproj", "Fulfillment.Infrastructure/"]
RUN dotnet restore "Fulfillment.Api/Fulfillment.Api.csproj"
COPY . .
RUN dotnet build "Fulfillment.Api/Fulfillment.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Fulfillment.Api/Fulfillment.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fulfillment.Api.dll"]
