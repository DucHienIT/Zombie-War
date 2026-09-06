# Weapon icons matched to in-game models

Created on 2026-09-06 with the built-in image_gen tool. The actual weapon prefabs were rendered in Unity to inspect their meshes, silhouettes and material colors. Reference renders are in Captures/WeaponReferences/.

Reference-image generations and alpha cleanup attempts returned baked checkerboard backgrounds. Final icons were generated from detailed descriptions of the inspected models, then visually compared with those references. The user requested continuing with ImageGen; no script-based image editing was used.

The four existing first-party PNGs are replaced in place, retaining GUID references from weapon configs, UIRoot and UI_Items atlas. Unity import: single Sprite, transparent alpha, mipmaps off, max size 256 px. Gameplay data and gun models are unchanged.

## Outputs and model sources

- AssaultRifle: Assets/_ZombieWar/Art/UI/Sprites/Icons/Item_Icon_Gun_Rifle.png; source Assets/_ZombieWar/Models/Weapons/assault1.fbx with Assets/_ZombieWar/Art/Materials/Weapons/assault1.mat.
- SMG: Assets/_ZombieWar/Art/UI/Sprites/Icons/Item_Icon_Gun_Smg.png; source Assets/_ZombieWar/Models/Weapons/smg1.fbx with Assets/_ZombieWar/Art/Materials/Weapons/smg1.mat.
- Shotgun: Assets/_ZombieWar/Art/UI/Sprites/Icons/Item_Icon_Gun_Shotgun.png; source Assets/_ZombieWar/Models/Weapons/shotgun1.fbx with Assets/_ZombieWar/Art/Materials/Weapons/shotgun1.mat.
- Sniper: Assets/_ZombieWar/Art/UI/Sprites/Icons/Item_Icon_Gun_Sniper.png; source Assets/_ZombieWar/Models/Weapons/sniper2.fbx with Assets/_ZombieWar/Art/Materials/Weapons/sniper2.mat.

## Final Assault Rifle prompt

Generate a single game inventory sprite on a transparent background with real alpha. An AK-pattern rifle with fixed brown wooden buttstock at lower left, wooden pistol grip, wooden upper and lower handguard, black steel receiver, curved black magazine, slim exposed barrel and gas tube, upright iron front sight at upper right. No attachments or scope. Polished stylized 3D render, near side view, diagonal 25 degree upward angle. Whole gun centered in square image with generous margins, entire muzzle and buttstock visible. Match a casual military zombie game. Restrained realistic brown wood and black steel. Isolated cutout, TRANSPARENT BACKGROUND, empty pixels around the gun. No text, no border, no surface, no shadow. Return PNG with transparency.

## Shared final prompt for SMG, Shotgun and Sniper

Generate a single game inventory sprite on a transparent background with real alpha. Polished stylized 3D render with simple clean hand-painted materials and soft beveled shading. Near side view, whole weapon angled diagonally upward about 25 degrees, buttstock lower left and muzzle upper right. Whole gun centered in square image with generous margins, entire muzzle and buttstock visible. Match a casual military zombie game. Preserve all the following specified gun features and proportions. Isolated cutout, TRANSPARENT BACKGROUND, empty pixels around the gun. No text, no border, no surface, no shadow, no effects. Return PNG with transparency. Weapon: 

### SMG subject

Compact MP5-pattern submachine gun, matte black steel and polymer, small dark red panels at the magazine well and stock attachment, straight retractable metal stock with narrow black buttplate, short barrel, hooded front iron sight, curved black magazine and black pistol grip. Preserve the reference silhouette and red panel placements; no wood, scope, suppressor, or foregrip.

Final SMG prompt (used to enforce the collapsed stock and real alpha):

Generate a single Unity mobile game weapon inventory sprite as a polished stylized 3D cutout. A compact MP5-pattern submachine gun matching this exact silhouette: matte black receiver and polymer fore-end; short barrel; hooded circular front iron sight; curved black magazine; black pistol grip; small dark red metal panels at the magazine well and rear receiver. The retractable stock is FULLY COLLAPSED: only a narrow vertical black buttplate directly touching the rear of the receiver, with no long rods extending behind it. Very compact overall length. Near side view, butt lower-left and muzzle upper-right at about 25 degrees. Entire weapon centered with 10% padding. Genuine transparent background with alpha=0 outside the weapon. No checkerboard, no white or gray background, no shadow, text, hands, scope, suppressor, foregrip, or wood. Return transparent PNG.

### Shotgun subject

Long pump-action shotgun, reddish brown fixed wooden shoulder stock and matching broad wooden pump fore-end, dark steel receiver, long barrel with parallel magazine tube underneath, traditional curved stock wrist, gray butt pad. Preserve this exact traditional hunting shotgun silhouette. No detachable box magazine, no separate pistol grip, no scope.

### Sniper subject

Long traditional bolt-action military rifle with orange-brown full-length wooden stock, dark steel bolt receiver, visible downward bolt handle, two dark barrel bands, dark buttplate and iron front sight. The source model has NO telescopic scope. Preserve its long narrow wooden fore-stock and slim barrel, traditional integrated grip and overall proportions. No scope, no bipod, no detachable magazine, no modern tactical furniture.
