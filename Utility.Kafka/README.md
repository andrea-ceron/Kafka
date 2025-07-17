# Modulo Kafka.Utility
Il modulo implementa classi per la gestione di produzione e consumo di messaggi Kafka riguardanti la comunicazione tra diversi microservizi.

# Funzionalità principali
- **Creazione di Admin Client**: Vengono messe a disposizione classe e interfaccia per la creazione di un Admin Client Kafka, utile per la gestione dei topic e delle configurazioni del cluster.
- **Creazione di Producer Client**: Vengono messe a disposizione classe e interfaccia per la creazione di un Producer Client Kafka, utile per l'invio di messaggi ai topic.
- **Creazione di Consumer Client**: Vengono messe a disposizione classe e interfaccia per la creazione di un Consumer Client Kafka, utile per la lettura dei messaggi dai topic.

# Dependency Injection
la configurazione dei precendenti Client viene gestita dalla libreria [Utility.DependencyInjection](../DependencyInjection/README.md) che permette di registrare i servizi e le configurazioni necessarie per l'utilizzo di Kafka nei microservizi. 
A seconda del ruolo del microservizio possono essere invocate diverse funzioni che registrano i servizi necessari attraverso IServiceCollection.

# Produzione di Messaggi
Per produrre i messaggi su kafka viene utilizzata la classe ProducerServiceWithSubscription.
La classe estente BackgroudService e permette al microservizio di attivare la produzione attraverso TaskCompletionSource previa notifica da parte della funzione presente all'interno del microservizio che crea un record all'interno della tabella TransactionalOutbox.

# Consumo di Messaggi
Il consumo di messaggi avviene tramite la classe ConsumerServiceWithSubscription.
Questa classe consuma i messaggi da un topic specifico e li elabora in base alla logica definita nel microservizio. La classe estende BackgroundService e utilizza un Consumer Client Kafka per leggere i messaggi in modo asincrono.
Ogni record precedentemente prodotto dal Produttore avrà una struttura definita dalla classe OperationMessage. 
Le operazioni accettate dalla libreria sono definite invece all'interno della classe Operations e poi associate ad una funzione allàinterno della classe OperationMessageHandler che dovrà essere implementata all'interno del microservizio in cui la libreria viene installata.
Le Operazioni accettate sono:
- **Create**
- **Update**
- **Delete**se
- **Compensazione della Create**
- **Compensazione della Update**
- **Compensazione della Delete**

Le Operazioni di compensazione sono utilizzate solamente in caso di errore durante l'elaborazione di un messaggio, per garantire che lo stato dei micorservizi comunicanti rimanga coerente. 
Questo labling è stato introdotto per differenziare i ruoli delle operazioni presenti nei record TransactionalOutput che non riescono ad essere prodotti.

# Gestione degli Errori
La libreria implementa uyna gestione degli errori centralizzata attraverso una classe ErrorManagerMiddleware. 
Oltre alla gestione centralizzata di errori è anche implementato un CircuitBreaker che ha lo scopo di ritentare l'esecuzione di un messaggio in caso di errore un predeterminato numero di volte. 
Le configurazioni del circuit Breaker e del relativo timer vengono gestite dal file appsettings.development.json all'interno del microservizio produttore dei messaggi.
All'interno del CircuitBreaker è stata introdotta anche una variabile che permette di testare il funzionamento del CircuitBreaker stesso, inducendo di proposito errori. 
I dettagli della configurazione del CircuitBreaker sono presenti all'interno della classe OptionExtention. 