## Database
quando il container si genera, deve:
- [ ] eliminare l'utente root, o dargli una password complessa (da valutare)
- [X] importare SOLO i dati nella tabella delle migrazioni
*i dati sono opzionali*

## Docker compose
- [ ] aggiornare i docker compose build per fare il bind mount
- [X] aggiornare il docker compose che pulla le immagini, a pullare l immagine del database gia pronto (ancora da creare il container e da pubblicare)

## ImageController
- [ ] aggiungere controli sul file, verificare che sia un immagine .png, .jpg
- [ ] se è un immagine valida, convertirla in jpeg (facendo conversioni, non solo modificando i metadati)

## UserController
- [ ] implementare un register, che registra senza token solo se non ci sono user, e crea il primo user admin
- [ ] implementare il register "main" solo da un utente admin loggato
- [ ] aggiungere un controllo in cui verifica che esiste almeno un admin nella listaù

## ItemController
- [ ] fare refactor degli endpoint dove possibile
- [X] creare i test in Items_test.http

## Client
- [ ] il client, quando aggiunge un item, posta l'immagine, col return ottiene id e nome dell'immagine, loassegna all item e posta l'item

## Server
- [ ] Aggiungere i log, chi aggiunge cosa, chi legge cosa, chi prova a fare cio che non puo (non admin actions)