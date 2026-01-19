FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Linder-DatingApp.csproj", "./"]
RUN dotnet restore "Linder-DatingApp.csproj"
COPY . .
RUN dotnet build "Linder-DatingApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Linder-DatingApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Linder-DatingApp.dll"]