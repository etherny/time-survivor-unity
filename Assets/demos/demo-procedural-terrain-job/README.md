# Démo: Procedural Terrain Generation Job

## Description

Cette démonstration illustre le système de génération procédurale de terrain voxel utilisant **ProceduralTerrainGenerationJob** (ADR-007) et **GreedyMeshingJob** (ADR-003). Elle permet de générer dynamiquement un chunk 64³ (262,144 voxels) en temps réel avec contrôle complet des paramètres de génération.

**Fonctionnalités démontrées** :
- Génération procédurale basée sur Simplex Noise 3D multi-octaves
- Meshing optimisé via Greedy Meshing Algorithm
- Contrôle en temps réel des paramètres (Seed, Frequency, Amplitude, Offset Y)
- Métriques de performance (temps de génération, FPS, distribution des voxels)
- Rendu 3D avec vertex colors et caméra orbit interactive

**Performance cible** : Génération complète (voxels + mesh) en <5ms pour un chunk 64³.

---

## Prérequis

### Logiciels
- **Unity Version**: 6000.2.12f1 (ou supérieure)
- **Render Pipeline**: Universal Render Pipeline (URP)

### Packages Unity requis
- **Burst Compiler** (com.unity.burst) - Pour l'optimisation des jobs
- **Mathematics** (com.unity.mathematics) - Pour les calculs vectoriels
- **TextMeshPro** (com.unity.textmeshpro) - Pour l'interface utilisateur
- **Collections** (com.unity.collections) - Pour les NativeArray/NativeList

Vérifiez dans `Window > Package Manager` que ces packages sont installés.

### Dépendances du Voxel Engine
Cette démo utilise les packages du moteur voxel :
- `TimeSurvivor.Voxel.Core` (VoxelType, ChunkCoord, VoxelMath)
- `TimeSurvivor.Voxel.Terrain` (ProceduralTerrainGenerationJob, SimplexNoise3D)
- `TimeSurvivor.Voxel.Rendering` (GreedyMeshingJob)

Assurez-vous que ces packages sont présents dans `Assets/lib/voxel-*/`.

---

## Installation

### Option A : Setup Automatique (Recommandé)

1. **Ouvrir le menu Unity**
   - Aller dans `Tools > Voxel Demos > Setup Procedural Terrain Demo Scene`
   - Attendre que Unity crée automatiquement la scène et la structure UI

2. **Créer le material VoxelTerrain**
   - Aller dans `Assets/demos/demo-procedural-terrain-job/Materials/`
   - Créer un nouveau material : Clic droit > Create > Material
   - Nommer le material `VoxelTerrain`
   - Configuration :
     - **Shader**: URP > Lit (ou Custom Vertex Color Shader)
     - **Surface Type**: Opaque
     - **Workflow Mode**: Metallic
     - **Base Map**: Blanc (#FFFFFF)
     - **Smoothness**: 0.2
     - **Important** : Si le shader URP/Lit ne supporte pas les vertex colors par défaut, créez un shader custom (voir section "Shader Custom" ci-dessous)

3. **Configurer le DemoController**
   - Ouvrir la scène `Assets/demos/demo-procedural-terrain-job/Scenes/DemoScene.unity`
   - Sélectionner l'objet `Demo Controller` dans la hiérarchie
   - Dans l'Inspector, assigner les références manquantes :
     - **Terrain Container**: Drag l'objet "Terrain Container" de la hiérarchie
     - **Voxel Material**: Drag le material `VoxelTerrain` créé à l'étape 2
     - **UI Controls**: Assigner manuellement les sliders et boutons créés dans le canvas (voir section "Configuration UI" ci-dessous)
     - **UI Stats**: Assigner les TextMeshPro pour les statistiques

4. **Configuration de l'UI** (Manuel - requis après setup automatique)
   - Le script de setup crée la structure de base, mais vous devez ajouter manuellement :
     - 4 **Sliders** (Seed, Frequency, Amplitude, OffsetY) dans le Panel - Controls
     - 2 **Buttons** (Generate, Randomize) dans le Panel - Controls
   - Configuration des sliders :
     - **Seed Slider**: Min Value = 0, Max Value = 999999, Whole Numbers = true
     - **Frequency Slider**: Min Value = 0.01, Max Value = 0.2, Whole Numbers = false
     - **Amplitude Slider**: Min Value = 5, Max Value = 50, Whole Numbers = false
     - **OffsetY Slider**: Min Value = 0, Max Value = 64, Whole Numbers = true
   - Assigner tous les éléments UI dans le `DemoController` Inspector

5. **Configuration de la caméra**
   - Sélectionner l'objet `Main Camera` dans la hiérarchie
   - Dans le composant `CameraOrbitController` :
     - **Target**: Assigner l'objet "Terrain Container"
     - **Distance**: 80
     - **Orbit Speed**: 0.2
     - **Zoom Speed**: 10

### Option B : Setup Manuel (Avancé)

Si vous préférez créer la scène manuellement, suivez ces étapes détaillées :

1. **Créer une nouvelle scène**
   - File > New Scene > Basic (Built-in)
   - Sauvegarder dans `Assets/demos/demo-procedural-terrain-job/Scenes/DemoScene.unity`

2. **Créer la hiérarchie**
   ```
   DemoScene
   ├── Directional Light (default)
   ├── Main Camera
   │   └── CameraOrbitController.cs
   ├── Terrain Container (empty GameObject)
   ├── Demo Controller
   │   └── DemoController.cs
   ├── UI Canvas (Screen Space - Overlay)
   │   ├── Panel - Controls (Left, 300x400px)
   │   │   ├── Text - Title
   │   │   ├── Slider - Seed (0-999999)
   │   │   ├── Slider - Frequency (0.01-0.2)
   │   │   ├── Slider - Amplitude (5-50)
   │   │   ├── Slider - OffsetY (0-64)
   │   │   ├── Button - Generate
   │   │   └── Button - Randomize
   │   └── Panel - Stats (Right, 350x300px)
   │       ├── Text - Title
   │       ├── Text - Generation Time
   │       ├── Text - Voxel Count
   │       ├── Text - Distribution
   │       └── Text - FPS
   └── Event System (default)
   ```

3. **Configurer la caméra**
   - Position: (32, 48, -48)
   - LookAt: (32, 32, 32)
   - Ajouter le script `CameraOrbitController.cs`
   - Assigner `Terrain Container` comme Target

4. **Configurer DemoController**
   - Créer un empty GameObject "Demo Controller"
   - Ajouter le script `DemoController.cs`
   - Assigner toutes les références (voir Option A, étape 3)

5. **Créer le material VoxelTerrain** (voir Option A, étape 2)

---

## Shader Custom (Optionnel - Si URP/Lit ne supporte pas vertex colors)

Si le shader URP/Lit par défaut n'affiche pas les vertex colors correctement, créez un shader custom :

**Fichier**: `Assets/demos/demo-procedural-terrain-job/Materials/VoxelTerrainShader.shader`

```hlsl
Shader "Custom/VoxelTerrainVertexColor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0, 1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample texture
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Apply vertex color
                half4 finalColor = texColor * input.color;

                // Simple lighting (diffuse)
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 normal = normalize(input.normalWS);
                float NdotL = saturate(dot(normal, lightDir));
                float3 lighting = mainLight.color * NdotL;

                // Ambient
                float3 ambient = half3(0.3, 0.3, 0.3);

                finalColor.rgb *= (lighting + ambient);

                // Apply fog
                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
```

Ensuite, créez un material `VoxelTerrain` et assignez ce shader custom.

---

## Utilisation

### Étape 1: Ouvrir la scène

1. Aller dans `Assets/demos/demo-procedural-terrain-job/Scenes/`
2. Double-cliquer sur `DemoScene.unity` pour l'ouvrir
3. Vérifier que tous les objets sont présents dans la hiérarchie (voir structure ci-dessus)

### Étape 2: Configuration de la scène

1. **Sélectionner `Demo Controller`** dans la hiérarchie
2. **Vérifier l'Inspector** :
   - ✅ Terrain Container est assigné
   - ✅ Voxel Material est assigné
   - ✅ Tous les sliders UI sont assignés
   - ✅ Tous les boutons UI sont assignés
   - ✅ Tous les TextMeshPro de stats sont assignés
3. Si des références manquent, les assigner manuellement depuis la hiérarchie

### Étape 3: Lancer la démonstration

1. **Appuyer sur Play** dans l'éditeur Unity
2. **Attendre la génération initiale** (1-5 secondes selon votre machine)
3. **Observer le terrain généré** dans la vue Game

**Comportements attendus** :
- Un chunk 64³ de terrain procédural apparaît au centre de la scène
- Les statistiques s'affichent dans le panel de droite :
  - Temps de génération (devrait être <5ms pour un chunk 64³)
  - Nombre de voxels solides
  - Distribution par type (Air, Grass, Dirt, Stone, Water)
  - FPS en temps réel
- Le terrain est composé de voxels colorés par type :
  - **Vert** : Grass (surface)
  - **Marron** : Dirt (sous-sol)
  - **Gris** : Stone (profondeur / montagnes)
  - **Bleu** : Water (cavernes souterraines)

### Étape 4: Interagir avec la démo

#### Contrôles de la caméra
- **Clic gauche + Drag** : Orbiter autour du terrain
- **Molette souris** : Zoomer/Dézoomer
- Objectif : Inspecter le terrain sous tous les angles pour valider la génération

#### Contrôles de génération (Panel gauche)
1. **Slider "Seed"** (0-999999)
   - Change le seed de génération
   - Même seed = même terrain (déterministe)
   - Tester différents seeds pour voir la variété

2. **Slider "Frequency"** (0.01-0.2)
   - Contrôle l'échelle des features du terrain
   - **Faible** (0.01-0.05) : Terrain très lisse, grandes collines
   - **Moyenne** (0.05-0.1) : Équilibre naturel
   - **Élevée** (0.1-0.2) : Terrain très détaillé, petites features

3. **Slider "Amplitude"** (5-50)
   - Contrôle l'influence du bruit
   - **Faible** (5-15) : Terrain plat, peu de variation
   - **Moyenne** (15-30) : Relief naturel
   - **Élevée** (30-50) : Terrain très montagneux

4. **Slider "Offset Y"** (0-64)
   - Décale le chunk verticalement
   - **0** : Chunk à l'origine (surface visible)
   - **32** : Chunk en hauteur (plus de pierre)
   - **-32** : Chunk souterrain (plus de cavernes)

5. **Bouton "Generate"**
   - Régénère le terrain avec les paramètres actuels
   - Observe le temps de génération dans les stats

6. **Bouton "Randomize"**
   - Génère un seed aléatoire et régénère
   - Parfait pour explorer différents terrains rapidement

#### Workflow de test recommandé
1. Laisser les valeurs par défaut, cliquer "Generate" → Valider la génération de base
2. Augmenter Frequency à 0.15 → Observer les détails fins
3. Augmenter Amplitude à 40 → Observer les montagnes
4. Cliquer "Randomize" 5-10 fois → Valider la variété
5. Modifier OffsetY à -32 → Observer les cavernes souterraines

---

## Validation

### Ce que vous devriez voir:

#### ✅ Génération réussie
- Un terrain voxel 64³ apparaît dans la scène
- Le terrain est composé de voxels de différentes couleurs (Grass, Dirt, Stone, Water)
- Le terrain a un aspect naturel (collines, relief, cavernes)

#### ✅ Performance acceptable
- **Temps de génération** : <5ms pour un chunk 64³
  - Si >10ms : Vérifier que Burst est activé (`Jobs > Burst > Enable Compilation`)
  - Si >50ms : Vérifier que le projet est en mode Release, pas Debug
- **FPS** : >60 FPS en mode Game (selon votre machine)
  - Si <30 FPS : Vérifier que le mesh n'a pas trop de vertices (>100k = problème potentiel)

#### ✅ Distribution correcte des voxels
- **Air** : ~40-60% (espaces vides, cavernes)
- **Grass** : ~5-15% (surface)
- **Dirt** : ~10-20% (sous-sol)
- **Stone** : ~15-25% (profondeur)
- **Water** : ~0-5% (cavernes)

Si la distribution est trop déséquilibrée (ex: 90% Stone, 10% Air), ajuster les paramètres :
- Réduire Amplitude pour plus d'air
- Augmenter Frequency pour plus de détails

#### ✅ Déterminisme
- Même seed + mêmes paramètres = terrain identique
- Test : Noter un seed (ex: 123456), modifier les paramètres, revenir au seed 123456 avec les mêmes paramètres → le terrain doit être identique

#### ✅ Pas d'erreurs console
- Aucun warning ou erreur dans la Console Unity
- Si erreurs NullReferenceException : Vérifier que toutes les références du DemoController sont assignées

---

## Problèmes connus

### 1. Le terrain n'apparaît pas
**Symptômes** : Scène vide après clic sur "Generate"

**Solutions** :
- Vérifier que `Terrain Container` est assigné dans `DemoController`
- Vérifier que `Voxel Material` est assigné et configuré correctement
- Vérifier la Console Unity pour des erreurs de NullReferenceException
- Vérifier que la caméra pointe vers le centre du terrain (32, 32, 32)

### 2. Le terrain est tout noir ou tout blanc
**Symptômes** : Terrain généré mais sans couleurs

**Solutions** :
- Vérifier que le material `VoxelTerrain` utilise un shader supportant les vertex colors
- Si utilisation de URP/Lit : Créer un shader custom (voir section "Shader Custom")
- Vérifier que le mesh a bien des vertex colors assignées (Debug dans `CreateMeshFromJobData`)

### 3. Performance très lente (<10 FPS)
**Symptômes** : Génération >100ms, FPS <10

**Solutions** :
- Activer Burst : `Jobs > Burst > Enable Compilation`
- Vérifier que le projet est en mode Release : `Edit > Preferences > Jobs > Leak Detection = Off`
- Réduire la taille du chunk : Passer de 64³ à 32³ dans `DemoController.chunkSize`
- Vérifier que le mesh n'a pas trop de vertices : Si >500k vertices, il y a un problème dans GreedyMeshing

### 4. Erreurs de compilation
**Symptômes** : Erreurs dans la Console lors de l'ouverture de la scène

**Solutions** :
- Vérifier que tous les packages requis sont installés (Burst, Mathematics, Collections)
- Vérifier que les namespaces `TimeSurvivor.Voxel.*` sont accessibles
- Reconstruire les assembly definitions : `Assets > Reimport All`

### 5. UI ne répond pas
**Symptômes** : Clic sur boutons ou sliders sans effet

**Solutions** :
- Vérifier que `EventSystem` est présent dans la scène
- Vérifier que tous les listeners sont assignés dans `DemoController.Start()`
- Vérifier que les références UI sont correctement assignées dans l'Inspector

### 6. Vertex colors ne s'affichent pas en URP
**Symptômes** : Terrain blanc ou uniformément coloré

**Solutions** :
- Le shader URP/Lit par défaut ne supporte pas toujours les vertex colors
- **Solution recommandée** : Créer un shader custom (voir section "Shader Custom" ci-dessus)
- Alternative : Utiliser le shader Built-in "Unlit/Color" (moins réaliste)

---

## Notes techniques

### Architecture de la démo

Cette démo suit l'architecture recommandée par les ADRs du voxel engine :

- **ADR-007** : ProceduralTerrainGenerationJob
  - Utilise SimplexNoise3D multi-octaves
  - Génère des VoxelType déterministes basés sur altitude et density
  - Performance : <0.3ms pour génération voxels 64³

- **ADR-003** : GreedyMeshingJob
  - Optimise le mesh en fusionnant les faces adjacentes identiques
  - Réduit le nombre de vertices de ~1.5M (naïve) à ~20-50k (greedy)
  - Génère automatiquement les vertex colors par VoxelType

### Optimisations appliquées

1. **Burst Compilation**
   - Tous les jobs utilisent `[BurstCompile]` pour une génération ultra-rapide
   - Gain de performance : 10-20x par rapport à du code C# non-Burst

2. **Job System**
   - Utilisation de `IJobParallelFor` pour ProceduralTerrainGenerationJob
   - Utilisation de `IJob` pour GreedyMeshingJob
   - Exécution multi-thread automatique

3. **NativeContainers**
   - Utilisation de `NativeArray` et `NativeList` pour éviter les allocations managed
   - Réduction de la pression GC (Garbage Collector)

4. **Greedy Meshing**
   - Fusion des quads adjacents réduit drastiquement le vertex count
   - Impact sur performance de rendu : 5-10x plus rapide qu'un mesh naïf

### Considérations de performance

**Génération complète (64³ chunk)** :
- ProceduralTerrainGenerationJob : ~0.2-0.5ms
- GreedyMeshingJob : ~1-3ms
- Création Unity Mesh : ~0.5-1ms
- **Total** : ~2-5ms

**Memory footprint** :
- VoxelData (64³) : 262,144 bytes (~256KB)
- Mesh vertices (moyenne) : ~30k vertices × 32 bytes = ~960KB
- **Total temporaire** : ~1.2MB (libéré après génération)

**Scalabilité** :
- Cette démo génère UN chunk statique
- Pour un monde infini, utiliser `ChunkManager` et `ProceduralTerrainStreamer` (voir `Assets/lib/voxel-terrain/`)

### Extension de la démo

**Idées pour aller plus loin** :
1. Ajouter un système de chunking infini (utiliser `ProceduralTerrainStreamer`)
2. Implémenter le LOD (Level of Detail) avec `AmortizedMeshingJob`
3. Ajouter la destruction de voxels (utiliser `DestructibleOverlayManager`)
4. Implémenter les collisions physiques (utiliser `VoxelRaycast` et `VoxelCollisionBaker`)
5. Ajouter des biomes (modifier `ProceduralTerrainGenerationJob` pour sélectionner les VoxelType selon une heatmap)

---

## Support et Documentation

### Documentation complète du Voxel Engine
- **ADR-003** : Greedy Meshing Algorithm - `Assets/lib/voxel-rendering/Documentation~/ADR-003-Greedy-Meshing.md`
- **ADR-007** : Procedural Terrain Generation - `Assets/lib/voxel-terrain/Documentation~/ADR-007-Procedural-Terrain-Generation.md`

### Fichiers de la démo
- **Scripts** : `Assets/demos/demo-procedural-terrain-job/Scripts/`
  - `DemoController.cs` - Orchestration de la génération et UI
  - `CameraOrbitController.cs` - Contrôles caméra
- **Scene** : `Assets/demos/demo-procedural-terrain-job/Scenes/DemoScene.unity`
- **Materials** : `Assets/demos/demo-procedural-terrain-job/Materials/VoxelTerrain.mat`

### Contact
Pour des questions ou bugs :
1. Vérifier les problèmes connus ci-dessus
2. Consulter la documentation des ADRs
3. Ouvrir une issue sur le dépôt Git du projet

---

**Auteur** : Claude Code (Unity C# Developer Agent)
**Date** : 2025-11-21
**Version** : 1.0
