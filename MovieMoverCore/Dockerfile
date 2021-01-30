#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:3.1-buster AS build
WORKDIR /src
COPY ["MovieMoverCore/MovieMoverCore.csproj", "MovieMoverCore/"]
RUN dotnet restore "MovieMoverCore/MovieMoverCore.csproj"
COPY . .
WORKDIR "/src/MovieMoverCore"
RUN dotnet build "MovieMoverCore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MovieMoverCore.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MovieMoverCore.dll"]

VOLUME /appdata
VOLUME /data
VOLUME /cert

ENV PLEX_BaseUrl=
ENV PLEX_MoviesSection=
ENV PLEX_SeriesSection=
ENV FILES_Downloads=
ENV FILES_Movies=
ENV FILES_Series=
ENV FILES_KeepSuccess=5
ENV SUBS_SearchLink=
ENV EPGUIDE_SearchLink=
ENV DL_Series_SearchLink=
ENV JD_Method=direct
ENV JD_My_ApiPath=https://api.jdownloader.org
ENV JD_PreferredClient=

# Secrets here
ENV PLEX_ApiToken=
ENV JD_Email=
ENV JD_Password=
ENV JD_ApiPath=