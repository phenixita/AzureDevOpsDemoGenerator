# US05 - Import Template Privato

## User Story
Come utente avanzato  
voglio caricare un template zip privato (locale, GitHub o URL)  
cosi' da provisionare progetti con una baseline personalizzata.

## Descrizione
La UI consente tre modalita' di import: file locale, URL GitHub (con eventuale PAT), URL generico (con eventuali credenziali). Il template viene validato, estratto in area privata temporanea e poi reso selezionabile.

## Acceptance Criteria (Gherkin)
```gherkin
Feature: Importazione template privato

  Scenario: Upload da file locale
    Given l'utente seleziona la modalita' "Local Drive"
    When carica un file
    Then il sistema accetta solo file con estensione ".zip"

  Scenario: URL GitHub non valido
    Given l'utente seleziona la modalita' "GitHub"
    When inserisce un URL non GitHub o che non termina con ".zip"
    Then la UI mostra un errore
    And blocca l'invio

  Scenario: GitHub privato senza token
    Given l'utente seleziona la modalita' "GitHub"
    And abilita "Requires Authorization"
    When il token non e' fornito
    Then la UI richiede il PAT
    And blocca l'invio

  Scenario: URL autenticato senza credenziali
    Given l'utente seleziona la modalita' "URL"
    And abilita "Requires Authorization"
    When username o password non sono compilati
    Then la UI mostra un errore
    And blocca l'invio

  Scenario: Import riuscito
    Given l'utente fornisce input valido per una delle modalita' supportate
    When validazione e import terminano con successo
    Then il template privato risulta selezionato nella pagina principale
    And il template e' disponibile per il provisioning

  Scenario: Sostituzione template privato precedente
    Given esiste un template privato gia' caricato
    When l'utente importa un nuovo template privato valido
    Then il precedente template temporaneo viene eliminato
```
