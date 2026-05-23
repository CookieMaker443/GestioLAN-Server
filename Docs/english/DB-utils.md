## Import/export from container to PC
To retrieve only the database schema:
```bash
mariadb-dump -h 127.0.0.1 -P 3306 -u {DB_USERNAME} -p --no-data GestioLAN > database.sql
```
To retrieve the database data:
```bash
mariadb-dump -h 127.0.0.1 -P 3306 -u {DB_USERNAME} -p --no-create-info GestioLAN > data.sql
```
To retrieve ONLY the EF migration data:
```bash
mariadb-dump -h 127.0.0.1 -P 3306 -u {DB_USERNAME} -p --no-create-info GestioLAN __EFMigrationsHistory > migrations_data.sql
```


## Building the containers:
Build the container from source code: (can reuse cache)
```bash
docker compose -f docker-compose-build.yaml up -d
```
Build the containers forcing image rebuild:
```bash
docker compose -f docker-compose-build.yaml up -d --build
```
Destroy the containers and any volumes:
```bash
docker compose -f docker-compose-build.yaml down
```
Build the containers without previous cache (rarely necessary, unless changes were made to this service):
```bash
docker compose -f docker-compose-build.yaml build --no-cache
```