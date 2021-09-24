FROM mcr.microsoft.com/dotnet/sdk:3.1 as build
Copy ./ /Publisher
WORKDIR /Publisher
RUN dotnet restore
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:3.1 as server
WORKDIR /app
RUN apt-get update 
RUN apt-get install telnet -y
COPY --from=build /app .
CMD ["dotnet","Publisher.dll"]

