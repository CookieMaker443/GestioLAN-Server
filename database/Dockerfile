FROM mariadb:11.6

# Copia lo script SQL (database.sql) nel container
COPY database.sql /docker-entrypoint-initdb.d/

# Copia lo script per aggiornare le migrazioni del db
COPY migrations_data.sql /docker-entrypoint-initdb.d/

# esegue lo script delle migrations copiato
