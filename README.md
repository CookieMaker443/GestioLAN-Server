# Gestionale_casa

# questo progetto è un gestionale è progettato per essere usato in un contesto casalingo o privato, tramite la lan #

l'idea è di avere il database sql in un container che comunica solo con la API, 
l'app python che funge da API in un secondo container che comunica internamente col database, e con l'estero per ricevere richieste dai vari client

i client dovranno essere delle app e software esterne, per il pc pernsavo di implementarlo con il progetto "MyDailyNews"
per dispositivi mobile, invece un progetto separato
(una feature interessante sarebbe quella di rendere i client separabili dal server del database, per poter eventualmente collegarli con altri database sql)
