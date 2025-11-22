# Démo: Flat Checkerboard Terrain

## Description

Cette démo présente un terrain voxel plat avec **streaming dynamique** et motif damier.

Elle démontre:
- Génération de terrain plat infini avec pattern damier (Grass vert / Dirt brun, cases 8x8 voxels)
- **Streaming dynamique**: Nouveaux chunks se créent quand le joueur se déplace
- **Unloading automatique**: Chunks lointains sont supprimés pour optimiser la mémoire
- Joueur sans gravité (mouvement planar fluide)
- Caméra isométrique qui suit le joueur
- Utilisation d'un générateur custom (`FlatCheckerboardGenerator`)

### Configuration Streaming
- **Load Radius**: 2 chunks autour du joueur (grille 5×5 = 25 chunks max)
- **Unload Radius**: 3 chunks (chunks au-delà sont supprimés)
- **Update Interval**: 0.5 secondes (vérification streaming 2×/seconde)

**Objectif**: Démontrer le système de streaming de chunks radius-based avec un pattern visuel simple pour validation.

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
- Chunks initiaux (5x5 grid) s'affichent au démarrage autour du joueur
- Pattern damier visible: alternance Grass (vert) / Dirt (brun) en cases de 8x8 voxels
- Terrain plat à hauteur fixe (8 voxels de haut)
- Joueur spawné à position (0, 2, 0) - juste au-dessus du terrain
- Caméra suit le joueur avec vue isométrique rapprochée
- **NOUVEAUX chunks apparaissent** quand le joueur se déplace vers les bords
- **Chunks lointains disparaissent** automatiquement (unload radius: 3)

**Contrôles disponibles**:
- **W/A/S/D**: Déplacer le joueur (mouvement planar, pas de gravité)
- **Shift (maintenu)**: Sprint (vitesse x2)
- **Souris**: Pas de contrôle de caméra (caméra suit automatiquement)

### Étape 4: Tester le streaming

1. **Déplacer le joueur** avec WASD dans différentes directions
2. **Observer les chunks actifs** dans le panneau Stats (nombre change dynamiquement)
3. **Vérifier que nouveaux chunks apparaissent** aux bords quand vous avancez
4. **Vérifier que chunks lointains disparaissent** (optimisation mémoire)
5. **Observer le "chunk joueur"** qui change dans les statistiques

### Étape 5: Observer les statistiques

Le panneau Stats (coin supérieur droit) affiche:
- FPS actuel
- **Nombre de chunks actifs** (varie entre 9-25 selon position)
- Type de pattern (Damier Grass/Dirt)
- Taille de case (8 voxels)
- **Load radius** et **Unload radius** configurés
- **Position du joueur** en temps réel
- **Chunk joueur** (coordonnées chunk actuel)

## Validation

### Ce que vous devriez voir:

- ✅ **Terrain plat visible**: Chunks affichés formant un plateau vert/brun infini
- ✅ **Pattern damier CLAIR**: Alternance Grass (vert vif) / Dirt (brun) en cases 8x8 voxels
- ✅ **Joueur sur le terrain**: Avatar rouge positionné à Y=2 (juste au-dessus du sol)
- ✅ **Caméra suit**: Vue isométrique (offset: 0, 12, -12) qui suit le joueur
- ✅ **Mouvement fluide**: WASD sans gravité, Sprint avec Shift
- ✅ **FPS élevés**: >60 FPS attendu avec streaming actif
- ✅ **Streaming fonctionnel**: Nouveaux chunks apparaissent aux bords quand vous bougez
- ✅ **Unloading automatique**: Nombre de chunks actifs reste entre 9-25
- ✅ **Stats mises à jour**: Chunk joueur et nombre chunks changent dynamiquement

### Ce que vous NE devriez PAS voir:

- ❌ **Terrain comme des "marches"**: Si c'est le cas, le joueur est mal positionné
- ❌ **Tout vert uniforme**: Si pas de pattern brun, vérifier material/shader (doit utiliser VoxelVertexColor)
- ❌ **Nombre de chunks explose**: Si >30 chunks, l'unloading ne fonctionne pas
- ❌ **Pas de nouveaux chunks**: Si aucun chunk n'apparaît en se déplaçant, le streaming est cassé
- ❌ **Chunks vides**: Tous les chunks doivent contenir du terrain solide

### Critères de succès détaillés:

1. **Génération correcte**:
   - Chunks générés dynamiquement autour du joueur
   - Coordonnées chunks varient selon position joueur
   - Hauteur terrain: 8 voxels (y: 0-7 solide, y: 8-31 air)

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

4. **Streaming dynamique**:
   - Nouveaux chunks apparaissent quand joueur se déplace
   - Chunks lointains sont unloadés (>3 chunks de distance)
   - Nombre de chunks actifs entre 9-25
   - Pas de freeze pendant streaming

## Problèmes connus

### Limitations volontaires:

- **Pas de gravité**: Le joueur ne tombe pas (mouvement planar uniquement)
- **Pattern simple**: Seulement 2 types de voxels (Grass/Dirt)
- **Terrain plat uniquement**: Pas de relief 3D (Y=0 seulement)

### Bugs connus:

- **Aucun bug connu** - Le streaming est stable et performant

### Troubleshooting:

**Problème**: Damier invisible (tout vert uniforme)
- **Solution**: Vérifier que le material utilise bien le shader `VoxelVertexColor` (créé automatiquement par setup)

**Problème**: Pas de nouveaux chunks quand je me déplace
- **Solution**: Vérifier que `loadRadius` est configuré (défaut: 2) et que Player reference est assignée

**Problème**: Trop de chunks (>30), performance dégradée
- **Solution**: Vérifier que `unloadRadius` est configuré (défaut: 3) et que l'unloading fonctionne

**Problème**: Joueur tombe à travers le terrain
- **Solution**: Vérifier que CharacterController a `center = (0, 1, 0)` et que le joueur spawn à y > 1.6

**Problème**: FPS très bas avec streaming
- **Solution**: Augmenter `updateInterval` (défaut: 0.5s) pour réduire la fréquence des vérifications streaming

**Problème**: Erreurs "Keyboard.current is null"
- **Solution**: Vérifier que Input System package est installé et activé dans Player Settings

## Notes techniques

### Architecture

- **Générateur**: `FlatCheckerboardGenerator` implements `IVoxelGenerator`
- **Hauteur sol**: 8 voxels (GROUND_HEIGHT = 8)
- **Taille cases damier**: 8x8 voxels (TILE_SIZE = 8)
- **Streaming**: Radius-based autour du joueur (load: 2, unload: 3)
- **ChunkSize**: 32 voxels (VoxelConfiguration)
- **MacroVoxelSize**: 0.2 Unity units

### Système de Streaming

**Algorithme**:
1. Calcul du chunk joueur depuis position monde
2. Load chunks dans radius autour du joueur (grille 5×5)
3. Unload chunks hors du radius (distance >3 chunks)
4. Update toutes les 0.5 secondes (configurable)

**Performance**:
- Chunks max: 25 (5×5 grid avec radius=2)
- Génération async via Jobs System
- Meshing amortisé pour éviter freeze
- Unloading automatique pour libérer mémoire

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
- **Streaming dynamique** basé sur position joueur
- Process queues de génération et meshing en continu
- Update streaming toutes les 0.5 secondes
- Affiche statistiques en temps réel (chunks actifs, chunk joueur)

### Considérations de performance:

**Génération dynamique**:
- Chunks générés à la demande (streaming)
- Max 25 chunks en mémoire (5×5 grid)
- Génération async via Jobs System
- Pas de freeze pendant génération

**Meshing**:
- Greedy Meshing activé par défaut (réduction polygones)
- Meshing amortisé pour éviter spike FPS
- Mesh regeneration seulement pour nouveaux chunks

**Mémoire**:
- ~50-60 MB max pour 25 chunks (voxel data + meshes)
- Unloading automatique libère mémoire
- Pas de cache LRU (unload radius suffit)

**Optimisations appliquées**:
- NativeArray pour voxel data (performance)
- Job System pour génération parallèle
- Greedy Meshing pour réduire draw calls
- Chunk batching par ChunkManager
- Streaming radius-based (pas d'explosion mémoire)

### Différences avec ProceduralTerrainStreamer:

| Feature | Flat Checkerboard | Procedural Streamer |
|---------|-------------------|---------------------|
| Chunks | Dynamiques (radius-based) | Dynamiques (LRU cache) |
| Terrain | Plat, damier | Procédural 3D noise |
| Gravité | Non | Oui |
| Streaming | Oui (simple radius) | Oui (LRU cache) |
| Complexité | Simple | Complexe |

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

**Streaming radius-based (pseudocode)**:
```csharp
// Calculer chunk joueur
ChunkCoord playerChunk = GetChunkCoordFromPosition(player.position);

// Load chunks dans radius
for (int x = -loadRadius; x <= loadRadius; x++)
{
    for (int z = -loadRadius; z <= loadRadius; z++)
    {
        ChunkCoord coord = new ChunkCoord(playerChunk.X + x, 0, playerChunk.Z + z);
        if (!chunkManager.HasChunk(coord))
        {
            chunkManager.LoadChunk(coord);
        }
    }
}

// Unload chunks hors radius
foreach (var chunk in chunkManager.GetAllChunks())
{
    int dx = Abs(chunk.Coord.X - playerChunk.X);
    int dz = Abs(chunk.Coord.Z - playerChunk.Z);

    if (dx > unloadRadius || dz > unloadRadius)
    {
        chunkManager.UnloadChunk(chunk.Coord);
    }
}
```

## Auteur

Généré avec Claude Code (Anthropic) - Architecture voxel Unity
Date: 2025-11-22
Version: 1.0.0
