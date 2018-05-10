FROM microsoft/dotnet:2.0-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.0-sdk AS build
WORKDIR /src
COPY . .
RUN dotnet restore 
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

ENV ROOT_IP_ADDRESS=104.18.32.48
ENV ROOT_DOMAIN=dexih.com
ENV DNS_IP_ADDRESS=60.224.193.14
ENV DNS_EMAIL=gholland.dataexpertsgroup.com
ENV DNS_TTL=300
ENV LOG_REQUESTS=TRUE
ENV LOG_ERRORS=TRUE
ENV DNS_TXT_URL=https://dexih.dataexpertsgroup.com/api/Remote/GetTxtRecords

EXPOSE 53:53

ENTRYPOINT ["dotnet", "Dexih.Dns.dll"]
