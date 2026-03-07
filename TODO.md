## Database
quando il container si genera, deve:
- eliminare l'utente root, o dargli una password complessa (da valutare)
- importare SOLO i dati nella tabella delle migrazioni
*i dati sono opzionali*

## Docker compose
- aggiornare i docker compose build per fare il bind mount
- aggiornare il docker compose che pulla le immagini, a pullare l immagine del database gia pronto (ancora da creare il container e da pubblicare)