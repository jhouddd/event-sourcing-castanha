docker run -d -p 2181:2181 -p 9092:9092 -e ADVERTISED_HOST=172.17.0.1 -e ADVERTISED_PORT=9092 --name kafka spotify/kafka
