FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /app

COPY *.csproj ./
COPY *.sh ./
COPY loadgenerator ./

RUN dotnet restore 
RUN dotnet publish -c Release -o out

FROM microsoft/dotnet:2.2-runtime AS runtime

WORKDIR /app

COPY --from=build /app/out .
COPY --from=build /app/*.sh .
CMD [ "/bin/bash", "./run.sh" ]