# US08 - Analisi Progetto per Estrazione Template

## User Story
Come utente che vuole creare un template custom  
voglio analizzare un progetto esistente  
cosi' da verificare che gli artifact necessari siano supportati prima dell'estrazione.

## Descrizione
La UI extractor permette selezione organizzazione e progetto, lettura proprieta' progetto/process template e analisi quantitativa degli artifact (team, iterazioni, WI, build, release, ecc.).

## Acceptance Criteria (Gherkin)
```gherkin
Feature: Analisi progetto nell'extractor

  Scenario: Caricamento progetti dell'organizzazione
    Given l'utente ha una sessione valida nell'extractor
    When seleziona un'organizzazione
    Then la UI mostra l'elenco dei progetti disponibili

  Scenario: Lettura proprieta' progetto
    Given l'utente seleziona un progetto
    When la chiamata delle proprieta' termina
    Then la UI mostra process template e classe associata

  Scenario: Analisi artifact
    Given organizzazione e progetto sono selezionati
    When l'utente clicca "Analyze"
    Then la UI mostra il riepilogo artifact con i conteggi

  Scenario: Warning o errori durante analisi
    Given durante l'analisi emergono warning o errori
    When il risultato viene presentato
    Then i messaggi vengono mostrati nella sezione diagnostica

  Scenario: Abilitazione generazione artifact
    Given l'analisi e' terminata
    When i risultati sono disponibili
    Then il pulsante "Generate Artifacts" e' disponibile
```
