# US07 - Provisioning Asincrono, Stato e Risultato

## User Story
Come utente  
voglio vedere l'avanzamento della creazione progetto in tempo reale  
cosi' da sapere cosa sta succedendo e come agire in caso di errore.

## Descrizione
Dopo l'invio, la UI blocca i campi, avvia polling sullo stato e mostra messaggi incrementali con progress bar. Al termine mostra esito positivo con link al progetto oppure errore con diagnostica.

## Acceptance Criteria (Gherkin)
```gherkin
Feature: Stato provisioning progetto

  Scenario: Avvio provisioning
    Given l'utente clicca "Create Project" con input validi
    When il processo di provisioning viene avviato
    Then la UI disabilita i controlli di input
    And mostra area stato e progress bar

  Scenario: Aggiornamento progressivo stato
    Given il provisioning e' in esecuzione
    When il polling riceve nuovi messaggi dal backend
    Then la UI aggiunge i messaggi al log
    And evita la duplicazione dei messaggi gia' mostrati

  Scenario: Esito positivo
    Given il provisioning termina senza errori
    When il sistema raggiunge lo stato finale
    Then la UI mostra un messaggio di successo
    And mostra il link "Navigate to project"

  Scenario: Esito con errori
    Given il provisioning termina con errori
    When il log diagnostico e' disponibile
    Then la UI mostra un pannello di errore
    And rende disponibile il pulsante "View Diagnostics"

  Scenario: Errori bloccanti di policy o OAuth
    Given il backend restituisce un errore bloccante noto
    When la UI intercetta il messaggio
    Then interrompe il flusso di avanzamento
    And ripristina i controlli per un nuovo tentativo
```
