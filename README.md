# GestioLAN #

# Gestionale Casalingo Progettato per girare in LAN #

L'idea: Il database in SQL comunica con l'API (AspNetCore) utilizzando l'EntityFramework (EF) & Pomelo
L'EF Permette ai client di comunicare con il Database
Pomelo traduce le query, da codice a SQL.

I Client saranno sia desktop che mobile, si connetteranno alla rete domestica e saranno in grado di comunicare con il database, e ricevere le informazioni contenute in esso!

TO-DO
Un idea futura, sarà quella di creare un automazione che esegue periodicamente delle query al DB (secondo certi criteri scelti dall'utente)
e manda (tramite bot telegram per esempio) dei messaggi con delle informazioni

TO-DO
Creare un MCP server come client,cosi da poter integrare delle interazioni con degli LLM
- OUTPUT: un LLM puo fare delle query e in base al contenuto del database, fare delle computazioni 
( es: "consigliamo cosa preparare per cena usando gli alimenti che ho in casa" )
( es: "stampa su un foglio la lista della spesa da fare")
- INPUT: un LLM puo aggiungere in maniera smart, item nel database, passandogli lo scontrino della spesa, in modo da poter categorizzare gli oggetti nuovi e inserirli correttamente! 

# Info Utile! #

"GestioLANConnection": "Server=IP_ADDRESS;Port=3306;Database=GestioLAN;Uid=YOUR_USER;Pwd=YOUR_PASSWORD;

questo riga in appsettings.json deve essere modificato!

Server      : è l'ip della macchina che fa girare il server MySQL (localhost se nella stessa macchina)
Database    : Nome del database
User Id     : Utente di MySQL / mariadb
Password    : Password di MySQL / mariadb

meglio usare un UserSecret

# Versione #
Mariadb 11.6.0