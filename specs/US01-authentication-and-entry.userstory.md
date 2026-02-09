# US01 - Accesso Utente e Avvio Sessione

## User Story
Come utente Azure DevOps  
voglio autenticarmi dal portale Demo Generator  
cosi' da poter avviare la creazione o l'estrazione di template nella mia organizzazione.

## Descrizione
La UI espone una pagina di ingresso con call to action di login. Il login avviene via OAuth con Azure Entra ID / Active Directory e crea una sessione applicativa. Se il browser non e' supportato (es. Internet Explorer), l'utente viene reindirizzato a una pagina dedicata.

## Acceptance Criteria (Gherkin)
```gherkin
Feature: Accesso e sessione utente

  Scenario: Avvio login dalla pagina di ingresso
    Given l'utente e' nella pagina di ingresso
    When clicca "Sign In"
    Then il sistema avvia il flusso OAuth verso Azure DevOps

  Scenario: Accesso completato con successo
    Given l'utente completa il flusso OAuth con successo
    When ritorna all'applicazione
    Then visualizza la pagina di creazione progetto
    And visualizza le organizzazioni disponibili

  Scenario: Sessione non valida
    Given la sessione utente non e' valida
    When l'utente accede a una pagina operativa
    Then il sistema reindirizza alla pagina di ingresso

  Scenario: Browser non supportato
    Given l'utente usa Internet Explorer
    When avvia il flusso di accesso
    Then il sistema reindirizza alla pagina "unsupported browser"

  Scenario: Logout
    Given l'utente e' autenticato
    When clicca "Sign out"
    Then la sessione applicativa viene svuotata
```

