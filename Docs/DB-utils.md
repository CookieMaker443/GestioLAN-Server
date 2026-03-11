## Import/export da container a PC
per recuperare lo schema soltanto del database:
```bash
mariadb-dump -h 127.0.0.1 -P 3306 -u {DB_USERNAME} -p --no-data GestioLAN > database.sql
```
per recuperare i dati del database:
```bash
mariadb-dump -h 127.0.0.1 -P 3306 -u {DB_USERNAME} -p --no-create-info GestioLAN > data.sql
```
per recuperare SOLO i dati delle migrazioni di EF:
```bash
mariadb-dump -h 127.0.0.1 -P 3306 -u {DB_USERNAME} -p --no-create-info GestioLAN __EFMigrationsHistory > migrations_data.sql
```


## per buildare i container:
builda il container da codice sorgente: (puo riutilizare la cache)
```bash
docker compose -f docker-compose-build.yaml up -d
```
builda i container forzando la ricostruzione delle immagini:
```bash
docker compose -f docker-compose-build.yaml up -d --build
```
distrugge i container ed eventuali volumi:
```bash
docker compose -f docker-compose-build.yaml down
```
builda i container senza la cache precedente (difficilmeten necessario, se si usa questo servizio senza fare modifiche)
```bash
docker compose -f docker-compose-build.yaml build --no-cache
```
