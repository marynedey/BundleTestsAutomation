# BundleTestsAutomation
![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![Status](https://img.shields.io/badge/status-active-success)

**BundleTestsAutomation** est une application Windows Forms en C# conçue pour :
- La **gestion et comparaison de fichiers CSV** (notamment pour les données CANalyzer).
- La **génération automatique de fichiers `BundleManifest.xml`** pour les tests de bundles logiciels.

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

### <ins>Gestion des CSV</ins>
- Charger et afficher un **CSV standard** ou un **CSV CANalyzer** (filtré pour exclure les lignes contenant "System").
- Comparer deux CSV et **mettre en évidence les différences** (en rouge).
- Synchronisation des grilles lors du scroll pour faciliter la comparaison.

### <ins>Génération de `BundleManifest.xml`</ins>
- Génération d’un **template XML complet** avec :
  - Services et paramètres (ex: `mist_core`, `mist_securelog`).
  - Runlevels configurables (niveaux 0 à 13).
  - Packages avec attributs `digest`, `filename`, `version`, et arguments d’exécution.
  - Signature XML conforme à la norme `xmldsig`.
- **Mise à jour automatique** des packages :
  - Calcul du **SHA-256** pour les fichiers et dossiers.
  - Extraction des versions depuis les noms de fichiers.
  - Modification du **second argument du package `WirelessManager`** (`sw_id`).
  - Adaptation du nom du package `dse` pour les projets **IVECOCITYBUS** (`dsecitybus`).
- Boîtes de dialogue pour saisir : **version**, `sw_id`, et `sw_part_number`.

---

## Structure du projet


---

## Utilisation

### Charger un CSV
1. Cliquez sur **Charger un CSV** pour un fichier personnalisé.
2. Cliquez sur **Charger data CANalyzer** pour charger le fichier `data_full.csv` (filtré automatiquement).
3. Le fichier s’affiche dans la grille de gauche.

### Comparer deux CSV
1. Cliquez sur **Comparer 2 CSV**.
2. Sélectionnez successivement les deux fichiers CSV.
3. Les différences entre les deux fichiers sont **mises en évidence en rouge** dans les grilles.

### Générer le BundleManifest
1. Cliquez sur **Générer le Bundle Manifest**.
2. Saisissez les informations demandées :
   - Version (ex: `1.0.0`).
   - `sw_id` (ex: `IVECOINTERCITY3`).
   - `sw_part_number` (ex: `5803336620`).
3. Le fichier `BundleManifest.xml` est généré avec :
   - Les packages mis à jour (versions, noms, hash).
   - Les modifications spécifiques (ex: `WirelessManager`, `dse` → `dsecitybus`).

> ⚠️ **Après génération** :
> - Supprimez la ligne d’encodage XML en première ligne.
> - Ajoutez l’attribut `xmlns` dans la balise `<Signature>` :
>   ```xml
>   <Signature xmlns="http://www.w3.org/2000/09/xmldsig#">
>   ```

---

## Pré-requis
- **Système** : Windows 10 ou supérieur.
- **Framework** : .NET 8.0 (ou supérieur).
- **Accès** : Droits d’écriture dans le dossier `BundleTestsAutomation/data/`.

---

## Exemple d’utilisation
1. Lancez l’application.
2. Chargez un CSV CANalyzer via **Charger data CANalyzer**.
3. Comparez-le avec un autre CSV via **Comparer 2 CSV**.
4. Générez un `BundleManifest.xml` pour un nouveau bundle via **Générer le Bundle Manifest**.

---

## Contributions
Ce projet est développé dans le cadre d’un stage chez **Viveris** :
- **Nom** : Maryne DEY
- **Projet** : Automatisation des tests de bundles (2025)
- **Email** : [maryne.dey@viveris.fr](mailto:maryne.dey@viveris.fr)