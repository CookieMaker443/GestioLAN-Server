## Architettura repo
- [X] Refactorare la repo e l architettura del codice seguendo la mappa lavorata assieme a Claude per una migliore struttura e ciomprensione 
- [ ] Aggiungere un rate limiting, (differenziandolo per l endpoint della AI)
- [ ] implementare una chat con un LLM (potendolo cambiare)(come uno locale con Ollama o eventualente con Claude o OpenAI) e i suoi endopoint
- [X] aggiungere le interfacce come "layer" tra le classi controller e le API che riceveranno con DI (Dependency Injection) l'oggetto interfaccia (implementata dal con troller) come service da Program.cs per permettere dei test con controller (che non toccano il db ma usano l'interfaccia)

## Database
quando il container si genera, deve:
- [ ] eliminare l'utente root, o dargli una password complessa (da valutare)
- [X] importare SOLO i dati nella tabella delle migrazioni
*i dati sono opzionali*

## Docker compose
- [X] Migliorare la sicurezza dei container
- [X] aggiornare i docker compose build per fare il bind mount
- [X] aggiornare il docker compose che pulla le immagini, a pullare l immagine del database gia pronto (ancora da creare il container e da pubblicare)

## ImageController
- [ ] aggiungere controli sul file, verificare che sia un immagine .png, .jpg con i magic bytes
- [ ] se è un immagine valida, convertirla in jpeg (facendo conversioni, non solo modificando i metadati) se è caricata dall'utente
- [X] creare in src/GestioLan.API/Plugins un interfaccia per crreare pi dei puligin per i vari provider esterni di immagini per le varie categorie usando il pattern `plugin`
- [X] fare una migazione nella tabella category, aggiungere uno (o piu, da vedere) elementi NULLABILI che sono i link delle api che fungeranno da provider per quegli item (Aggiunto una stringa)
- [X] se l'item ha un valore in "barcode" e ha una categoria associata ad un endpoint API per l'immagine, e l'utente manda un FLAG "TryFetchImage" (con o senza immagine dall'utente), il controller prova a fetchare l'immagine dall API
(implementato conun booleano come variabile d'ambiente, che da prioprita alle immagini dell'utente  o delle api)

## UserController
- [ ] implementare un register, che registra senza token solo se non ci sono user, e crea il primo user admin
- [ ] implementare il register "main" solo da un utente admin loggato
- [ ] aggiungere un controllo in cui verifica che esiste almeno un admin nella lista

## ItemController
- [X] fare refactor degli endpoint dove possibile
- [X] creare i test in Items_test.http
- [X] fare una migrazione e aggiungere una colonna nullabile per dei barcode

## CategoryController
- [X] Se un user toglie una categoria, il db trova gli item che hanno quella categoria, e toglie il bit di quella cat da essi

## Client
- [ ] il client, quando aggiunge un item, posta l'immagine, col return ottiene id e nome dell'immagine, loassegna all item e posta l'item

## Server
- [X] Aggiungere i log, chi aggiunge cosa, chi legge cosa, chi prova a fare cio che non puo (non admin actions)

## Metadata provider
- [ ] provare a fetchare immagini anche doppo il PUT degli item (con modifiche) 
- [ ] a plugin stabili, provare a inserire metodi per fetchare nome, descrizione, se i plugin non possono provvedere , fanno return null