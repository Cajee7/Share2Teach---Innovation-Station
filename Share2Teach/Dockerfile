#Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

#Setting working directory in container
WORKDIR /app

#Copying .csproj file and restoring dependancies
COPY Share2Teach.csproj ./
RUN dotnet restore

#Copying rest of the application
COPY . ./

#BUILDING APP
RUN dotnet publish -c Release -o out

#Using ASP>NET Core as runtime env
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

#Exposing on port 80
EXPOSE 80

#Finally running
ENTRYPOINT ["dotnet", "Share2Teach.dll"]

