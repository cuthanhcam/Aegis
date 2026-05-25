FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Aegis.sln .
COPY Directory.Build.props .
COPY Directory.Build.targets .
COPY global.json .
COPY src/Aegis.Api/Aegis.Api.csproj src/Aegis.Api/
COPY src/Aegis.Application/Aegis.Application.csproj src/Aegis.Application/
COPY src/Aegis.Authorization/Aegis.Authorization.csproj src/Aegis.Authorization/
COPY src/Aegis.Contracts/Aegis.Contracts.csproj src/Aegis.Contracts/
COPY src/Aegis.Domain/Aegis.Domain.csproj src/Aegis.Domain/
COPY src/Aegis.Infrastructure/Aegis.Infrastructure.csproj src/Aegis.Infrastructure/
COPY src/Aegis.SharedKernel/Aegis.SharedKernel.csproj src/Aegis.SharedKernel/

RUN dotnet restore src/Aegis.Api/Aegis.Api.csproj

COPY . .
RUN dotnet publish src/Aegis.Api/Aegis.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Aegis.Api.dll"]
