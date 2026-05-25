## Architettura del software ##

Il database (in SQL) comunica con l'API (In C#, usando AspNetCore ed EntityFramework), l'idea è che essi si trovino su un server in LAN (un raspberry, un server dedicato, un vecchio portatile)

I Client si connetteranno alla rete domestica e saranno in grado di comunicare con il database, e ricevere le informazioni contenute in esso!

#### Come funziona? ####
- il client manda richieste al server backend
- il backend (scritto in C# con AspNetCore) esegue la logica per interrogare il database usando Entity Framework
- Entity Framework astrae il DB "rendendolo" interrogabile tramite codice, astraendo in Models:
    - il context che descrive il database
    - le altre classi che rappresentano ciascuna una tabella del DB
- Pomelo è il responsabile che trasforma le query da codice C# a SQL
