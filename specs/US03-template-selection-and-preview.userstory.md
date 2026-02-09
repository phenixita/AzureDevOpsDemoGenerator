# US03 - Selezione Template e Anteprima

## User Story
Come utente  
voglio sfogliare template per gruppo e selezionarne uno  
cosi' da capire rapidamente cosa verra' provisionato.

## Descrizione
La UI mostra una modale di selezione con gruppi, card template, descrizione, tag e immagine. La selezione aggiorna il riepilogo nella pagina principale e i metadati necessari al provisioning.

## Acceptance Criteria (Gherkin)
```gherkin
Feature: Selezione e anteprima template

  Scenario: Apertura galleria template
    Given l'utente apre la modale "Choose template"
    When il caricamento termina
    Then visualizza i template organizzati per gruppi

  Scenario: Selezione template
    Given la galleria template e' visibile
    When l'utente clicca un template
    Then il template risulta selezionato
    And la UI aggiorna nome, descrizione e immagine di anteprima

  Scenario: Conferma selezione
    Given un template e' selezionato
    When l'utente clicca "Select Template"
    Then il campo template nella pagina principale viene valorizzato

  Scenario: Messaggio informativo template
    Given il template selezionato include un messaggio informativo
    When la selezione viene confermata
    Then il messaggio viene mostrato nell'area informazioni

  Scenario: Cambio template e reset dipendenze
    Given l'utente aveva selezionato un template precedente
    When conferma un nuovo template
    Then stati dipendenti come estensioni, parametri ed errori vengono riallineati
```
