# US10 - Deep Link, Selezione Template e Toggle Extractor

## User Story
Come owner del tool  
voglio poter aprire l'app con parametri URL  
cosi' da preconfigurare template/eventi e abilitare funzionalita' avanzate.

## Descrizione
La pagina di ingresso supporta parametri (es. short name template, evento, URL template zip, enable extractor). Questi parametri influenzano la sessione iniziale e il comportamento della UI nelle pagine successive.

## Acceptance Criteria (Gherkin)
```gherkin
Feature: Deep link e configurazione iniziale via query string

  Scenario: Abilitazione extractor da parametro
    Given l'utente apre l'app con "enableextractor=true"
    When completa il login
    Then il link "Build your template" e le funzionalita' extractor sono abilitate

  Scenario: Preselezione template da short name
    Given l'utente apre l'app con short name template valido
    When arriva alla pagina di creazione progetto
    Then il template corrispondente risulta preselezionato

  Scenario: Risoluzione evento da parametro
    Given l'utente apre l'app con parametro evento noto
    When la pagina viene inizializzata
    Then il messaggio evento viene risolto dalla mappa eventi

  Scenario: Caricamento template privato da URL valido
    Given l'utente apre l'app con parametro "TemplateURL" valido e con zip raggiungibile
    When la validazione termina con successo
    Then il template privato viene caricato in sessione

  Scenario: URL template non valido
    Given l'utente apre l'app con parametro "TemplateURL" non valido o non scaricabile
    When la validazione fallisce
    Then la UI mostra un messaggio di errore
    And non applica la preselezione template
```
