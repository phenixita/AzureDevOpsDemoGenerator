# US09 - Generazione Artifact Template e Download Zip

## User Story
Come utente extractor  
voglio generare e scaricare il pacchetto template  
cosi' da riutilizzarlo come sorgente di provisioning.

## Descrizione
Dopo l'analisi, l'utente avvia estrazione asincrona con tracking stato. A fine processo, la UI espone il link di download zip. Il contenuto estratto viene rimosso dal server dopo il download.

## Acceptance Criteria (Gherkin)
```gherkin
Feature: Generazione e download template estratto

  Scenario: Avvio generazione artifact
    Given l'analisi progetto e' completata
    When l'utente clicca "Generate Artifacts"
    Then la UI avvia il polling dello stato
    And mostra il log di avanzamento

  Scenario: Generazione completata con successo
    Given la generazione termina senza errori
    When il processo raggiunge lo stato finale
    Then la UI mostra il link di download del file zip

  Scenario: Download zip
    Given il link di download e' disponibile
    When l'utente avvia il download
    Then il sistema restituisce il file zip
    And elimina la cartella temporanea lato server

  Scenario: Generazione con errori
    Given la generazione termina con errori
    When il log diagnostico e' disponibile
    Then la UI mostra messaggio di errore e dettagli diagnostici

  Scenario: Sessione scaduta durante processo
    Given il processo e' in corso
    When la UI riceve una risposta di sessione non valida
    Then il flusso viene interrotto
    And l'utente viene indirizzato a un percorso di recupero login/landing
```
