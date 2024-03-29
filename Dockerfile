FROM mcr.microsoft.com/dotnet/aspnet:5.0-focal AS base
WORKDIR /app
EXPOSE 80

ENV ASPNETCORE_URLS=http://+:80

FROM mcr.microsoft.com/dotnet/sdk:5.0-focal AS build
WORKDIR /src
COPY ["FreeCMS.csproj", "./"]
RUN dotnet restore "FreeCMS.csproj"
COPY . .
RUN dotnet build "FreeCMS.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FreeCMS.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FreeCMS.dll"]
