#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build
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
ENV FILES_Downloads=downloads
ENV FILES_Movies=movies
ENV FILES_Series=series
ENV FILES_KeepSuccess=5
ENV SUBS_SearchLink=https://www.addic7ed.com/search.php?search={0} {1}x{2}&amp;Submit=Search
ENV EPGUIDE_SearchLink=http://epguides.com/{0}/
ENV DL_Series_SearchLink=https://2ddl.ms/?q={0}
ENV JD_Method=direct
ENV JD_My_ApiPath=https://api.jdownloader.org
ENV JD_PreferredClient=
ENV JD_MaxRefreshInterval=2000

# Secrets here
ENV PLEX_ApiToken=
ENV JD_Email=
ENV JD_Password=
ENV JD_ApiPath=