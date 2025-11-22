# Démo: Flat Checkerboard Terrain

## Description

Cette démo présente un terrain voxel **plat et STATIQUE** avec un motif en damier.

**IMPORTANT**: Cette démo ne comporte PAS de streaming dynamique - seulement 9 chunks fixes (3x3 grille).
Pour une démo de streaming dynamique avec génération infinie, voir: `demo-procedural-terrain-streamer`.

Elle démontre:
- Génération de terrain plat fixe (9 chunks de 32x32x32 voxels)
- Pattern damier visible (Grass vert / Dirt brun alternés, cases 8x8 voxels)
- Joueur sans gravité (mouvement planar fluide)
- Caméra isométrique qui suit le joueur
- Utilisation d'un générateur custom (`FlatCheckerboardGenerator`)

**Objectif**: Fournir une démonstration simplifiée et stable du moteur voxel sans complexité de terrain procédural ou streaming dynamique.

## Prérequis

- **Unity Version**: 6000.2.12f1
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Packages requis**:
  - TextMeshPro (com.unity.textmeshpro)
  - Input System (com.unity.inputsystem)
  - Universal RP (com.unity.render-pipelines.universal)
- **Configuration**:
  - VoxelConfiguration asset dans `Assets/Resources/VoxelConfiguration.asset`
  - Material voxel compatible URP

## Installation

### Option 1: Setup Automatique (RECOMMANDÉ)

1. Ouvrir Unity 6000.2.12f1
2. Charger le projet TimeSurvivorGame
3. Aller dans le menu Unity: `Tools > Voxel Demos > Setup Flat Checkerboard Terrain Demo`
4. Attendre la création automatique de la scène (5-10 secondes)
5. La scène sera sauvegardée dans `Assets/demos/demo-flat-checkerboard-terrain/Scenes/DemoScene.unity`

Le setup automatique crée:
- Lighting (Directional Light)
- Main Camera avec SimpleCameraFollow
- Player avec CharacterController + FlatTerrainPlayerController + visual capsule
- Terrain Manager avec FlatTerrainDemoController
- UI Canvas avec stats et instructions
- EventSystem avec InputSystemUIInputModule

Toutes les références sont assignées automatiquement - aucune configuration manuelle requise!

### Option 2: Setup Manuel (si nécessaire)

Si le menu Tools ne fonctionne pas:

1. Créer une nouvelle scène
2. Ajouter Directional Light (rotation: 50, -30, 0)
3. Créer GameObject "Player" avec:
   - CharacterController (radius: 0.5, height: 2, center: 0, 1, 0)
   - FlatTerrainPlayerController (moveSpeed: 10, sprintMultiplier: 2)
   - Visual: Capsule child (position: 0, 1, 0)
4. Configurer Main Camera avec:
   - SimpleCameraFollow (target: Player, offset: 0, 20, -20, smoothSpeed: 5)
5. Créer GameObject "Terrain Manager" avec:
   - FlatTerrainDemoController
   - Assigner VoxelConfiguration, Material, Player reference
6. Créer UI Canvas avec TextMeshProUGUI pour stats et instructions
7. Assigner les références UI dans FlatTerrainDemoController

## Utilisation

### Étape 1: Ouvrir la scène

- Aller dans `Assets/demos/demo-flat-checkerboard-terrain/Scenes/DemoScene.unity`
- Double-cliquer pour ouvrir

### Étape 2: Configuration de la scène

**Vérifications dans l'Inspector (si setup manuel)**:

1. Sélectionner "Terrain Manager" GameObject
2. Vérifier que FlatTerrainDemoController a:
   - `Voxel Config`: VoxelConfiguration asset
   - `Chunk Material`: Material URP
   - `Player`: Référence au Player Transform
   - `Stats Text`: Référence au TextMeshProUGUI stats
   - `Instructions Text`: Référence au TextMeshProUGUI instructions
   - `Fps Update Interval`: 1.0

3. Sélectionner "Main Camera" GameObject
4. Vérifier que SimpleCameraFollow a:
   - `Target`: Référence au Player Transform
   - `Offset`: (0, 12, -12)
   - `Smooth Follow`: coché
   - `Smooth Speed`: 5

### Étape 3: Lancer la démonstration

1. Appuyer sur **Play** dans Unity Editor
2. Attendre 2-3 secondes pour la génération initiale des chunks
3. Observer le terrain plat avec pattern damier

**Comportements attendus**:
- 9 chunks (3x1x3 grid) s'affichent au démarrage
- Pattern damier visible: alternance Grass (vert) / Dirt (brun) en cases de 8x8 voxels
- Terrain plat à hauteur fixe (8 voxels de haut)
- Joueur spawné à position (0, 2, 0) - juste au-dessus du terrain
- Caméra suit le joueur avec vue isométrique rapprochée

**Contrôles disponibles**:
- **W/A/S/D**: Déplacer le joueur (mouvement planar, pas de gravité)
- **Shift (maintenu)**: Sprint (vitesse x2)
- **Souris**: Pas de contrôle de caméra (caméra suit automatiquement)

### Étape 4: Observer les statistiques

Le panneau Stats (coin supérieur droit) affiche:
- FPS actuel
- Nombre de chunks actifs (9 / 9)
- Type de pattern (Damier Grass/Dirt)
- Taille de case (8 voxels)
- Position du joueur en temps réel

## Validation

### Ce que vous devriez voir:

- ✅ **Terrain plat visible**: 9 chunks affichés formant un plateau vert/brun
- ✅ **Pattern damier CLAIR**: Alternance Grass (vert vif) / Dirt (brun) en cases 8x8 voxels
- ✅ **Joueur sur le terrain**: Avatar rouge positionné à Y=2 (juste au-dessus du sol)
- ✅ **Caméra suit**: Vue isométrique (offset: 0, 12, -12) qui suit le joueur
- ✅ **Mouvement fluide**: WASD sans gravité, Sprint avec Shift
- ✅ **FPS élevés**: >60 FPS attendu (seulement 9 chunks statiques)
- ✅ **Pas de chunks vides**: Tous les chunks contiennent du terrain solide

### Ce que vous NE devriez PAS voir:

- ❌ **Terrain comme des "marches"**: Si c'est le cas, le joueur est mal positionné
- ❌ **Tout vert uniforme**: Si pas de pattern brun, vérifier material/shader
- ❌ **Nouveaux chunks apparaissent**: Pas de streaming dans cette démo (9 chunks fixes)

### Critères de succès détaillés:

1. **Génération correcte**:
   - Exactement 9 chunks générés
   - Coordonnées chunks: (-1,0,-1) à (1,0,1)
   - Hauteur terrain: 8 voxels (y: 0-7 solide, y: 8-63 air)

2. **Pattern damier**:
   - Alternance visible Grass/Dirt
   - Taille de case: 8x8 voxels
   - Pattern cohérent entre les chunks

3. **Mouvement joueur**:
   - WASD fonctionnel
   - Sprint x2 vitesse avec Shift
   - Pas de chute (gravité désactivée)
   - Mouvement fluide sans à-coups

4. **Caméra**:
   - Suit le joueur en continu
   - Offset isométrique maintenu (0, 12, -12)
   - Smoothing visible et agréable

5. **Performance**:
   - FPS ≥ 60 (matériel moderne)
   - Pas de freeze pendant le mouvement
   - Génération initiale < 3 secondes

## Problèmes connus

### Limitations volontaires:

- **Pas de streaming dynamique**: Les 9 chunks sont fixes, pas de génération/suppression pendant le jeu
- **Terrain limité**: Seulement 192x192 voxels (3 chunks x 64 voxels)
- **Pas de gravité**: Le joueur ne tombe pas (mouvement planar uniquement)
- **Pattern simple**: Seulement 2 types de voxels (Grass/Dirt)

### Bugs connus:

- **Aucun bug connu** - Cette démo est volontairement simplifiée pour éviter les instabilités

### Troubleshooting:

**Problème**: Chunks vides ou non affichés
- **Solution**: Vérifier que VoxelConfiguration est assigné et que le Material est compatible URP

**Problème**: Joueur tombe à travers le terrain
- **Solution**: Vérifier que CharacterController a `center = (0, 1, 0)` et que le joueur spawn à y > 8

**Problème**: FPS très bas
- **Solution**: Vérifier que URP est configuré correctement et que Greedy Meshing est activé dans VoxelConfiguration

**Problème**: Erreurs "Keyboard.current is null"
- **Solution**: Vérifier que Input System package est installé et activé dans Player Settings

## Notes techniques

### Architecture

- **Générateur**: `FlatCheckerboardGenerator` implements `IVoxelGenerator`
- **Hauteur sol**: 8 voxels (GROUND_HEIGHT = 8)
- **Taille cases damier**: 8x8 voxels (TILE_SIZE = 8)
- **Chunks**: 9 fixes (coordonnées -1 à 1 en X/Z, Y=0)
- **ChunkSize**: 32 voxels (VoxelConfiguration)
- **MacroVoxelSize**: 0.2 Unity units

### Coordonnées

- Chunk(0,0,0) position monde: (0, 0, 0)
- Surface du terrain: Y = 1.6 Unity units (top du voxel Y=7)
- Spawn joueur: (0, 2, 0) - juste au-dessus du terrain
- Caméra offset: (0, 12, -12) - vue isométrique rapprochée

### Pattern Damier

Algorithme XOR sur les coordonnées monde:
```csharp
bool isEvenTileX = ((worldX / TILE_SIZE) % 2) == 0;
bool isEvenTileZ = ((worldZ / TILE_SIZE) % 2) == 0;
return (isEvenTileX == isEvenTileZ) ? VoxelType.Grass : VoxelType.Dirt;
```

### Shader

- **Shader URP custom**: `URP/VoxelVertexColor`
- **Vertex colors**: Grass = (0.2, 0.8, 0.2), Dirt = (0.6, 0.4, 0.2)
- **Lighting**: Simple Lambert + ambient

### Limitations volontaires

- **Pas de streaming dynamique**: Les 9 chunks sont fixes, pas de génération/suppression pendant le jeu
- **Terrain limité**: 3x3 chunks = 96x96 voxels = 19.2x19.2 Unity units
- **Pas de gravité**: Mouvement planar uniquement (design voulu pour la démo)

**Contrôleur joueur**: `FlatTerrainPlayerController`
- Hérite de MonoBehaviour
- Utilise Input System (`Keyboard.current`)
- Mouvement planar uniquement (pas de gravité)
- Sprint configurable (x2 par défaut)

**Caméra**: `SimpleCameraFollow`
- Follow avec offset configurable
- Smoothing optionnel (Lerp)
- LookAt automatique vers le joueur

**Orchestration**: `FlatTerrainDemoController`
- Crée ChunkManager avec générateur custom
- Génère 9 chunks fixes au Start()
- Process toutes les queues immédiatement (generation + meshing)
- Affiche statistiques en temps réel

### Considérations de performance:

**Génération initiale**:
- 9 chunks x 262,144 voxels = 2,359,296 voxels total
- Génération immédiate au Start() (pas d'async)
- Traitement queue complet avant premier frame

**Meshing**:
- Greedy Meshing activé par défaut (réduction polygones)
- 9 meshes générés une seule fois
- Pas de remeshing dynamique (terrain statique)

**Mémoire**:
- ~20-25 MB pour 9 chunks (voxel data + meshes)
- Pas de cache LRU nécessaire (chunks fixes)

**Optimisations appliquées**:
- NativeArray pour voxel data (performance)
- Job System pour génération parallèle
- Greedy Meshing pour réduire draw calls
- Chunk batching par ChunkManager

### Différences avec ProceduralTerrainStreamer:

| Feature | Flat Checkerboard | Procedural Streamer |
|---------|-------------------|---------------------|
| Chunks | 9 fixes | Dynamiques (streaming) |
| Terrain | Plat, damier | Procédural 3D noise |
| Gravité | Non | Oui |
| Streaming | Non | Oui (LRU cache) |
| Complexité | Très simple | Complexe |

### Code de référence:

**Générateur damier (pseudocode)**:
```csharp
VoxelType CalculateCheckerboardPattern(int localX, int localZ, int chunkX, int chunkZ)
{
    int worldX = chunkX * 32 + localX;
    int worldZ = chunkZ * 32 + localZ;

    int tileSize = 8; // Increased for better visibility
    bool isEvenTileX = ((worldX / tileSize) % 2) == 0;
    bool isEvenTileZ = ((worldZ / tileSize) % 2) == 0;

    // XOR pour pattern damier
    return (isEvenTileX == isEvenTileZ) ? VoxelType.Grass : VoxelType.Dirt;
}
```

**Boucle génération 9 chunks**:
```csharp
for (int x = -1; x <= 1; x++)
{
    for (int z = -1; z <= 1; z++)
    {
        ChunkCoord coord = new ChunkCoord(x, 0, z);
        chunkManager.LoadChunk(coord);
    }
}
```

## Auteur

Généré avec Claude Code (Anthropic) - Architecture voxel Unity
Date: 2025-11-22
Version: 1.0.0
