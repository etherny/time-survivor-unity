# Guide de Configuration UI - Demo Procedural Terrain

Ce guide détaille la création des éléments UI manquants dans la scène de démo.

## Éléments à Créer

Dans **Panel - Controls** (Left) :
- 4 Sliders (Seed, Frequency, Amplitude, OffsetY)
- 2 Buttons (Generate, Randomize)

Dans **Panel - Stats** (Right) :
- 4 Text elements (déjà créés par le script)

---

## Étape 1 : Créer les Sliders

### A. Slider - Seed

1. **Clic droit** sur `Panel - Controls` dans la Hierarchy
2. **UI > Slider** → Renommer en `Slider - Seed`
3. **Configurer le Slider** :
   - **Rect Transform** :
     - Anchor Presets : Stretch (horizontal)
     - Pos Y : -40
     - Height : 30
     - Left : 10, Right : 10
   - **Slider Component** :
     - Min Value : `0`
     - Max Value : `999999`
     - Whole Numbers : ✅ (coché)
     - Value : `12345`

4. **Ajouter un Label** :
   - Clic droit sur `Slider - Seed` > **UI > Text - TextMeshPro**
   - Renommer en `Label - Seed`
   - **Rect Transform** :
     - Anchor : Top-Left
     - Pos X : 0, Pos Y : 20
     - Width : 200, Height : 20
   - **TextMeshPro - Text** :
     - Text : `Seed: 12345`
     - Font Size : 14
     - Color : White
     - Alignment : Left-Middle

### B. Slider - Frequency

1. **Clic droit** sur `Panel - Controls` > **UI > Slider**
2. Renommer en `Slider - Frequency`
3. **Configurer** :
   - **Rect Transform** :
     - Pos Y : -90
     - Height : 30
     - Left : 10, Right : 10
   - **Slider Component** :
     - Min Value : `0.01`
     - Max Value : `0.2`
     - Whole Numbers : ❌ (décoché)
     - Value : `0.05`

4. **Label** :
   - Clic droit sur `Slider - Frequency` > **UI > Text - TextMeshPro**
   - Renommer en `Label - Frequency`
   - Text : `Frequency: 0.05`
   - (mêmes settings Rect Transform que Label - Seed)

### C. Slider - Amplitude

1. **Clic droit** sur `Panel - Controls` > **UI > Slider**
2. Renommer en `Slider - Amplitude`
3. **Configurer** :
   - **Rect Transform** :
     - Pos Y : -140
     - Height : 30
   - **Slider Component** :
     - Min Value : `5`
     - Max Value : `50`
     - Whole Numbers : ✅
     - Value : `20`

4. **Label** :
   - Text : `Amplitude: 20`

### D. Slider - OffsetY

1. **Clic droit** sur `Panel - Controls` > **UI > Slider**
2. Renommer en `Slider - OffsetY`
3. **Configurer** :
   - **Rect Transform** :
     - Pos Y : -190
     - Height : 30
   - **Slider Component** :
     - Min Value : `0`
     - Max Value : `64`
     - Whole Numbers : ✅
     - Value : `32`

4. **Label** :
   - Text : `Offset Y: 32`

---

## Étape 2 : Créer les Buttons

### A. Button - Generate

1. **Clic droit** sur `Panel - Controls` > **UI > Button - TextMeshPro**
2. Renommer en `Button - Generate`
3. **Configurer** :
   - **Rect Transform** :
     - Anchor : Stretch (horizontal)
     - Pos Y : -250
     - Height : 40
     - Left : 10, Right : 10
   - **Button Component** :
     - Transition : Color Tint
     - Normal Color : `#2196F3` (bleu)
     - Highlighted Color : `#42A5F5`
     - Pressed Color : `#1976D2`

4. **Modifier le Text enfant** :
   - Sélectionner `Text (TMP)` sous `Button - Generate`
   - **TextMeshPro - Text** :
     - Text : `Generate Chunk`
     - Font Size : 18
     - Color : White
     - Alignment : Center-Middle
     - Font Style : Bold

### B. Button - Randomize

1. **Clic droit** sur `Panel - Controls` > **UI > Button - TextMeshPro**
2. Renommer en `Button - Randomize`
3. **Configurer** :
   - **Rect Transform** :
     - Pos Y : -310
     - Height : 40
     - Left : 10, Right : 10
   - **Button Component** :
     - Normal Color : `#4CAF50` (vert)
     - Highlighted Color : `#66BB6A`
     - Pressed Color : `#388E3C`

4. **Text** :
   - Text : `Randomize Seed`
   - Font Size : 18
   - Font Style : Bold

---

## Étape 3 : Assigner les Références dans DemoController

1. **Sélectionner** `Demo Controller` dans la Hierarchy

2. **Dans l'Inspector**, section **UI Controls** :
   - **Seed Slider** : Glisser `Slider - Seed`
   - **Frequency Slider** : Glisser `Slider - Frequency`
   - **Amplitude Slider** : Glisser `Slider - Amplitude`
   - **Offset Y Slider** : Glisser `Slider - OffsetY`
   - **Generate Button** : Glisser `Button - Generate`
   - **Randomize Button** : Glisser `Button - Randomize`

3. **Section UI Stats** (normalement déjà assignés par le script) :
   - **Generation Time Text** : `Text - Generation Time`
   - **Voxel Count Text** : `Text - Voxel Count`
   - **Distribution Text** : `Text - Distribution`
   - **FPS Text** : `Text - FPS`

4. **Section References** :
   - **Terrain Container** : Glisser `Terrain Container`
   - **Voxel Material** : Glisser `VoxelTerrain.mat` (créé par menu)

5. **Sauvegarder la scène** : `Ctrl+S`

---

## Étape 4 : Tester

1. **Press Play** (`Ctrl+P`)
2. **Vérifier** :
   - ✅ Un terrain apparaît au centre
   - ✅ Les sliders modifient les valeurs
   - ✅ Le bouton "Generate Chunk" régénère le terrain
   - ✅ Le bouton "Randomize Seed" change le seed et régénère
   - ✅ Les stats s'affichent (temps, FPS, distribution)
   - ✅ La caméra orbit avec clic gauche + molette

---

## Résultat Attendu

**Panel - Controls (Left)** devrait ressembler à :
```
┌─────────────────────────┐
│ Procedural Terrain Demo │
├─────────────────────────┤
│ Seed: 12345             │
│ ▬▬▬▬▬▬▬▬▬▬▬▬▬▬○        │ (Slider)
├─────────────────────────┤
│ Frequency: 0.05         │
│ ▬▬○▬▬▬▬▬▬▬▬▬▬▬▬        │ (Slider)
├─────────────────────────┤
│ Amplitude: 20           │
│ ▬▬▬▬▬○▬▬▬▬▬▬▬▬▬        │ (Slider)
├─────────────────────────┤
│ Offset Y: 32            │
│ ▬▬▬▬▬▬▬○▬▬▬▬▬▬▬        │ (Slider)
├─────────────────────────┤
│   [ Generate Chunk ]    │ (Button bleu)
│   [ Randomize Seed ]    │ (Button vert)
└─────────────────────────┘
```

---

## Troubleshooting

### Les sliders ne font rien
- Vérifier que les références sont bien assignées dans `Demo Controller`
- Vérifier que `DemoController.cs` est attaché au GameObject `Demo Controller`

### Les boutons ne fonctionnent pas
- Les listeners sont ajoutés automatiquement dans `Start()`
- Vérifier la console Unity pour d'éventuelles erreurs

### Le terrain n'apparaît pas
- Vérifier que `VoxelTerrain.mat` est assigné dans `Demo Controller > Voxel Material`
- Vérifier que la caméra est bien positionnée (orbiter pour voir)

### Erreur de références manquantes
- Exécuter `ValidateReferences()` s'exécute dans `Start()`
- Regarder la console Unity pour les messages d'erreur spécifiques

---

## Temps Estimé

- Création des 4 sliders : **3-4 minutes**
- Création des 2 buttons : **1-2 minutes**
- Assignation des références : **1 minute**
- **TOTAL : 5-7 minutes**

---

## Alternative Rapide : Dupliquer un Slider

Pour gagner du temps :

1. **Créer le premier slider** (Seed) avec son label
2. **Dupliquer** (`Ctrl+D`) 3 fois pour créer les autres
3. **Ajuster** les positions Y (-40, -90, -140, -190)
4. **Modifier** les valeurs Min/Max et les labels
5. **Créer les buttons** (2 minutes)
6. **Assigner** toutes les références (1 minute)

**Total optimisé : 3-4 minutes**
