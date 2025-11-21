# Démo: Terrain Plat Damier 50×50

## Description

Démonstration simple d'un terrain plat en damier avec 2 nuances de vert alternées (Grass et Leaves). Cette démo illustre l'utilisation d'un générateur de terrain personnalisé (`SimpleCheckerboardGenerator`) avec le moteur voxel existant.

## Objectifs de la Démo

- Montrer comment implémenter l'interface `IVoxelGenerator`
- Démontrer l'intégration avec le `ChunkManager` existant
- Visualiser un terrain voxel simple sans complexité algorithmique
- Fournir un exemple éducatif pour débuter avec le moteur voxel

## Prérequis

- **Unity Version**: 6000.2.12f1
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Packages requis**:
  - voxel-core
  - voxel-terrain
  - voxel-rendering

## Structure des Fichiers

```
Assets/demos/demo-checkerboard/
├── Scenes/
│   └── CheckerboardDemo.unity       # Scène Unity de démonstration
├── Scripts/
│   └── CheckerboardTerrainDemo.cs   # Script de contrôle de la démo
└── README.md                         # Ce fichier
```

**Note**: Le générateur `SimpleCheckerboardGenerator.cs` se trouve dans:
```
Assets/lib/voxel-terrain/Runtime/Generation/SimpleCheckerboardGenerator.cs
```

## Installation

### Étape 1: Créer la Scène Unity

1. Ouvrir Unity Editor
2. **File → New Scene** (ou créer une nouvelle scène vide)
3. Sauvegarder la scène: **File → Save As...**
   - Localisation: `Assets/demos/demo-checkerboard/Scenes/CheckerboardDemo.unity`

### Étape 2: Créer le GameObject de Gestion

1. Dans la Hierarchy: **Right-click → Create Empty**
2. Nommer le GameObject: `CheckerboardTerrainManager`
3. Sélectionner ce GameObject

### Étape 3: Ajouter le Script

1. Dans l'Inspector: **Add Component**
2. Chercher: `CheckerboardTerrainDemo`
3. Ajouter le composant

### Étape 4: Configurer le Script

Dans l'Inspector du script `CheckerboardTerrainDemo`:

#### Configuration
- **Voxel Config**: Assigner le ScriptableObject `VoxelConfiguration`
  - Localisation typique: `Assets/lib/voxel-core/...` ou créer un nouveau
  - Paramètres recommandés:
    - Chunk Size: 16 ou 32
    - Macro Voxel Size: 1.0
- **Terrain Material**: Assigner le material pour le rendu du terrain
  - Utiliser un material URP compatible (ex: URP/Lit)
  - Localisation typique: `Assets/lib/voxel-rendering/Materials/`

#### Terrain Size
- **Size X**: 50 (largeur en voxels)
- **Size Z**: 50 (profondeur en voxels)

#### Debug
- **Enable Debug Logs**: Coché (pour voir les logs de génération)

### Étape 5: Ajouter une Caméra et Éclairage

1. Créer une caméra si non présente: **GameObject → Camera**
   - Position recommandée: (25, 30, -20)
   - Rotation: (45, 0, 0)
   - Permet de voir le terrain en vue isométrique

2. Créer une lumière directionnelle: **GameObject → Light → Directional Light**
   - Rotation: (50, -30, 0)
   - Intensity: 1

### Étape 6: (Optionnel) Ajouter des Contrôles Caméra

Pour naviguer autour du terrain:
- Utiliser le package Cinemachine (recommandé)
- Ou créer un script simple de caméra orbital

## Utilisation

### Lancer la Démonstration

1. Ouvrir la scène `Assets/demos/demo-checkerboard/Scenes/CheckerboardDemo.unity`
2. Vérifier que tous les paramètres sont assignés dans l'Inspector
3. **Appuyer sur Play**

### Comportement Attendu

Au démarrage (mode Play):
1. Le terrain se génère **instantanément** (< 50ms pour 50×50)
2. Console affiche les logs de génération (si Debug Logs activé):
   ```
   [CheckerboardTerrainDemo] Starting terrain generation: 50×50 voxels
   [CheckerboardTerrainDemo] Generating 4 chunks (2×2) with chunk size 32
   [CheckerboardTerrainDemo] Terrain generated successfully!
   [CheckerboardTerrainDemo] Total chunks: 4
   [CheckerboardTerrainDemo] Generation time: 15.32ms
   [CheckerboardTerrainDemo] Terrain dimensions: 50×1×50 voxels
   ```
3. Terrain visible dans la vue Scene/Game

### Régénérer le Terrain

En mode Play, vous pouvez régénérer le terrain:
1. Sélectionner le GameObject `CheckerboardTerrainManager`
2. Dans l'Inspector: **Right-click sur le script → Regenerate Terrain**
3. Le terrain est recréé avec les paramètres actuels

### Modifier la Taille en Temps Réel

1. **Arrêter le mode Play**
2. Sélectionner `CheckerboardTerrainManager`
3. Modifier `Size X` et `Size Z` dans l'Inspector
4. **Relancer Play**
5. Observer le nouveau terrain généré

## Validation

### Ce que vous devriez voir:

- ✅ **Terrain plat** de 50×50 voxels (ou taille configurée)
- ✅ **Motif damier** avec 2 nuances de vert alternées
- ✅ **1 voxel de hauteur** uniquement (Y=0)
- ✅ **Génération instantanée** (< 50ms)
- ✅ **Aucune erreur** dans la Console
- ✅ **Mesh rendu correctement** avec le material assigné

### Critères de Succès:

1. **Visuel**:
   - Damier visible avec alternance de couleurs
   - Pas de trous ou voxels manquants
   - Material appliqué correctement

2. **Performance**:
   - Génération < 100ms pour 50×50
   - FPS > 60 en mode Play (terrain statique)

3. **Console**:
   - Aucune erreur (Error)
   - Aucun warning (sauf avertissements Unity standards)
   - Logs de génération affichés si Debug activé

## Problèmes Connus

### Terrain Non Visible
**Symptôme**: Le terrain ne s'affiche pas en mode Play.

**Solutions**:
1. Vérifier que `Terrain Material` est assigné
2. Vérifier que `Voxel Config` est assigné
3. Vérifier la position de la caméra (doit voir Y=0)
4. Vérifier les logs Console pour erreurs

### Material Incorrect
**Symptôme**: Terrain visible mais sans couleurs/textures.

**Solutions**:
1. Utiliser un material URP compatible
2. Vérifier que le material utilise un shader approprié
3. Tester avec un material URP/Lit simple

### Génération Lente
**Symptôme**: Génération prend plusieurs secondes.

**Solutions**:
1. Réduire la taille du terrain (Size X/Z)
2. Augmenter la taille des chunks dans VoxelConfiguration
3. Vérifier que le mode Release est utilisé (pas Debug)

## Notes Techniques

### Architecture

- **SimpleCheckerboardGenerator**: Implémente `IVoxelGenerator`
  - Génère terrain plat à Y=0 uniquement
  - Algorithme: `(x + z) % 2 == 0 ? Grass : Leaves`
  - Complexité: O(n²) où n = taille terrain

- **CheckerboardTerrainDemo**: MonoBehaviour orchestrateur
  - Crée le générateur et le ChunkManager
  - Calcule le nombre de chunks nécessaires
  - Gère le lifecycle (Start, Destroy)

### Performances

- **Génération**: ~0.5ms par chunk (16×16×16)
- **Meshing**: ~2-5ms par chunk (greedy meshing)
- **Total**: < 50ms pour 50×50 terrain (4 chunks avec chunk size 32)

### Considérations

1. **Chunk Size Impact**:
   - Chunk size 16: 16 chunks pour 50×50 (plus lent)
   - Chunk size 32: 4 chunks pour 50×50 (recommandé)
   - Chunk size 64: 1 chunk pour 50×50 (optimal pour petits terrains)

2. **VoxelType Utilisés**:
   - `VoxelType.Grass`: Vert clair (cases blanches du damier)
   - `VoxelType.Leaves`: Vert foncé (cases noires du damier)
   - `VoxelType.Air`: Tout le reste (Y > 0)

3. **Extensibilité**:
   - Facilement modifiable pour d'autres patterns
   - Peut servir de base pour terrains procéduraux simples
   - Utile pour tests de rendu et performance

## Code Source

### SimpleCheckerboardGenerator.cs

Localisation: `Assets/lib/voxel-terrain/Runtime/Generation/SimpleCheckerboardGenerator.cs`

**Responsabilités**:
- Implémente `IVoxelGenerator`
- Génère terrain damier plat
- Thread-safe pour Unity Jobs

**Méthodes clés**:
- `Generate(ChunkCoord, int, Allocator)`: Génération chunk complet
- `GetVoxelAt(int, int, int)`: Query point unique

### CheckerboardTerrainDemo.cs

Localisation: `Assets/demos/demo-checkerboard/Scripts/CheckerboardTerrainDemo.cs`

**Responsabilités**:
- Orchestration de la démo
- Création générateur + ChunkManager
- Validation configuration
- Cleanup ressources

**Méthodes publiques**:
- `RegenerateTerrain()`: Régénère le terrain (Context Menu)

## Références

- **Voxel Engine Documentation**: `Assets/lib/voxel-core/Documentation~/`
- **IVoxelGenerator Interface**: `Assets/lib/voxel-core/Runtime/Interfaces/IVoxelGenerator.cs`
- **ChunkManager**: `Assets/lib/voxel-terrain/Runtime/Chunks/ChunkManager.cs`

## Support

Pour toute question ou problème:
1. Vérifier les logs Console (Enable Debug Logs)
2. Consulter la documentation du moteur voxel
3. Tester avec un terrain plus petit (10×10)

---

**Version**: 1.0
**Date**: 2025-11-21
**Auteur**: TimeSurvivor Voxel Engine Demo
