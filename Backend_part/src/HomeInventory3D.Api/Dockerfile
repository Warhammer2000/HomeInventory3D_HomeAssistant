FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Install native Assimp library + create libdl.so symlink
# (AssimpNet 4.1.0 calls dlopen via libdl.so which was merged into libc in newer glibc)
RUN apt-get update && apt-get install -y --no-install-recommends \
    libassimp-dev \
    && rm -rf /var/lib/apt/lists/* \
    && ln -sf /usr/lib/x86_64-linux-gnu/libdl.so.2 /usr/lib/x86_64-linux-gnu/libdl.so \
    || ln -sf $(find / -name 'libdl.so*' -type f 2>/dev/null | head -1) /usr/lib/libdl.so \
    || true

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/HomeInventory3D.Api/HomeInventory3D.Api.csproj", "src/HomeInventory3D.Api/"]
COPY ["src/HomeInventory3D.Application/HomeInventory3D.Application.csproj", "src/HomeInventory3D.Application/"]
COPY ["src/HomeInventory3D.Domain/HomeInventory3D.Domain.csproj", "src/HomeInventory3D.Domain/"]
COPY ["src/HomeInventory3D.Infrastructure/HomeInventory3D.Infrastructure.csproj", "src/HomeInventory3D.Infrastructure/"]
RUN dotnet restore "src/HomeInventory3D.Api/HomeInventory3D.Api.csproj"
COPY . .
WORKDIR "/src/src/HomeInventory3D.Api"
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HomeInventory3D.Api.dll"]
