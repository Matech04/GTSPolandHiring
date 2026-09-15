#!/bin/bash
SESSION_NAME="GTSPolandHiring-dev"

if [ ! "$(docker ps -a -q -f name=GTSPolandHiringDbDev)" ]; then
    echo "Starting New Database Container..."
    docker run --name GTSPolandHiringDbDev -e POSTGRES_PASSWORD=Password123! -e POSTGRES_DB=GTSPolandHiringDbDev -v pgdata:/var/lib/postgresql/data -p 5432:5432 -d postgres:18-alpine
else
    echo "Starting Existing Database Container..."
    docker start GTSPolandHiringDbDev
fi

tmux new-session -d -s $SESSION_NAME
tmux send-keys -t $SESSION_NAME:0 "dotnet watch --project src/GTSPolandHiring.WebApi/GTSPolandHiring.WebApi.csproj" C-m

tmux attach-session -t $SESSION_NAME
