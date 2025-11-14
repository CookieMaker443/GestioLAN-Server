# GestioLAN #

# Gestionale Casalingo Progettato per girare in LAN #

L'idea: Il database in SQL comunica con l'API (AspNetCore) utilizzando l'EntityFramework (EF)
L'EF Permette ai client di comunicare con il Database

I Client saranno sia desktop che mobile, si connetteranno alla rete domestica e saranno in grado di comunicare con il database, e ricevere le informazioni contenute in esso!

Un idea futura, sarà quella di creare un automazione che esegue periodicamente delle query al DB (secondo certi criteri scelti dall'utente)
e manda (tramite bot telegram per esempio) dei messaggi con delle informazioni

# Info Utile! #

"GestioLANConnection": "Server=IP_ADDRESS;Database=GestioLAN;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;

questo riga in appsettings.json deve essere modificato!

Server      : è l'ip della macchina che fa girare il server MySQL (localhost se nella stessa macchina)
Database    : Nome del database
User Id     : Utente di MySQL
Password    : Password di MySQL

meglio usare un UserSecret