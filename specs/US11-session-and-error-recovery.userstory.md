# US11 - Gestione Sessione, Errori e Diagnostica

## User Story
Come utente operativo  
voglio ricevere feedback chiaro su sessione scaduta e failure di processo  
cosi' da recuperare rapidamente senza perdere contesto.

## Descrizione
La UI gestisce molte chiamate asincrone. In caso di sessione scaduta o errore tecnico, mostra messaggi espliciti, propone reload/login e rende disponibile la diagnostica condivisibile.

## Acceptance Criteria (Gherkin)
```gherkin
Feature: Recupero sessione e gestione errori

  Scenario: Sessione scaduta su chiamata asincrona
    Given una chiamata AJAX riceve una risposta non coerente con sessione valida
    When il client intercetta la risposta
    Then propone un percorso di reload o nuovo login

  Scenario: Fallimento processo asincrono
    Given un processo asincrono fallisce
    When il backend pubblica i log di errore
    Then la UI mostra un pannello errore con dettagli diagnostici

  Scenario: Apertura diagnostica
    Given e' disponibile il pulsante "View Diagnostics"
    When l'utente apre la modale diagnostica
    Then il testo completo dei log e' disponibile per copia/incolla

  Scenario: Ripristino controlli dopo errore
    Given un tentativo di esecuzione termina con errore
    When la UI completa il ciclo di gestione errore
    Then i controlli principali tornano utilizzabili per un nuovo tentativo

  Scenario: Gestione errori noti di provisioning
    Given il polling riceve un errore noto di provisioning
    When la UI lo riconosce
    Then interrompe l'avanzamento
    And mostra una motivazione comprensibile all'utente
```
