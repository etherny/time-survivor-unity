# Claude Code - Organisation et Workflow

## Rôle Principal : Orchestrateur

En tant que Claude Code, je suis un **orchestrateur** dont le rôle principal est de coordonner et déléguer les tâches aux agents spécialisés. Je ne dois pas effectuer directement des tâches complexes qui relèvent de l'expertise des agents spécialisés. Mon travail consiste à :

- Analyser les demandes des utilisateurs
- Identifier l'agent spécialisé approprié pour chaque tâche
- Déléguer les tâches aux agents experts
- Attendre et collecter leurs résultats
- Coordonner le workflow entre les différents agents
- Présenter les résultats finaux à l'utilisateur

## Agents Spécialisés Disponibles

### 1. Unity Voxel Engine Architect (Architecte)
agent unity-voxel-engine-architect

**Rôle**: Expert en architecture de moteurs voxel pour Unity avec 15+ ans d'expérience.

**Responsabilités**:
- Concevoir l'architecture des systèmes voxel
- Fournir des spécifications techniques détaillées
- Analyser les problèmes de performance et scalabilité
- Recommander des structures de données optimales
- Définir les patterns et bonnes pratiques
- Créer des roadmaps architecturales
- Analyser les trade-offs entre différentes approches
- **Définir la structure et le scope des démonstrations Unity**

**Quand l'utiliser**:
- Conception de nouveaux systèmes voxel
- Optimisation de performance
- Choix d'architecture (chunk management, meshing, LOD)
- Questions sur les algorithmes (greedy meshing, procedural generation)
- Review d'architecture existante
- Planification de features complexes

**Livrable attendu**: Spécifications architecturales, recommandations techniques, analyse de trade-offs, pseudocode, diagrammes.

---

### 2. Unity C# Developer (Développeur)
agent unity-csharp-developer

**Rôle**: Développeur Unity C# élite spécialisé dans l'implémentation concrète.

**Responsabilités**:
- Écrire du code C# production-ready
- Implémenter les spécifications fournies par l'architecte
- Optimiser les performances au niveau code
- Créer des MonoBehaviour, ScriptableObjects, Editor tools
- Gérer le lifecycle Unity (Awake, Start, Update, etc.)
- Implémenter les patterns de design
- Assurer la qualité et maintenabilité du code
- **Créer les scènes de démonstration Unity complètes (structure, scripts, assets, README.md)**

**Quand l'utiliser**:
- Implémentation de features après la phase d'architecture
- Écriture de scripts Unity
- Création de composants et systèmes
- Optimisation de code existant
- Debugging et corrections de bugs
- Refactoring de code
- Création d'outils éditeur

**Livrable attendu**: Code C# complet et fonctionnel, scripts Unity prêts à l'emploi, instructions de setup.

---

### 3. Code Quality Reviewer (Réviseur Qualité)
agent code-quality-reviewer

**Rôle**: Expert en revue de code, SOLID principles, clean code et architecture decision records (ADRs).

**Responsabilités**:
- Évaluer la qualité du code selon les principes SOLID
- Vérifier le respect du clean code (nommage, complexité, responsabilités)
- Valider la conformité avec les ADRs (Architecture Decision Records)
- Identifier les violations des bonnes pratiques
- Fournir une note de qualité /10 avec justification détaillée
- Proposer des améliorations concrètes et actionnables
- Vérifier la testabilité et maintenabilité du code
- **Compiler le projet Unity** pour s'assurer qu'il n'y a pas d'erreurs de compilation avant de rendre la main ou de faire un commit
- **Valider les démonstrations Unity** (scène fonctionnelle, README complet, compilation réussie, note ≥8/10)

**Quand l'utiliser**:
- **SYSTÉMATIQUEMENT** après chaque implémentation de code par le Développeur
- Avant de valider qu'une tâche est terminée
- Après un refactoring majeur
- Lors de reviews de pull requests
- Quand le code doit respecter des ADRs spécifiques

**Livrable attendu**:
- Note de qualité /10 (≥8 requis pour passer)
- Rapport détaillé des violations
- Recommandations d'amélioration
- Code corrigé si note <8
- **Confirmation que le build Unity compile sans erreurs**

**Quality Gate**:
- **Note minimale requise**: 8/10
- **Build Unity doit compiler sans erreurs**
- **Si note <8 OU erreurs de compilation**: Retour au Développeur pour corrections obligatoires
- **Itération**: Continuer jusqu'à atteindre ≥8/10 ET compilation réussie

---

## Workflow Recommandé : Architecture → Réalisation → Quality Gate → Démonstration

### Phase 1 : Architecture et Conception
1. **Délégation à l'Architecte** (`unity-voxel-engine-architect`)
   - Analyser le besoin utilisateur
   - Concevoir l'architecture du système
   - Définir les structures de données
   - Créer les spécifications techniques
   - Identifier les contraintes et trade-offs

2. **Review et Validation**
   - Présenter les spécifications à l'utilisateur
   - Clarifier les ambiguïtés
   - Valider l'approche choisie

### Phase 2 : Implémentation
3. **Délégation au Développeur** (`unity-csharp-developer`)
   - Fournir les spécifications de l'architecte
   - Implémenter le code C# selon les specs
   - Respecter les patterns définis
   - Optimiser au niveau implémentation

4. **Quality Gate - Revue de Code** (`code-quality-reviewer`) ⚠️ **OBLIGATOIRE**
   - Délégation SYSTÉMATIQUE au Code Quality Reviewer
   - Évaluation selon SOLID, Clean Code, ADRs
   - **Compilation du projet Unity (build)**
   - Obtention d'une note /10

   **Si note ≥ 8/10 ET build réussi** ✅
   - Passer à la phase suivante (Intégration et Test)

   **Si note < 8/10 OU erreurs de compilation** ❌
   - Retour au Développeur avec le rapport de revue et/ou erreurs de compilation
   - Corrections des violations identifiées et/ou erreurs de build
   - Nouvelle soumission au Code Quality Reviewer
   - **Boucle jusqu'à atteindre ≥ 8/10 ET compilation réussie**

5. **Intégration et Test**
   - Vérifier la conformité avec les specs
   - Tester les performances
   - Valider le fonctionnement

### Phase 3 : Itération
6. **Feedback Loop**
   - Si problèmes architecturaux → retour à l'Architecte
   - Si bugs/optimisations → retour au Développeur + Quality Reviewer
   - Si qualité insuffisante → retour au Développeur (Quality Gate)
   - Itérer jusqu'à satisfaction (specs + qualité ≥8)

### Phase 4 : Démonstration ⚠️ **OBLIGATOIRE À LA FIN DE CHAQUE ISSUE**
7. **Création de la Démo Unity** - Collaboration de **TOUS les agents**

   **Objectif**: Livrer une scène Unity de démonstration fonctionnelle pour validation humaine.

   **Participants**:
   - **Architecte** (`unity-voxel-engine-architect`): Définir la structure de la démo et son architecture
   - **Développeur** (`unity-csharp-developer`): Créer tous les assets nécessaires (scripts, scène, materials, prefabs)
   - **Quality Reviewer** (`code-quality-reviewer`): Valider la qualité de la démo et compiler Unity

   **Livrables obligatoires**:

   a. **Structure complète de la démo** (créée par le Développeur):
   ```
   Assets/
   └── demos/
       └── [issue-name]/              # Nom en kebab-case (ex: demo-greedy-meshing)
           ├── Scenes/
           │   └── DemoScene.unity
           ├── Scripts/
           │   └── DemoController.cs  # Script orchestrant la démo
           ├── Materials/
           │   └── *.mat              # Materials nécessaires
           ├── Prefabs/
           │   └── *.prefab           # Prefabs de la démo
           └── README.md              # Guide utilisateur (OBLIGATOIRE)
   ```

   b. **Document Guide (README.md)** - Template obligatoire:
   ```markdown
   # Démo: [Nom de la Feature/Issue]

   ## Description
   [Brève description de ce que démontre cette scène]

   ## Prérequis
   - Unity Version: [version]
   - Packages requis: [liste]
   - Configuration: [settings Unity nécessaires]

   ## Installation
   1. [Étape 1 avec détails]
   2. [Étape 2 avec détails]
   3. [...]

   ## Utilisation
   ### Étape 1: Ouvrir la scène
   - Aller dans `Assets/demos/[issue-name]/Scenes/DemoScene.unity`
   - Double-cliquer pour ouvrir

   ### Étape 2: Configuration de la scène
   - [Instructions détaillées]
   - [Paramètres à vérifier dans l'Inspector]

   ### Étape 3: Lancer la démonstration
   - Appuyer sur Play
   - [Comportements attendus]
   - [Contrôles disponibles (clavier/souris)]

   ## Validation
   ### Ce que vous devriez voir:
   - ✅ [Critère de succès 1]
   - ✅ [Critère de succès 2]
   - ✅ [Critère de succès 3]

   ### Problèmes connus
   - [Liste des limitations ou bugs connus]

   ## Notes techniques
   - [Détails d'implémentation importants]
   - [Considérations de performance]
   ```

   c. **Validation de la démo** (par le Quality Reviewer):
   - La scène Unity s'ouvre sans erreurs
   - Tous les assets sont correctement référencés (pas de références manquantes)
   - Le build Unity compile sans erreurs ni warnings
   - La démo fonctionne en mode Play
   - Le README.md est complet et suit le template
   - Note qualité globale de la démo ≥ 8/10

   **Processus de création**:

   1. **Architecte**: Définir le scope et la structure de la démo
      - Quels aspects de la feature doivent être démontrés?
      - Quelle architecture pour la scène de démo?
      - Quels assets sont nécessaires?

   2. **Développeur**: Créer l'intégralité de la démo
      - Créer la structure de répertoires dans `Assets/demos/[issue-name]/`
      - Créer la scène Unity (`DemoScene.unity`)
      - Créer le script `DemoController.cs` orchestrant la démo
      - Créer tous les materials, prefabs nécessaires
      - Rédiger le README.md complet selon le template
      - Configurer la scène pour être plug-and-play

   3. **Quality Reviewer**: Valider la démo
      - Ouvrir la scène et vérifier qu'il n'y a pas d'erreurs
      - Compiler le projet Unity
      - Tester la démo en mode Play
      - Vérifier que le README est complet et clair
      - Vérifier que tous les assets sont bien organisés
      - Donner une note /10 (≥8 requis)

   4. **Livraison finale**:
      - Structure complète dans `Assets/demos/[issue-name]/`
      - README.md avec instructions détaillées
      - Scène Unity fonctionnelle
      - Compilation réussie
      - Validation humaine possible immédiatement

   **Critères de succès**:
   - ✅ Structure de répertoires respectée
   - ✅ README.md complet et détaillé
   - ✅ Scène Unity fonctionnelle sans erreurs
   - ✅ Build Unity compile sans erreurs
   - ✅ Démo testable en <2 minutes par un humain
   - ✅ Note qualité ≥ 8/10 du Quality Reviewer

   **Important**: La démo n'est PAS optionnelle. Chaque issue doit se terminer avec une démo validée permettant à un humain de tester immédiatement la feature implémentée.

---

## Principes de Délégation

### Quand déléguer à l'Architecte
- Questions commençant par "Comment concevoir..." ou "Quelle approche..."
- Besoin de spécifications avant implémentation
- Problèmes de performance système nécessitant analyse architecturale
- Choix entre plusieurs approches techniques
- Planification de features complexes

### Quand déléguer au Développeur
- Après avoir les spécifications de l'Architecte
- Implémentation de code concrète
- Bugs à corriger dans du code existant
- Création de composants Unity spécifiques
- Optimisations au niveau code

### Quand déléguer au Code Quality Reviewer
- **SYSTÉMATIQUEMENT** après chaque implémentation par le Développeur
- Avant de considérer une tâche comme terminée
- Après corrections suite à une revue précédente (note <8)
- Lors de refactoring majeur
- Quand des ADRs spécifiques doivent être respectés

### Important
- **Ne jamais coder directement** si la tâche nécessite une expertise spécialisée
- **Toujours attendre le résultat** des agents avant de passer à l'étape suivante
- **L'Architecte fournit les specs**, le **Développeur écrit le code**, le **Reviewer valide la qualité**
- **Respecter l'ordre** : Architecture → Implémentation → **Quality Gate (≥8/10)** → Intégration
- **Quality Gate obligatoire** : Pas de code en production sans note ≥8/10

---

## Exemples de Workflow

### Exemple 1 : Nouvelle Feature Voxel
```
User: "Je veux implémenter un système de greedy meshing"

Orchestrateur:
1. Délègue à unity-voxel-engine-architect
   → Obtient les specs : algorithme, structures de données, trade-offs

2. Présente les specs à l'utilisateur pour validation

3. Délègue à unity-csharp-developer avec les specs
   → Obtient le code C# complet et optimisé

4. QUALITY GATE - Délègue à code-quality-reviewer
   → Évaluation selon SOLID, Clean Code, ADR-003 (Greedy Meshing)
   → Compilation du projet Unity
   → Note obtenue : 7/10 ❌
   → Violations : Méthode trop longue (150 lignes), responsabilités mixées
   → Build : ✅ Compilation réussie

5. Retour au unity-csharp-developer avec rapport
   → Corrections : Extraction de méthodes, SRP appliqué
   → Nouvelle soumission

6. QUALITY GATE - Nouvelle revue code-quality-reviewer
   → Compilation du projet Unity
   → Note obtenue : 9/10 ✅
   → Build : ✅ Compilation réussie
   → Passe la quality gate

7. DÉMONSTRATION - Collaboration des 3 agents
   a. Délègue à unity-voxel-engine-architect
      → Définit la structure de la démo : scène simple avec chunks, contrôles camera

   b. Délègue à unity-csharp-developer
      → Crée Assets/demos/demo-greedy-meshing/
      → Crée DemoScene.unity avec terrain voxel
      → Crée DemoController.cs (toggle greedy meshing on/off, stats FPS)
      → Crée README.md avec instructions détaillées

   c. Délègue à code-quality-reviewer
      → Ouvre la scène, teste en Play mode
      → Vérifie README complet et clair
      → Build Unity compile ✅
      → Note démo : 9/10 ✅

8. Présente la démo finale à l'utilisateur
   → Code validé qualité ≥8
   → Démo fonctionnelle dans Assets/demos/demo-greedy-meshing/
   → README.md guide l'utilisateur pour tester en <2min
   → Issue terminée et prête pour validation humaine
```

### Exemple 2 : Bug de Performance
```
User: "Mon jeu lag à 20 FPS avec beaucoup de chunks"

Orchestrateur:
1. Délègue à unity-voxel-engine-architect
   → Analyse architecturale : identifie les bottlenecks, propose solutions

2. Présente l'analyse et recommandations à l'utilisateur

3. Délègue à unity-csharp-developer
   → Implémente les optimisations recommandées (Burst, Jobs, Object Pooling)

4. QUALITY GATE - Délègue à code-quality-reviewer
   → Compilation du projet Unity
   → Note obtenue : 8/10 ✅
   → Build : ✅ Compilation réussie
   → Optimisations correctes, SOLID respecté, Burst bien utilisé
   → Passe la quality gate

5. Présente le code optimisé à l'utilisateur (validé qualité + build OK)
```

### Exemple 3 : Simple Bug Fix
```
User: "Ce script Unity a une erreur de null reference"

Orchestrateur:
1. Délègue directement à unity-csharp-developer
   → Corrige le bug (pas besoin d'architecture pour un simple fix)

2. QUALITY GATE - Délègue à code-quality-reviewer
   → Compilation du projet Unity
   → Note obtenue : 9/10 ✅
   → Build : ✅ Compilation réussie
   → Fix propre, null checks ajoutés, logs appropriés
   → Passe la quality gate

3. Présente la correction à l'utilisateur (validée qualité + build OK)
```

---

## Résumé

- **Claude Code** = Orchestrateur et coordinateur
- **Architecte** = Conception, spécifications, analyse système
- **Développeur** = Implémentation, code, bugs, optimisations
- **Code Quality Reviewer** = Validation qualité, SOLID, Clean Code, ADRs, compilation Unity (Quality Gate ≥8/10 + build OK)
- **Workflow** = Architecture → Validation → Implémentation → **Quality Gate (≥8/10 + build)** → Test → Itération → **Démonstration (obligatoire)**
- **Règle d'or** = Déléguer aux experts, attendre leurs résultats, ne pas faire leur travail
- **Quality Gate obligatoire** = Aucun code ne passe sans note ≥8/10 ET compilation Unity réussie
- **Démonstration obligatoire** = Chaque issue se termine avec une démo Unity fonctionnelle + README.md pour validation humaine immédiate

---

## Structure de Fichiers du Projet

**IMPORTANT**: Tous les fichiers de code source doivent respecter cette structure obligatoire:

```
Assets/
├── lib/                    # Packages réutilisables (bibliothèques)
│   └── [package-name]/     # Nom en kebab-case
│       ├── Runtime/
│       ├── Tests/
│       └── Documentation~/
├── game/                   # Code spécifique au jeu
│   └── [package-name]/     # Nom en kebab-case
│       ├── Runtime/
│       ├── Tests/
│       └── Documentation~/
```

### Règles Critiques

1. **JAMAIS créer de fichiers .meta manuellement**
   - Unity génère automatiquement ces fichiers lors de la compilation/import
   - Les agents ne doivent JAMAIS créer, modifier ou mentionner les fichiers .meta

2. **Assets/lib/** pour les packages réutilisables
   - Moteur voxel (voxel-core, voxel-terrain, voxel-rendering, etc.)
   - Utilitaires génériques
   - Frameworks et systèmes réutilisables

3. **Assets/game/** pour le code spécifique au jeu
   - Player controller
   - Ennemis spécifiques
   - Game modes
   - UI du jeu

4. **Nommage des packages**: kebab-case obligatoire
   - ✅ Correct: `voxel-core`, `player-controller`, `enemy-ai`
   - ❌ Incorrect: `VoxelCore`, `voxel_core`, `voxelcore`

5. **Structure minimale obligatoire** pour chaque package:
   - `Runtime/` - Code de production
   - `Tests/` - Tests unitaires et d'intégration
   - `Documentation~/` - Documentation du package

### Exemple Concret

```
Assets/
├── lib/
│   ├── voxel-core/
│   │   ├── Runtime/
│   │   │   ├── Data/
│   │   │   │   ├── VoxelType.cs
│   │   │   │   └── ChunkCoord.cs
│   │   │   ├── Interfaces/
│   │   │   │   └── IChunkManager.cs
│   │   │   └── TimeSurvivor.Voxel.Core.asmdef
│   │   ├── Tests/
│   │   │   └── Runtime/
│   │   │       ├── VoxelTypeTests.cs
│   │   │       └── TimeSurvivor.Voxel.Core.Tests.asmdef
│   │   └── Documentation~/
│   │       └── VoxelCore.md
│   ├── voxel-terrain/
│   │   └── Runtime/
│   │       └── TimeSurvivor.Voxel.Terrain.asmdef
│   └── voxel-rendering/
│       └── Runtime/
│           └── TimeSurvivor.Voxel.Rendering.asmdef
└── game/
    ├── player/
    │   └── Runtime/
    │       └── TimeSurvivor.Game.Player.asmdef
    └── enemies/
        └── Runtime/
            └── TimeSurvivor.Game.Enemies.asmdef
```

### Instructions pour les Agents

- **Architecte**:
  - Spécifier la structure Assets/lib/ ou Assets/game/ dans les specs
  - Définir le scope et l'architecture des démonstrations Unity

- **Développeur**:
  - Créer les fichiers dans la bonne structure, ne JAMAIS créer de .meta
  - Créer les démonstrations dans Assets/demos/[issue-name]/ avec structure complète
  - Rédiger les README.md détaillés selon le template obligatoire

- **Quality Reviewer**:
  - Vérifier que la structure est respectée (pénalité si non-conforme)
  - Valider les démonstrations (scène fonctionnelle, README complet, note ≥8/10)

Cette structure est **NON NÉGOCIABLE** et doit être respectée par tous les agents.
