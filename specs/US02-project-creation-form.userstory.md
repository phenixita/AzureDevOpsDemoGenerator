# US02 - Configurazione Creazione Progetto

## User Story
Come utente autenticato  
voglio compilare i dati minimi di provisioning  
cosi' da creare un nuovo progetto demo nella mia organizzazione.

## Descrizione
La UI di creazione richiede template, nome progetto e organizzazione. Il form supporta anche parametri dinamici del template e validazioni client-side/server-side sui vincoli del nome progetto.

## Acceptance Criteria (Gherkin)
```gherkin
Feature: Compilazione form di creazione progetto

  Scenario: Visualizzazione campi minimi
    Given l'utente e' nella pagina "Create Project"
    When il form viene caricato
    Then sono visibili i campi "Template", "Project Name" e "Organization"
    And Project name è un campo di testo libero
    And Organization è una scelta da menù a tendina
    And Template è una scelta da menù a tendina

  Scenario: Organizzazione non selezionata
    Given l'utente non ha selezionato un'organizzazione valida
    When clicca "Create Project"
    Then il sistema blocca l'invio
    And mostra un errore contestuale

  Scenario: Nome progetto non valido
    Given l'utente inserisce un nome progetto vuoto o non valido
    When prova a inviare il form
    Then il sistema blocca l'azione
    And mostra un messaggio di validazione

  Scenario: Parametri dinamici del template
    Given l'utente seleziona un template con parametri aggiuntivi
    When conferma la selezione del template
    Then la UI visualizza i campi parametro richiesti

  Scenario: Avvio provisioning
    Given tutti i campi obbligatori sono validi
    When l'utente invia il form
    Then il sistema avvia un processo asincrono di provisioning
    And genera un identificativo di tracking
```
