# Istruzioni per Copilot

- Sii sempre laconico, riduci le chiacchiere al massimo e vai sempre al punto.
- Risparmia le parole: sii sintetico e diretto.
- Rispondi sempre in lingua italiana, salvo richiesta esplicita di traduzione.
- Non dare sempre ragione: sii onesto e non accondiscendente.
- Se un'idea o una scelta non è brillante o efficace, dillo chiaramente.
- Se non capisci qualcosa, fai domande invece di assumere.
- Se serve, cerca informazioni sul web, ma solo da fonti attendibili.
- Sii sempre onesto e sincero, anche a costo di risultare scomodo.

# Linee guida progetto TimeFlow

## Dominio
- Web app multiutente per la gestione timesheet (ore lavorate).
- Ogni utente gestisce autonomamente: clienti, progetti, attività.
- Registrazione ore giornaliera per cliente/progetto e, opzionalmente, attività.

## Gestione utenti e autorizzazioni
- Ogni utente che si logga viene registrato nel sistema, ma di default non è autorizzato.
- Flag "Autorizzato": finché un admin non lo attiva, l'utente non può accedere alle funzionalità.
- Flag "Admin": gli utenti admin possono gestire l'elenco utenti (autorizzare/revocare, promuovere/rimuovere admin).
- Alla registrazione di un nuovo utente non autorizzato, inviare una mail di alert a tutti gli admin per notificare la richiesta di accesso.

## Stack tecnico
- ASP.NET Core Razor Pages, .NET 10.
- Autenticazione: login tramite account Microsoft (OAuth/OpenID Connect, Microsoft Identity Platform).
- Database: MySQL.
- Entità/modelli dati nominati in italiano (es. Utente, Cliente, Progetto, Attività), non in inglese.

## Funzionalità chiave
- Visualizzazione mensile del timesheet, con navigazione al mese precedente/successivo.
- Evidenziazione grafica di sabati, domeniche e festività, ma inserimento dati sempre consentito in questi giorni.
- Esportazione timesheet mensile in Excel e PDF.
- Esportazione dedicata per singolo cliente (layout diverso da quella mensile completa, da definire in seguito).

## UI/UX
- Tema chiaro (non bianco puro), tonalità soffuse azzurro/verde acqua.
- Logo e favicon già disponibili, da integrare.

## Note
- Dettagli implementativi di export ed entità dati da definire progressivamente.
