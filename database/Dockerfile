FROM mariadb:11.6

# Copia gli script di inizializzazione
COPY --chown=mysql:mysql database.sql /docker-entrypoint-initdb.d/01_database.sql
# Copia lo script per aggiornare le migrazioni del db
COPY --chown=mysql:mysql migrations_data.sql /docker-entrypoint-initdb.d/02_migrations.sql

# Cartella dati con permessi corretti
RUN mkdir -p /var/lib/mysql && \
    chown -R mysql:mysql /var/lib/mysql && \
    chmod 750 /var/lib/mysql

USER mysql
