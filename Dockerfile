FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/SmartFoods.Web/SmartFoods.Web.csproj", "src/SmartFoods.Web/"]
RUN dotnet restore "src/SmartFoods.Web/SmartFoods.Web.csproj"

COPY . .
RUN dotnet publish "src/SmartFoods.Web/SmartFoods.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet SmartFoods.Web.dll"]