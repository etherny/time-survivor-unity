# Démo: ProceduralTerrainStreamer - Player Follow Streaming

## Description

Cette démonstration illustre le système de streaming de terrain voxel procédural (`ProceduralTerrainStreamer`) du moteur voxel TimeSurvivor. Le système génère dynamiquement des chunks de terrain autour du joueur en fonction de sa position, offrant une expérience de monde infini avec une gestion optimisée de la mémoire.

**Fonctionnalités démontrées:**
- **Streaming automatique**: Chunks générés/supprimés selon la position du joueur
- **Hysteresis**: Zone tampon entre load/unload pour éviter le flickering
- **Cache LRU**: Réutilisation intelligente des chunks précédemment générés
- **Load budgeting**: Maximum de chunks générés par frame (smooth performance)
- **UI temps réel**: Statistiques détaillées (FPS, chunks, memory, position)
- **Debug gizmos**: Visualisation des zones de load (vert) et unload (rouge)

## Prérequis

- **Unity Version**: 6000.2.12f1+
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Packages requis**:
  - TextMeshPro (com.unity.textmeshpro)
  - Mathematics (com.unity.mathematics)
  - Burst (com.unity.burst)
  - Jobs (com.unity.jobs)
- **Configuration**: Le projet doit être configuré avec URP

## Installation

### Étape 1: Vérifier les dépendances

1. Ouvrez Unity Package Manager (Window > Package Manager)
2. Vérifiez que les packages suivants sont installés:
   - TextMeshPro
   - Mathematics
   - Burst
   - Jobs
3. Si un package est manquant, installez-le via Package Manager

### Étape 2: Vérifier la configuration URP

1. Allez dans Edit > Project Settings > Graphics
2. Vérifiez que le Scriptable Render Pipeline Settings utilise un asset URP
3. Si ce n'est pas le cas, créez un pipeline URP:
   - Assets > Create > Rendering > URP Asset (with Universal Renderer)
   - Assignez-le dans Project Settings > Graphics

### Étape 3: Importer les matériaux

Les matériaux URP sont fournis dans `Assets/demos/demo-procedural-terrain-streamer/Materials/`:
- `TerrainMaterial.mat`: Matériau vert prairie pour le terrain voxel
- `PlayerMaterial.mat`: Matériau rouge vif pour le joueur

Ces matériaux sont préconfigurés avec le shader URP/Lit.

## Utilisation

### Étape 1: Ouvrir la scène

1. Dans le Project panel, naviguez vers `Assets/demos/demo-procedural-terrain-streamer/Scenes/`
2. Double-cliquez sur `DemoScene.unity` pour ouvrir la scène

### Étape 2: Configuration de la scène

Avant de lancer la démo, vérifiez la configuration dans l'Inspector:

#### GameObject "TerrainSystem" (ProceduralTerrainStreamer)
- **Streaming Target**: Doit référencer le Transform du GameObject "Player"
- **Load Radius**: 100m (rayon de génération autour du joueur)
- **Unload Radius**: 120m (rayon de suppression, crée une hysteresis de 20m)
- **Max Chunks Per Frame**: 1 (limite pour éviter les freezes)
- **Enable Caching**: Coché (active le cache LRU pour réutiliser les chunks)

#### GameObject "Player" (PlayerController)
- **Move Speed**: 10 unités/sec (vitesse normale)
- **Sprint Multiplier**: 2x (vitesse lors du sprint avec Shift)
- **Gravity**: 9.81 m/s² (gravité appliquée au joueur)

#### GameObject "Configuration" (DemoController)
- **Terrain Streamer**: Doit référencer le ProceduralTerrainStreamer
- **Player**: Doit référencer le Transform du GameObject "Player"
- **Stats Text**: Doit référencer le TextMeshProUGUI du StatsPanel
- **Instructions Text**: Doit référencer le TextMeshProUGUI du InstructionsPanel
- **Show Gizmos**: Coché (affiche les zones debug en mode Scene/Game)

### Étape 3: Lancer la démonstration

1. Appuyez sur le bouton **Play** dans l'éditeur Unity
2. Vous devriez voir:
   - Le terrain voxel généré autour du joueur
   - Un capsule rouge représentant le joueur en position (0, 50, 0)
   - L'UI affichant les statistiques en temps réel
   - Les instructions de contrôle

#### Contrôles disponibles

- **W/A/S/D**: Déplacer le joueur (avant/gauche/arrière/droite)
- **Left Shift**: Maintenir pour sprinter (vitesse 2x)
- **G**: Toggle l'affichage des gizmos debug (zones load/unload)

#### Comportements attendus

1. **Déplacement normal**:
   - Le joueur se déplace à 10 unités/seconde
   - Les chunks se génèrent automatiquement dans un rayon de 100m
   - Les chunks hors du rayon de 120m sont désactivés (hysteresis)

2. **Sprint**:
   - Le joueur se déplace à 20 unités/seconde
   - Maximum 1 chunk généré par frame (pas de freeze)
   - Performance reste stable (~60 FPS)

3. **Retour arrière**:
   - Les chunks précédemment visités se rechargent instantanément depuis le cache LRU
   - Aucune régénération procédurale nécessaire (performance optimale)

4. **UI Stats**:
   - FPS: Moyenne mobile sur 1 seconde
   - Active Chunks: Nombre de chunks actuellement visibles
   - Cached Chunks: Nombre de chunks en cache (réutilisables)
   - Memory: Estimation de la mémoire utilisée (~2.5 MB par chunk)
   - Player Position: Position XYZ du joueur

5. **Debug Gizmos** (si activés avec G):
   - Sphère verte: Zone de load (rayon 100m)
   - Sphère rouge: Zone d'unload (rayon 120m)

## Validation

### Ce que vous devriez voir:

- ✅ **Streaming automatique**: En vous déplaçant avec WASD, de nouveaux chunks apparaissent devant vous et disparaissent derrière vous
- ✅ **Hysteresis fonctionnelle**: Les chunks ne "clignotent" pas en allant et venant sur la limite (zone tampon de 20m)
- ✅ **Cache LRU actif**: En revenant sur vos pas, les chunks se rechargent instantanément (visibles dans "Cached Chunks")
- ✅ **Performance stable**: Le FPS reste stable (~60 FPS) même en sprintant rapidement (max 1 chunk/frame)
- ✅ **UI informative**: Les statistiques se mettent à jour en temps réel et reflètent l'état du système

### Tests de validation manuels

1. **Test Streaming Forward**:
   - Maintenez W pour avancer
   - Vérifiez que de nouveaux chunks apparaissent devant vous
   - Vérifiez que "Active Chunks" augmente jusqu'à un plateau

2. **Test Streaming Backward**:
   - Avancez loin (W pendant 10 secondes)
   - Faites demi-tour et revenez (S pendant 10 secondes)
   - Vérifiez que "Cached Chunks" augmente
   - Les chunks derrière vous devraient réapparaître instantanément

3. **Test Hysteresis**:
   - Positionnez-vous à la limite de load (100m d'un chunk)
   - Avancez/reculez légèrement de part et d'autre
   - Le chunk NE DOIT PAS clignoter (grâce à l'hysteresis de 20m)

4. **Test Performance Sprint**:
   - Maintenez Shift + W pour sprinter rapidement
   - Vérifiez que le FPS reste stable (~60 FPS)
   - Vérifiez que "Max Chunks/Frame: 1" limite la génération

5. **Test Debug Gizmos**:
   - Appuyez sur G pour activer les gizmos
   - En mode Scene, vous devez voir les sphères vertes (load) et rouges (unload)
   - Déplacez-vous et vérifiez que les sphères suivent le joueur

### Problèmes connus

- **Terrain plat**: Le générateur actuel utilise SimplexNoise3D qui peut produire un terrain relativement plat. Ceci est normal pour cette démo focalisée sur le streaming.
- **Pas de collision terrain**: Le CharacterController n'a pas de collision avec le terrain voxel dans cette démo simplifiée. Le joueur flotte à Y=50.
- **Memory estimation**: L'estimation de mémoire est approximative (~2.5 MB/chunk) et ne reflète pas exactement l'utilisation réelle.
- **Gizmos performance**: L'affichage des gizmos (G) peut réduire légèrement le FPS en mode Scene avec beaucoup de chunks actifs.

## Notes techniques

### Architecture du système

Le `ProceduralTerrainStreamer` utilise:
1. **ChunkManager**: Gestion centralisée des chunks (activation/désactivation/cache)
2. **SimplexNoise3D**: Génération procédurale de terrain avec bruit Simplex
3. **LRUCache**: Cache LRU (Least Recently Used) pour réutiliser les chunks
4. **Job System**: Génération asynchrone du terrain (ProceduralTerrainGenerationJob)
5. **GreedyMeshing**: Algorithme de meshing optimisé pour réduire les triangles

### Considérations de performance

- **Max Chunks Per Frame**: Limiter à 1 garantit un frametime stable (≤16ms pour 60 FPS)
- **Hysteresis**: La zone tampon de 20m évite le thrashing (load/unload répétés)
- **Cache LRU**: Réutiliser les chunks réduit drastiquement le coût CPU/GPU de régénération
- **Burst Compilation**: Les jobs utilisent Burst pour performances natives (SIMD)

### Paramètres recommandés

Pour des performances optimales:
- **Load Radius**: 80-120m (selon puissance CPU/GPU)
- **Unload Radius**: Load Radius + 20m minimum (hysteresis)
- **Max Chunks Per Frame**: 1-2 (plus haut = risque de stuttering)
- **Max Cached Chunks**: 50-100 (selon mémoire disponible)

Pour tester des scénarios extrêmes:
- **Load Radius**: 200m (beaucoup de chunks, teste la scalabilité)
- **Max Chunks Per Frame**: 5-10 (teste le load budgeting)
- **Sprint Multiplier**: 5x (teste le streaming ultra-rapide)

---

**Démo créée pour l'issue #6: Implement ProceduralTerrainStreamer (Player Follow Streaming)**

Pour toute question ou problème, référez-vous à la documentation du voxel engine dans `Assets/lib/voxel-terrain/Documentation~/`.
