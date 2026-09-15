FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build

COPY ["src/GTSPolandHiring.WebApi/GTSPolandHiring.WebApi.csproj", "src/GTSPolandHiring.WebApi/"]
RUN dotnet restore "src/GTSPolandHiring.WebApi/GTSPolandHiring.WebApi.csproj"

COPY . .
WORKDIR "/build/src/GTSPolandHiring.WebApi"
RUN dotnet publish "GTSPolandHiring.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "GTSPolandHiring.WebApi.dll"]