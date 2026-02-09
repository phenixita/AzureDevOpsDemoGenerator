# US06 - Fork Repository GitHub e Autorizzazione

## User Story
Come utente che usa template con codice GitHub  
voglio poter autorizzare GitHub e forcare i repository del template  
cosi' da ottenere pipeline collegate ai miei repository forkati.

## Descrizione
Per template che supportano fork, la UI espone opzione esplicita. Se l'utente abilita il fork, deve completare OAuth GitHub. Il token viene mantenuto in sessione e usato durante il provisioning.

## Acceptance Criteria (Gherkin)
```gherkin
Feature: Autorizzazione GitHub e fork repository

  Scenario: Visualizzazione opzione fork
    Given il template selezionato supporta il fork GitHub
    When la selezione template viene confermata
    Then la UI mostra l'opzione "Yes, I want to fork this repository"

  Scenario: Fork richiesto senza autorizzazione
    Given l'utente abilita l'opzione fork
    And non ha completato l'autorizzazione GitHub
    When prova a proseguire
    Then il pulsante "Create Project" resta disabilitato
    And viene richiesto di cliccare "Authorize"

  Scenario: OAuth GitHub riuscito
    Given l'utente avvia il flusso OAuth GitHub
    When il callback restituisce un token valido
    Then il token viene salvato in sessione
    And la creazione progetto puo' essere riabilitata

  Scenario: OAuth GitHub fallito
    Given l'utente avvia il flusso OAuth GitHub
    When il callback non restituisce un token valido
    Then la UI mostra uno stato di errore
    And il flusso di fork non procede

  Scenario: Provisioning con fork attivo
    Given il fork e' attivo
    And il token GitHub e' valido
    When parte il provisioning
    Then endpoint e pipeline vengono configurati verso il repository forkato
```
