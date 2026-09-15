#!/bin/bash

tmux kill-session -t GTSPolandHiring-dev 2>/dev/null && echo "Zatrzymano sesję dev tmux." || echo "Sesja dev tmux nie była uruchomiona."

if [ "$(docker ps -q -f name=GTSPolandHiringDbDev)" ]; then
    echo "Stoping Database Container (GTSPolandHiringDbDev)..."
    docker stop GTSPolandHiringDbDev >/dev/null
    echo "Stopped Database Container."
elif [ "$(docker ps -a -q -f name=GTSPolandHiringDbDev)" ]; then
    echo "Database Container Is Already Stopped"
else
    echo "Database Container Doesn't exist"
fi