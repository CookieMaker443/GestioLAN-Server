## Import/export da container a PC
per recuperare lo schema soltanto del database:
```bash
mariadb-dump -h 127.0.0.1 -P 3306 -u {DB_USERNAME} -p --no-data GestioLAN > schema.sql
```
per recuperare i dati del database:
```bash
mariadb-dump -h 127.0.0.1 -P 3306 -u {DB_USERNAME} -p --no-create-info GestioLAN > data.sql
```
per recuperare SOLO i dati delle migrazioni di EF:
```bash
mariadb-dump -h 127.0.0.1 -P 3306 -u {DB_USERNAME} -p --no-create-info GestioLAN __EFMigrationsHistory > migrations_data.sql
```
