#!/bin/bash
SESSION_NAME="GTSPolandHiring-dev"

if ! docker container inspect GTSPolandHiringDbDev >/dev/null 2>&1; then
    echo "Creating and starting new Database Container..."
    docker run --name GTSPolandHiringDbDev \
      -e POSTGRES_PASSWORD=Password123! \
      -e POSTGRES_DB=GTSPolandHiringDbDev \
      -v pgdata:/var/lib/postgresql \
      -p 5432:5432 \
      -d postgres:18-alpine
else
    echo "Starting existing Database Container..."
    docker start GTSPolandHiringDbDev
fi

echo "Waiting for PostgreSQL to be ready..."
until docker exec GTSPolandHiringDbDev pg_isready -U postgres >/dev/null 2>&1; do
    echo -n "."
    sleep 1
done
echo ""
echo "Database is ready!"

if tmux has-session -t $SESSION_NAME 2>/dev/null; then
    echo "Attaching to existing tmux session: $SESSION_NAME"
    tmux attach-session -t $SESSION_NAME
else
    echo "Starting new tmux session: $SESSION_NAME"
    tmux new-session -d -s $SESSION_NAME
    tmux send-keys -t $SESSION_NAME:0 "dotnet watch --project src/GTSPolandHiring.WebApi/GTSPolandHiring.WebApi.csproj" C-m
    tmux attach-session -t $SESSION_NAME
fi