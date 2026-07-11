FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/ImaginArte.Server/ImaginArte.Server.csproj", "src/ImaginArte.Server/"]
COPY ["src/ImaginArte.Application/ImaginArte.Application.csproj", "src/ImaginArte.Application/"]
COPY ["src/ImaginArte.Infrastructure/ImaginArte.Infrastructure.csproj", "src/ImaginArte.Infrastructure/"]
COPY ["src/ImaginArte.Contracts/ImaginArte.Contracts.csproj", "src/ImaginArte.Contracts/"]

RUN dotnet restore "src/ImaginArte.Server/ImaginArte.Server.csproj"

COPY . .
RUN dotnet publish "src/ImaginArte.Server/ImaginArte.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ImaginArte.Server.dll"]