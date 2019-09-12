dotnet publish -c Release -o bin/publish

docker build . -f dockerfile-alpine -t ghosts/staypuft

docker run -d -p 7000:5000 --name ghosts-staypuft-001 ghosts/staypuft

# docker save ghosts/staypuft > ~/Downloads/ghosts-staypuft.tar
# cat ghosts-staypuft.tar | docker load
