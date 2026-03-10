# GestioLAN #
*Gestionale Casalingo Progettato per girare in LAN*

## Attenzione ##

Per collegarsi al database, ti serve la stringa di connessione, e si chiama `GestioLANConnection`
Ti serve anche una JWT key e si chiama `JWT-Settings:key`

*La cartella GestioLan-docs è possibile aprirla con Obsidian*

## Senza Docker (Sconsigliato)
Se l'API la esegui senza docker, ti serve creare la stringa di connessione e la JWT Key
La stringa di connessione la passi o in appsettings.json oppure ancora meglio come user secret con questo comando:
```Bash
dotnet user-secrets set "GestioLANConnection" "Server=IP_ADDRESS;Database=GestioLAN;User=YOUR_USER;Password=YOUR_PASSWORD;"
```
essa deve avere una struttura come questa:
`"GestioLANConnection": "Server=IP_ADDRESS;Port=3306;Database=GestioLAN;Uid=YOUR_USER;Pwd=YOUR_PASSWORD;`

Server      : è l'ip della macchina che fa girare il server MySQL (localhost se nella stessa macchina)
Database    : Nome del database
User Id     : Utente di MySQL / mariadb
Password    : Password di MySQL / mariadb
(per userid, nel database è meglio creare un utente NON root, il cui scopo è soltanto interagire con questo database)

Per la JWT key, alla stessa maniera, o la passi in appsettings.json oppure ancora meglio come user secret con questo comando:
```Bash
dotnet user-secrets set "JWT-Settings:key" "LaTuaKeyCreataConMinimo32Caratteri"
```

successivamente, spostati nella cartella /src/GestioLan.API ed esegui questo comando:
```Bash
dotnet run
```

e l'API sarà in funzione!
NOTA:
se questo comando lo esegui sulla tua macchina, dovrai digitare questo nel bowser:
```Plaintext
localhost:5069/swagger/index.html
```
*/swagger/index.html se sei in Developement mode, e vuoi testare l api da browser*

## Con Docker (Raccomandato)

Per usare il databse e l'api con docker basterà seguire questi step:
- (Opzionale) copiare la cartella `GestioLan-docker` nella  directory che preferisci della macchina in cui gireranno i container
- ntrare nella cartella `GestioLan-docker` e creare un file chiamato esattamente `.env`
- copiare in `.env` il contenuto di `ENV_template.txt` e sostituire i valori mancanti
- aprire il terminale ed entrare nella cartella `GestioLan-docker`
- scrivere nel terminale questo comando:
```Bash
docker compose up -d
```

## Struttura dati
personalmente mi trovo meglio a lavorare con docker usando i bind mount, per come ho creato il docker compose esso ha questa gerarchia (partendo da root)
```Plaintext
/docker/
├── compose-files/           # Qui risiedono i file di configurazione
│   ├── gestiolan/           
│   ├────── docker-compose.yaml
│   ├────── .env
└── services/                # Qui risiedono i dati dei container (Volume Mapping)
    ├── gestiolan/
    │   ├── uploads/items
    │   ├── uploads/users
    │   ├── Containers_logs/GestioLan.API
    │   └── gestiolan-mysql
    └── *otherServices*/
        └── *otherStuff*/
```
consiglio di usare la stessa struttura, anche perchè in questa maniera è piu comodo spostare i dati da un posto all'altro, oppure effettuare backup. 
Altrimenti potete sempre modificare il file docker-compose.yaml e gestire i volumi come piu volete

## IMPORTANTE
attualmente per come è costruita l'API, quando tirate su i container del db e dell'API, il container del database ancora non viene inizializzato in maniera tale da essere compatibile con l'api perche, quando viene creato, il container crea l'utente ma non crea il database compatibile con l'api

nel prossimo futuro mi dedicherò a sistemare questa cosa, anche perchè facendo brainstorming con gemini ho scoperto che l'API può essere intelligente abbastanza da "correggere" il database se quando prova a connettersi lo trova senza le migration apportate

## Idee e futuro ##
#### L'idea: ####
Il database (in SQL) comunica con l'API (In C#, AspNetCore ed EntityFramework), essi idealmente si dovrebbero trovare su un server in LAN (un raspberry, un server dedicato, un vecchio portatile)

I Client si connetteranno alla rete domestica e saranno in grado di comunicare con il database, e ricevere le informazioni contenute in esso!
i client sono/saranno: 
- sia [GestioLAN - Client Desktop](https://github.com/CookieMaker443/GestioLAN-Desktop)
- sia [GestioLAN - Client Mobile] *Ancora devo iniziare il progetto*
- sia [GestioLAN - WebApp] *Ancora devo iniziare il progetto*

#### Come funziona? ####
L'EF Permette ai client di comunicare con il Database
Pomelo traduce le query, da codice a SQL.

#### Idea per il futuro -1 ####
Un idea futura, sarà quella di creare un automazione che esegue periodicamente delle query al DB (secondo certi criteri scelti dall'utente)
e manda (tramite bot telegram per esempio) dei messaggi con delle informazioni
*può essere utile per esempio, per sapere se delle scorte di cibo stanno finendo, quindi avere una lista di cose da comprare*

#### Idea per il futuro -2 ####
Creare un MCP server come client,cosi da poter integrare delle interazioni con degli LLM
- OUTPUT: un LLM puo fare delle query e in base al contenuto del database, fare delle computazioni 
    - ( es: "consigliamo cosa preparare per cena usando gli alimenti che ho in casa" )
    - ( es: "stampa su un foglio la lista della spesa da fare")
- INPUT: un LLM puo aggiungere in maniera smart, item nel database, passandogli lo scontrino della spesa, in modo da poter categorizzare gli oggetti nuovi e inserirli correttamente! 

## Versione ##
Mariadb 11.6.0

Runtime dotnet 8.0

BCrypt 4.0.3
JwtBearer 8.0

