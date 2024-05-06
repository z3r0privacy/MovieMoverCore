#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "MovieMoverCore.dll"]

EXPOSE 80
EXPOSE 443

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
ENV SUBS_SearchLink=https://www.addic7ed.com/serie/{0}/{1}/{2}/Title
ENV EPGUIDE_AllShows=https://epguides.com/common/allshows.txt
ENV EPGUIDE_SearchLink=https://epguides.com/common/exportToCSVmaze.asp?maze={0}
ENV DL_Series_SearchLink=https://2ddl.ms/?q={0}
ENV JD_Method=direct
ENV JD_My_ApiPath=https://api.jdownloader.org
ENV JD_PreferredClient=
ENV JD_MaxRefreshInterval=2000
# C:\\Path\\to\download\\<jd:packagename> or /path/to/download/<jd:packagename>
ENV JD_DownloadPath=

# Secrets here
ENV PLEX_ApiToken=
ENV JD_Email=
ENV JD_Password=
ENV JD_ApiPath=
