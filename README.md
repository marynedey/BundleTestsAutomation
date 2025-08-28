# BundleTestsAutomation

![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![Status](https://img.shields.io/badge/status-active-success)

**BundleTestsAutomation** est une application Windows Forms en C# pour la gestion et comparaison de fichiers CSV et la génération automatique de fichiers `BundleManifest.xml` pour les tests de bundles logiciels.

---

## Table des matières

- [Fonctionnalités](#fonctionnalités)
- [Structure du projet](#structure-du-projet)
- [Utilisation](#utilisation)
  - [Charger un CSV](#charger-un-csv)
  - [Comparer deux CSV](#comparer-deux-csv)
  - [Générer le BundleManifest](#générer-le-bundlemanifest)
- [Pré-requis](#pré-requis)
- [Exemple d’utilisation](#exemple-dutilisation)
- [Contributions](#contributions)
- [Mainteneur](#mainteneur)

---

## Fonctionnalités

<details>
<summary>Gestion des CSV</summary>

- Charger et afficher un CSV standard dans une grille.
- Charger un CSV CANalyzer filtré.
- Comparer deux CSV et mettre en évidence les différences en rouge.
- Synchronisation des grilles lors du scroll pour faciliter la comparaison.
</details>

<details>
<summary>Génération de BundleManifest.xml</summary>

- Génération d’un template XML complet avec :
  - Services et paramètres
  - Runlevels configurables
  - Packages avec `digest`, `filename`, `version` et arguments d’exécution
  - Signature XML conforme `xmldsig`
- Mise à jour automatique des packages et calcul du SHA-256 pour fichiers et dossiers
- Modification du second argument du package `WirelessManager` (`sw_id`)
- Boîtes de dialogue pour saisir : version, `sw_id` et `sw_part_number`
</details>

---

## Structure du projet

- **MainForm.cs** : Interface principale et logique CSV / BundleManifest
- **HashUtils.cs** : Calcul SHA-256 pour fichiers et chaînes
- **BundleManifestGenerator.cs** : Génération du template et récupération du chemin du manifeste

---

## Utilisation

### Charger un CSV

1. Cliquer sur **Charger un CSV** ou **Charger data CANalyzer**  
2. Sélectionner le fichier CSV à afficher  

### Comparer deux CSV

1. Cliquer sur **Comparer 2 CSV**  
2. Sélectionner successivement les deux fichiers CSV  
3. Les différences apparaîtront en rouge

### Générer le BundleManifest

1. Cliquer sur **Générer le Bundle Manifest**  
2. Saisir : version, `sw_id`, `sw_part_number`  
3. Le manifeste est généré avec les packages mis à jour  
> ⚠️ Après génération, supprimer l’encodage XML de la première ligne et ajouter l’argument `xmlns` dans `<Signature>`

---

## Pré-requis

- Windows 10 ou supérieur
- .NET Framework 4.7.2 ou supérieur

---

## Contributions
**Nom:** Maryne DEY

**Projet:** 2025 STAGE Automatisation Tests Bundles

**Email:** maryne.dey@viveris.fr