# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Certificates.Api.csproj", "./"]
RUN dotnet restore "./Certificates.Api.csproj"

COPY . .
RUN dotnet publish "./Certificates.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
VOLUME ["/data"]

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Certificates.Api.dll"]
