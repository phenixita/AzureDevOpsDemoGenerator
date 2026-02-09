# US04 - Verifica Estensioni Richieste e Consenso

## User Story
Come amministratore di organizzazione  
voglio sapere se il template richiede estensioni non installate  
cosi' da autorizzarne l'installazione o procedere manualmente.

## Descrizione
Dopo la scelta di template e organizzazione, la UI verifica le estensioni richieste. Se mancanti, mostra elenco differenziato (Microsoft/terze parti), link e checkbox legali. Il pulsante di creazione resta bloccato finche' i consensi richiesti non sono espliciti.

## Acceptance Criteria (Gherkin)
```gherkin
Feature: Verifica estensioni richieste e consenso

  Scenario: Verifica estensioni template
    Given il template selezionato ha un file "Extensions.json"
    And l'utente ha selezionato un'organizzazione
    When la verifica prerequisiti viene eseguita
    Then il sistema controlla lo stato di installazione delle estensioni richieste

  Scenario: Nessuna estensione mancante
    Given tutte le estensioni richieste sono gia' installate
    When la verifica termina
    Then il pulsante "Create Project" viene abilitato

  Scenario: Estensioni mancanti
    Given una o piu' estensioni richieste non sono installate
    When la verifica termina
    Then la UI mostra l'elenco delle estensioni mancanti
    And mostra i consensi legali richiesti

  Scenario: Consenso non espresso
    Given esistono consensi legali obbligatori
    When l'utente non seleziona tutte le checkbox richieste
    Then il pulsante "Create Project" resta disabilitato

  Scenario: Consenso espresso
    Given esistono consensi legali obbligatori
    And l'utente seleziona tutte le checkbox richieste
    When i prerequisiti risultano soddisfatti
    Then il pulsante "Create Project" viene abilitato
```
