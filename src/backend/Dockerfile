FROM microsoft/dotnet:2.2-sdk AS build-env
WORKDIR /sln

COPY ./src/backend/MathQuiz.sln ./
COPY ./src/backend/**/*.csproj ./

# Recreation of the project folder structure by the name of csproj files, since COPY cannot transfer this structure.
RUN for f in *.csproj; do \
        filename=$(basename $f) && \
        dirname=${filename%.*} && \
        mkdir $dirname && \
        mv $filename ./$dirname/; \
    done

RUN dotnet restore MathQuiz.sln \
	-s https://api.nuget.org/v3/index.json \
	--packages /nuget/packages
    
COPY ./src/backend .

RUN dotnet build MathQuiz.sln -c Release --no-restore
RUN dotnet test MathQuiz.UnitTests/MathQuiz.UnitTests.csproj --no-restore
RUN dotnet publish MathQuiz.WebApi -c Release -o ../published/Web --no-restore
	
FROM microsoft/dotnet:2.2-aspnetcore-runtime AS api-final
WORKDIR /app
COPY --from=build-env ./sln/published/Web .
ENTRYPOINT ["dotnet", "MathQuiz.WebApi.dll"]