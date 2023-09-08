FROM mcr.microsoft.com/dotnet/sdk:7.0 AS installer-env

# Build requires 3.1 SDK
COPY --from=mcr.microsoft.com/dotnet/sdk:7.0 /usr/share/dotnet /usr/share/dotnet

COPY . /src/dotnet-function-app
RUN cd /src/dotnet-function-app && \
    mkdir -p /home/site/wwwroot && \
    dotnet publish ./ChargePlan.Api/*.csproj --output /home/site/wwwroot

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet-isolated:3.0-dotnet-isolated5.0-appservice
FROM mohsinonxrm/azure-functions-dotnet:4-isolated7.0-arm64v8
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    TZ=Europe/London \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    AzureWebJobsStorage=DefaultEndpointsProtocol=https;AccountName=sachargeplanazfn;AccountKey=OBdURgo8e49us0QIm0gmOH88tG9wg67bnvBY9j9PvIb+gzB7uYjo5/3eLfFRaxh7MHWJu6oFvHGJ+ASt6ZQfHw==;EndpointSuffix=core.windows.net

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]
EXPOSE 80





# FROM mcr.microsoft.com/dotnet/sdk:7.0-bullseye-slim-arm64v8 AS build-env

# WORKDIR /src

# COPY . .
# RUN dotnet restore ./ChargePlan.Api
# RUN dotnet publish -c Release -o /publish

# FROM mohsinonxrm/azure-functions-dotnet:4-isolated7.0 as runtime
# WORKDIR /publish

# COPY --from=build-env /publish .
# EXPOSE 80

# ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
#     TZ=Europe/London \
#     AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
#     AzureWebJobsStorage=DefaultEndpointsProtocol=https;AccountName=sachargeplanazfn;AccountKey=OBdURgo8e49us0QIm0gmOH88tG9wg67bnvBY9j9PvIb+gzB7uYjo5/3eLfFRaxh7MHWJu6oFvHGJ+ASt6ZQfHw==;EndpointSuffix=core.windows.net

# ENTRYPOINT ["dotnet", "myWebApp.dll"]


# # Build requires 3.1 SDK
# COPY --from=mcr.microsoft.com/dotnet/sdk:7.0-bullseye-slim-arm64v8 /usr/share/dotnet /usr/share/dotnet

# COPY . /src/dotnet-function-app
# RUN cd /src/dotnet-function-app
# RUN cd /src/dotnet-function-app && \
#     mkdir -p /home/site/wwwroot && \
#     dotnet publish ./ChargePlan.Api/*.csproj --output /home/site/wwwroot

# # To enable ssh & remote debugging on app service change the base image to the one below
# # FROM mcr.microsoft.com/azure-functions/dotnet-isolated:3.0-dotnet-isolated5.0-appservice
# FROM mohsinonxrm/azure-functions-dotnet:4-isolated7.0
# #FROM --platform=linux/arm mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated7.0-slim 
# ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
#     TZ=Europe/London \
#     AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
#     AzureWebJobsStorage=DefaultEndpointsProtocol=https;AccountName=sachargeplanazfn;AccountKey=OBdURgo8e49us0QIm0gmOH88tG9wg67bnvBY9j9PvIb+gzB7uYjo5/3eLfFRaxh7MHWJu6oFvHGJ+ASt6ZQfHw==;EndpointSuffix=core.windows.net

# COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]
# EXPOSE 80
