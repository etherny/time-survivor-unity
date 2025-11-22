# Instructions de Configuration Post-Import Unity

## Important : Configuration Manuelle Requise

La scène `DemoScene.unity` a été créée avec des références de scripts qui doivent être reconfigurées après l'import dans Unity. Voici les étapes à suivre :

## Étapes de Configuration

### 1. Ouvrir Unity et Laisser Importer

1. Ouvrez le projet dans Unity Editor
2. Attendez que Unity importe tous les nouveaux assets (cela génère les .meta files)
3. Vérifiez qu'il n'y a pas d'erreurs de compilation dans la Console

### 2. Configurer le GameObject "Player"

1. Ouvrez la scène `Assets/demos/demo-procedural-terrain-streamer/Scenes/DemoScene.unity`
2. Sélectionnez le GameObject "Player" dans la Hierarchy
3. Dans l'Inspector, ajoutez le composant `PlayerController` :
   - Add Component → Scripts → TimeSurvivor.Demos.ProceduralTerrainStreamer → PlayerController
4. Configurez les paramètres (si nécessaire, ils ont des valeurs par défaut) :
   - Move Speed: 10
   - Sprint Multiplier: 2
   - Gravity: 9.81

### 3. Configurer le GameObject "TerrainSystem"

1. Sélectionnez le GameObject "TerrainSystem" dans la Hierarchy
2. Dans l'Inspector, ajoutez le composant `ProceduralTerrainStreamer` :
   - Add Component → Scripts → TimeSurvivor.Voxel.Terrain → ProceduralTerrainStreamer
3. Configurez les paramètres :
   - **Streaming Target**: Drag le Transform du GameObject "Player" ici
   - **Load Radius**: 100
   - **Unload Radius**: 120
   - **Max Chunks Per Frame**: 1
   - **Enable Caching**: Coché (true)
   - **Max Cached Chunks**: 50

### 4. Configurer le GameObject "Configuration"

1. Sélectionnez le GameObject "Configuration" dans la Hierarchy
2. Dans l'Inspector, ajoutez le composant `DemoController` :
   - Add Component → Scripts → TimeSurvivor.Demos.ProceduralTerrainStreamer → DemoController
3. Configurez les références :
   - **Terrain Streamer**: Drag le composant ProceduralTerrainStreamer du GameObject "TerrainSystem"
   - **Player**: Drag le Transform du GameObject "Player"
   - **Stats Text**: Drag le composant TextMeshProUGUI du GameObject "StatsPanel" (dans Canvas)
   - **Instructions Text**: Drag le composant TextMeshProUGUI du GameObject "InstructionsPanel" (dans Canvas)
4. Configurez les paramètres :
   - **Show Gizmos**: Coché (true)
   - **Load Radius Color**: Vert (0, 1, 0, 0.3)
   - **Unload Radius Color**: Rouge (1, 0, 0, 0.3)
   - **FPS Update Interval**: 1

### 5. Configurer le Capsule Material

1. Sélectionnez le GameObject "Player → Capsule" dans la Hierarchy
2. Dans l'Inspector, trouvez le composant Mesh Renderer
3. Dans Materials, assignez `PlayerMaterial` :
   - Drag `Assets/demos/demo-procedural-terrain-streamer/Materials/PlayerMaterial.mat` dans le slot Material

### 6. Vérifier TextMeshPro

Si TextMeshPro n'est pas configuré :
1. Window → TextMeshPro → Import TMP Essential Resources
2. Cliquez sur "Import"

### 7. Sauvegarder la Scène

1. File → Save Scene (Ctrl+S)
2. La scène est maintenant prête à être testée

## Validation Post-Configuration

Avant de lancer la démo, vérifiez que :

- ✅ Le GameObject "Player" a le composant `PlayerController`
- ✅ Le GameObject "Player" a le composant `CharacterController` (ajouté automatiquement)
- ✅ Le GameObject "TerrainSystem" a le composant `ProceduralTerrainStreamer`
- ✅ Le GameObject "Configuration" a le composant `DemoController`
- ✅ Toutes les références sont assignées (pas de "None" dans l'Inspector)
- ✅ Le Capsule a le `PlayerMaterial` (rouge)
- ✅ Pas d'erreurs dans la Console Unity

## Lancer la Démonstration

1. Assurez-vous que la scène `DemoScene.unity` est ouverte
2. Appuyez sur Play
3. Utilisez WASD pour vous déplacer
4. Maintenez Shift pour sprinter
5. Appuyez sur G pour toggle les gizmos debug

Si tout fonctionne correctement, vous devriez voir :
- Le terrain voxel se générer autour du joueur
- Les statistiques s'afficher dans le coin supérieur gauche
- Les instructions dans le coin inférieur gauche
- Le joueur (capsule rouge) répondre aux commandes WASD

## Problèmes Courants

### "Missing Script" sur les GameObjects
- **Solution**: Les scripts n'ont pas été importés correctement. Fermez et rouvrez Unity, ou réassignez manuellement les scripts.

### "NullReferenceException" dans la Console
- **Solution**: Vérifiez que toutes les références sont assignées dans l'Inspector du DemoController et du ProceduralTerrainStreamer.

### Le terrain ne se génère pas
- **Solution**: Vérifiez que le `ProceduralTerrainStreamer` a bien le `Streaming Target` assigné au Transform du Player.

### L'UI n'apparaît pas
- **Solution**: Vérifiez que TextMeshPro est bien importé (Window → TextMeshPro → Import TMP Essential Resources).

---

**Une fois la configuration terminée, référez-vous au README.md principal pour les instructions d'utilisation détaillées.**
