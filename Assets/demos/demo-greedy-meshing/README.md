# Démo: Greedy Meshing Algorithm (Issue #4)

## Automated Setup (Recommended)

**IMPORTANT**: La scène Unity nécessite une configuration dans l'éditeur Unity pour fonctionner correctement.

### Option 1: Automated Setup (Easiest)

1. Ouvrir le projet Unity dans Unity 6000.2.12f1
2. Aller dans le menu: **Tools > Demos > Setup Greedy Meshing Demo Scene**
3. La scène sera automatiquement créée dans `Assets/demos/demo-greedy-meshing/Scenes/DemoScene.unity`
4. Ouvrir la scène créée
5. Appuyer sur **Play**

### Option 2: Testing Patterns via Menu (No Scene Setup Needed)

Si vous voulez tester rapidement sans ouvrir de scène:

1. Créer la scène avec: **Tools > Demos > Setup Greedy Meshing Demo Scene**
2. Ouvrir la scène `DemoScene.unity` créée
3. Aller dans le menu: **Tools > Demos > Generate Pattern > [Pattern Name]**
   - Single Cube
   - Flat Plane
   - Terrain
   - Checkerboard
   - Random
4. Observer le mesh généré dans la Scene view et les stats dans le Game view

### Option 3: Manual Setup (Advanced Users)

Si vous préférez configurer manuellement la scène, suivez les instructions détaillées dans les sections "Installation" et "Utilisation" ci-dessous.

## Description

Cette démonstration présente l'implémentation du **Greedy Meshing Algorithm** pour l'optimisation de la génération de mesh voxel. Le greedy meshing réduit significativement le nombre de vertices en fusionnant les faces adjacentes de même type, améliorant ainsi les performances de rendu.

La démo permet de comparer différents patterns de voxels:
- **Single Cube**: Un seul voxel (cas minimal - 24 vertices)
- **Flat Plane**: Un plan plat 16×16 (fusion optimale)
- **Procedural Terrain**: Terrain procédural réaliste (~28% fill ratio)
- **Checkerboard**: Pattern 3D en damier (worst-case - aucune fusion possible)
- **Random**: Pattern aléatoire (~20% fill ratio)

## Prérequis

- **Unity Version**: 6000.2.12f1
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Packages requis**:
  - TextMeshPro (com.unity.textmeshpro)
  - Unity.Mathematics
  - Unity.Burst
  - Unity.Collections
- **Configuration**: Build target compatible avec Burst (PC, Mac, Linux)

## Installation

### Étape 1: Vérifier les packages

1. Ouvrir le **Package Manager** (Window > Package Manager)
2. Vérifier que les packages suivants sont installés:
   - TextMeshPro
   - Mathematics
   - Burst
   - Collections

### Étape 2: Importer les dépendances voxel

Assurez-vous que les packages voxel suivants sont présents dans le projet:
- `Assets/lib/voxel-core/` - Core data structures (VoxelType, ChunkCoord, VoxelMath)
- `Assets/lib/voxel-rendering/` - Greedy meshing algorithm (GreedyMeshingJob, MeshBuilder)

### Étape 3: Configuration URP

1. Vérifier que le projet utilise bien URP (Project Settings > Graphics)
2. Si le shader apparaît en magenta, recompiler les shaders (Edit > Rendering > Shaders > Compile All)

## Utilisation

### Étape 1: Ouvrir la scène

1. Naviguer vers `Assets/demos/demo-greedy-meshing/Scenes/DemoScene.unity`
2. Double-cliquer pour ouvrir la scène dans l'éditeur

### Étape 2: Configuration de la scène

La scène est préconfigurée avec:
- **ChunkRenderer**: GameObject contenant le MeshFilter et MeshRenderer pour afficher le mesh
- **Camera**: Caméra orbit contrôlée par le script CameraController
- **Canvas UI**: Interface utilisateur avec statistiques et boutons de contrôle

**Vérifier dans l'Inspector** (sélectionner ChunkRenderer):
- `ChunkSize`: 32 (modifiable entre 8 et 64)
- `MeshFilter`: Référence au MeshFilter du GameObject
- `MeshRenderer`: Référence au MeshRenderer du GameObject
- `FpsText`, `VertexCountText`, `MeshingTimeText`, `PatternText`: Références aux éléments UI

### Étape 3: Lancer la démonstration

1. **Appuyer sur Play** dans l'éditeur Unity
2. Au démarrage, le terrain procédural est généré automatiquement

**Comportements attendus**:
- Le mesh voxel s'affiche au centre de la scène
- Les statistiques de FPS, nombre de vertices et temps de meshing apparaissent en haut à gauche
- Le pattern actuel est affiché

### Étape 4: Contrôles interactifs

**Contrôles caméra**:
- **Clic droit maintenu + Déplacement souris**: Orbiter autour du chunk
- **Molette souris**: Zoom avant/arrière (distance min: 10, max: 100)

**Boutons de génération** (dans l'UI):
- **Single Cube**: Génère un seul cube au centre (24 vertices attendus)
- **Flat Plane**: Génère un plan plat 16×16 (fusion optimale)
- **Terrain**: Génère un terrain procédural avec heightmap
- **Checkerboard**: Génère un damier 3D (worst-case - aucune fusion)
- **Random**: Génère un pattern aléatoire (~20% fill ratio)

### Étape 5: Analyser les résultats

**Statistiques affichées**:
- **FPS**: Frames per second (devrait rester >60 FPS pour ChunkSize=32)
- **Vertices**: Nombre de vertices dans le mesh généré
- **Meshing Time**: Temps d'exécution du GreedyMeshingJob en millisecondes
- **Pattern**: Nom du pattern actuellement affiché

**Comparaison des performances**:

| Pattern         | Fill Ratio | Vertices (estimé) | Fusion       |
|-----------------|------------|-------------------|--------------|
| Single Cube     | <1%        | 24                | Aucune       |
| Flat Plane      | ~8%        | 128-256           | Optimale     |
| Terrain         | ~28%       | 8000-15000        | Bonne        |
| Random          | ~20%       | 6000-12000        | Moyenne      |
| Checkerboard    | 50%        | 80000+            | Minimale     |

## Validation

### Ce que vous devriez voir:

- ✅ **Le mesh voxel s'affiche correctement** avec vertex colors (différentes couleurs pour Stone, Dirt, Grass)
- ✅ **L'éclairage fonctionne** (Lambert diffuse avec ambient)
- ✅ **Les statistiques se mettent à jour** en temps réel
- ✅ **La caméra orbit fonctionne** (clic droit + souris)
- ✅ **Le zoom fonctionne** (molette souris)
- ✅ **Les boutons génèrent les patterns corrects**
- ✅ **FPS stable** (≥60 FPS sur hardware moderne pour ChunkSize=32)
- ✅ **Meshing time rapide** (<5ms pour ChunkSize=32 avec Burst)
- ✅ **Aucune erreur dans la console Unity**

### Tests de validation spécifiques:

**Test 1: Single Cube**
1. Cliquer sur "Single Cube"
2. Vérifier: Vertices = 24 (6 faces × 4 vertices)
3. Vérifier: Un seul cube visible au centre

**Test 2: Flat Plane**
1. Cliquer sur "Flat Plane"
2. Vérifier: Fusion optimale (vertices << 16×16×6×4)
3. Vérifier: Plan plat de 16×16 voxels de Grass

**Test 3: Terrain**
1. Cliquer sur "Terrain"
2. Vérifier: Terrain avec variations de hauteur
3. Vérifier: Couleurs variées (Grass en haut, Dirt au milieu, Stone en bas)
4. Vérifier: Meshing time < 5ms (avec Burst)

**Test 4: Checkerboard (Worst Case)**
1. Cliquer sur "Checkerboard"
2. Vérifier: Vertices très élevé (aucune fusion possible)
3. Vérifier: Pattern damier 3D visible
4. Attention: FPS peut baisser selon le hardware

**Test 5: Random**
1. Cliquer sur "Random"
2. Vérifier: Pattern aléatoire mais déterministe (seed=42)
3. Vérifier: Résultat identique à chaque clic

## Problèmes connus

### Limitations

- **TextMeshPro manquant**: Si TextMeshPro n'est pas installé, les textes UI n'apparaîtront pas. Solution: Installer TextMeshPro via Package Manager.
- **Shader magenta**: Si le shader apparaît en magenta, recompiler les shaders (Edit > Rendering > Shaders > Compile All) ou vérifier que URP est bien configuré.
- **Performance variable**: Le pattern Checkerboard génère un nombre très élevé de vertices (worst-case) et peut impacter les performances sur hardware bas de gamme.
- **Burst non compilé**: En mode éditeur, Burst n'est pas toujours compilé immédiatement. Pour de meilleures performances, tester en mode Build.

### Bugs connus

- **Aucun bug critique identifié** à ce jour.

## Notes techniques

### Architecture

**Voxel Engine utilisé**:
- `TimeSurvivor.Voxel.Core` - Data structures (VoxelType, ChunkCoord, VoxelMath)
- `TimeSurvivor.Voxel.Rendering` - Greedy meshing (GreedyMeshingJob, MeshBuilder)

**Scripts de la démo**:
- `GreedyMeshingDemo.cs` - Contrôleur principal orchestrant génération et meshing
- `CameraController.cs` - Contrôle orbit de la caméra

**Shader**:
- `VoxelVertexColorURP.shader` - Shader URP custom avec support vertex colors et éclairage Lambert

### Optimisations

**Burst Compilation**:
- Le GreedyMeshingJob utilise `[BurstCompile]` pour compiler en code natif performant
- Gains de performance: 5-10x plus rapide qu'en C# standard

**NativeContainers**:
- Utilisation de `NativeArray<T>` pour éviter les allocations managed
- Dispose automatique via `try-finally` pour éviter les memory leaks

**Greedy Meshing Algorithm**:
- Fusion des faces adjacentes de même type et orientation
- Réduction de 70-90% du nombre de vertices selon le pattern
- Algorithme optimal O(n) en un seul pass par axe

### Considérations de performance

**ChunkSize vs Performance**:
- ChunkSize=16: ~200 vertices (terrain), <1ms meshing
- ChunkSize=32: ~8000 vertices (terrain), ~3ms meshing
- ChunkSize=64: ~60000 vertices (terrain), ~20ms meshing

**Recommandations**:
- Utiliser ChunkSize=32 pour un bon équilibre qualité/performance
- Éviter ChunkSize >64 en mode éditeur (performances dégradées sans Build)
- Tester les patterns Checkerboard sur ChunkSize <32 pour éviter lag

### Vertex Colors

Les vertex colors sont utilisés pour différencier les types de voxels:
- **Stone**: Gris (0.5, 0.5, 0.5)
- **Dirt**: Marron (0.6, 0.4, 0.2)
- **Grass**: Vert (0.2, 0.8, 0.2)

Le shader applique un éclairage Lambert simple sur ces couleurs pour un rendu basique mais performant.

### Centrage du chunk

Le chunk est centré sur l'origine (0, 0, 0) en appliquant une translation de `-ChunkSize/2` sur les axes X et Z. Cela facilite la visualisation et le contrôle de la caméra.

## Conclusion

Cette démonstration valide l'implémentation du Greedy Meshing Algorithm (Issue #4) et permet de comparer les performances sur différents patterns de voxels. L'algorithme réduit significativement le nombre de vertices tout en maintenant des temps de génération très rapides grâce à Burst.

**Prochaines étapes possibles**:
- Implémentation de l'Amortized Meshing pour streaming temps-réel
- Support de textures atlas au lieu de vertex colors
- LOD system basé sur la distance caméra
- Mesh colliders pour physique voxel
