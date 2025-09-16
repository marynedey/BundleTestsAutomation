# Bundle Tests Automation

## Table des matières
1. [Présentation](#présentation)  
2. [Architecture du projet](#architecture-du-projet)  
3. [Fonctionnalités principales](#fonctionnalités-principales)  
   - [Gestion des fichiers CSV](#gestion-des-fichiers-csv)  
   - [Analyse des logs](#analyse-des-logs)  
   - [Contrôle des DTC](#contrôle-des-dtc)  
4. [Prérequis](#prérequis)  
5. [Installation](#installation)  
6. [Utilisation](#utilisation)  
   - [Interface utilisateur](#interface-utilisateur)  
   - [Chargement et comparaison de CSV](#chargement-et-comparaison-de-csv)  
   - [Analyse des logs](#analyse-des-logs-ui)  
7. [Organisation des sources](#organisation-des-sources)  
8. [Données et fichiers de référence](#données-et-fichiers-de-référence)  
9. [Évolutions prévues](#évolutions-prévues)  

---

## Présentation
**Bundle Tests Automation** est une application Windows Forms permettant :  
- de charger et comparer des fichiers CSV,  
- d’analyser automatiquement différents types de fichiers de logs,  
- de vérifier la conformité des **DTC (Diagnostic Trouble Codes)** à l’aide d’un fichier de référence.  

L’objectif est de **faciliter et automatiser les tests de bundles logiciels** ainsi que le suivi des données associées.

---

## Architecture du projet
```text
└── Exclusif/
   ├── Documentation/
   │   └── README.md
   └── Sources/
       ├── BundleTestsAutomation.sln
       └── BundleTestsAutomation/
           ├── BundleTestsAutomation.csproj
           ├── Program.cs
           ├── data/
           │   ├── Données_manquantes_tigr+pm+dl+dc.xlsx
           │   ├── Données_manquantes_tigr.xlsx
           │   ├── CSV/
           │   │   ├── data_empty.csv
           │   │   ├── data_full - Copie.csv
           │   │   └── data_full.csv
           │   ├── DTC/
           │   │   ├── DTC.csv
           │   │   └── DTC.xlsx
           │   └── Logs+confs/
           │       ├── dse/
           │       │   └── .DseLog
           │       └── eHorizonISA/
           │           ├── configuration.conf
           │           ├── imageLog.txt
           │           └── log.txt
           ├── Models/
           │   ├── Bundle/
           │   │   ├── BundleInfo.cs
           │   │   ├── PackageInfo.cs
           │   │   └── VehicleType.cs
           │   ├── CSV/
           │   │   ├── CsvRow.cs
           │   │   ├── DtcRow.cs
           │   │   └── DtcRowMap.cs
           │   └── Tester/
           │       ├── ITester.cs
           │       └── TestResult.cs
           ├── Services/
           │   ├── AppSettings.cs
           │   ├── CsvService.cs
           │   ├── LogService.cs
           │   ├── ConfTesters/
           │   │   └── ISAConfigTester.cs
           │   └── LogTesters/
           │       ├── DataCollectorLogTester.cs
           │       ├── DiagnosticLoggerLogTester.cs
           │       ├── DTCLogTester.cs
           │       └── TiGRAgentLogTester.cs
           └── UI/
               ├── MainForm.cs
               └── MainForm.resx
└── Partage/
    └── Suivi_donnees_entree/
        ├── Donnees_manquantes_tigr+pm+dl+dc.xlsx
        └── Donnees_manquantes_tigr.xlsx
```


---

## Fonctionnalités principales

### Gestion des fichiers CSV
- Chargement d’un fichier CSV avec affichage en grille.  
- Comparaison **côte à côte** de deux fichiers CSV.  
- Mise en évidence des **différences cellule par cellule**.  
- Export des différences sous forme de grille dédiée.

### Analyse des logs
- Chargement et affichage des logs dans un panneau dédié.  
- Sélection du **type de véhicule**.  
- Rafraîchissement automatique après sélection d’un fichier.  
- Décodage et interprétation en fonction du contenu :  
  - Logs **TiGR Agent**
  - Logs **ISA Config**  
  - Logs contenant des trames CAN pour les **DTC**  

### Contrôle des DTC
- Extraction des DTC depuis les fichiers logs CAN.  
- Décodage des champs SPN, FMI, occurrences, timestamp et source ECU.  
- Vérification de la présence dans le fichier de référence `DTC.csv`.  

> Les résultats sont présentés avec un code couleur (vert/rouge) dans l’interface.  

---

## Prérequis
- **.NET 8.0 SDK** ou supérieur  
- IDE recommandé : **Visual Studio 2022**  
- Librairies externes :  
  - [`CsvHelper`](https://joshclose.github.io/CsvHelper/) (gestion des fichiers CSV)  

---

## Installation
1. Cloner le dépôt dans votre environnement local :  
   ```bash
   git clone <url-du-repo>
2. Ouvrir la solution BundleTestsAutomation.sln dans Visual Studio.
3. Restaurer les dépendances NuGet :
    ```bash
    dotnet restore
4. Compiler et exécuter le projet.

---

## Utilisation

### Interface utilisateur
L’application s’ouvre en plein écran avec un menu horizontal :
- CSV : accès aux outils de gestion et comparaison de CSV.
- LOGS : accès aux outils d’analyse et de test des fichiers logs.

### Chargement et comparaison de CSV
1. Cliquer sur CSV dans le menu.
2. Choisir Charger un CSV pour afficher un fichier.
3. Choisir Comparer 2 CSV pour mettre en évidence les différences.
4. Consulter les résultats dans la grille des différences.

⚠️ Note : le fichier DTC.csv n’est pas destiné à être chargé dans cette interface (il est réservé au module de contrôle DTC).

### Analyse des logs
1. Cliquer sur LOGS dans le menu.
2. Choisir Rafraîchir / Choisir un fichier de logs.
3. Sélectionner le type de véhicule dans la liste déroulante.
4. Les résultats des tests s’affichent automatiquement :
    - en haut : contenu brut du log,
    - en bas : résultats analysés avec codes couleurs.

## Organisation des sources

### Models/

- Bundle/ : description des bundles logiciels (bundle, package, type de véhicule).
- CSV/ : définitions des structures CSV (lignes génériques, DTC, mapping).
- Tester/ : interfaces et résultats des différents testeurs.

### Services/
- CsvService : lecture générique et typée des CSV, comparaison et synchronisation d’affichage.
- LogService : utilitaires liés à la lecture des logs.
- ConfTesters/ : testeur spécifique pour les configurations ISA.
- LogTesters/ : testeurs spécifiques (TiGR, DTC).

### UI/
- MainForm.cs : interface principale (menus, panneaux dynamiques).
- MainForm.resx : ressources associées.

## Données et fichiers de référence
- data/CSV/ : fichiers CSV d’exemple (vides ou complets).
- data/DTC/DTC.csv : fichier de référence des DTC autorisés.
- data/Logs+confs/ : dossiers de logs (DSE, eHorizonISA, etc.).
- Partage/Suivi_donnees_entree/ : suivi des fichiers Excel des inputs pour les tests.