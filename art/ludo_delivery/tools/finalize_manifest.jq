def static_original:
  {
    "environment.arena_base": "art/ludo_delivery/01_environment/candidates/arena_base_ludo_original.webp",
    "character.hollow.master": "art/ludo_delivery/02_characters/hollow/candidates/hollow_master_ludo_reroll.webp",
    "character.burning.master": "art/ludo_delivery/02_characters/burning/candidates/burning_master_ludo_original.webp",
    "character.devourer.master": "art/ludo_delivery/02_characters/devourer/candidates/devourer_master_ludo_original.webp",
    "weapon.soul_cannon": "art/ludo_delivery/03_weapons/candidates/soul_cannon_ludo_original.webp",
    "pickup.lost_soul": "art/ludo_delivery/05_pickups/candidates/lost_soul_ludo_original.webp",
    "ending.life_flame": "art/ludo_delivery/06_ending/candidates/life_flame_ludo_original.webp"
  }[.assetId];

def static_normalized:
  {
    "environment.arena_base": "art/ludo_delivery/01_environment/arena_base_1800x1000.png",
    "character.hollow.master": "art/ludo_delivery/02_characters/hollow/hollow_master_128.png",
    "character.burning.master": "art/ludo_delivery/02_characters/burning/burning_master_128.png",
    "character.devourer.master": "art/ludo_delivery/02_characters/devourer/devourer_master_192.png",
    "weapon.soul_cannon": "art/ludo_delivery/03_weapons/soul_cannon_256.png",
    "pickup.lost_soul": "art/ludo_delivery/05_pickups/lost_soul_64.png",
    "ending.life_flame": "art/ludo_delivery/06_ending/life_flame_128.png"
  }[.assetId];

def static_destination:
  {
    "environment.arena_base": "src/TheLostSoulOfFire/Content/Textures/Environment/arena_base_1800x1000.png",
    "character.hollow.master": "src/TheLostSoulOfFire/Content/Textures/Enemies/Hollow/hollow_master_128.png",
    "character.burning.master": "src/TheLostSoulOfFire/Content/Textures/Enemies/Burning/burning_master_128.png",
    "character.devourer.master": "src/TheLostSoulOfFire/Content/Textures/Enemies/Devourer/devourer_master_192.png",
    "weapon.soul_cannon": "src/TheLostSoulOfFire/Content/Textures/Weapons/soul_cannon_256.png",
    "pickup.lost_soul": "src/TheLostSoulOfFire/Content/Textures/Pickups/lost_soul_64.png",
    "ending.life_flame": "src/TheLostSoulOfFire/Content/Textures/Ending/life_flame_128.png"
  }[.assetId];

def vfx_filename: (.path | split("/") | last);
def vfx_stem: (vfx_filename | sub("\\.png$"; ""));
def vfx_original:
  if .assetId == "vfx.scythe_cleave" then
    "art/ludo_delivery/04_vfx/candidates/fx_scythe_cleave_sheet_reroll.webp"
  elif .assetId == "vfx.core_hit" then
    "art/ludo_delivery/04_vfx/candidates/fx_core_hit_sheet_reroll.webp"
  elif .assetId == "vfx.soul_release" then
    "art/ludo_delivery/04_vfx/candidates/fx_soul_release_base_reroll.webp"
  else
    "art/ludo_delivery/04_vfx/candidates/\(vfx_stem)_sheet.webp"
  end;

def vfx_dimensions:
  if (.assetId == "vfx.burning_detonation" or .assetId == "vfx.resonance_activate") then "1024x1024 sheet; 256x256 frames"
  elif .assetId == "vfx.soul_release" then "512x512 sheet; 128x128 frames"
  elif (.assetId == "vfx.scythe_slash_01" or .assetId == "vfx.scythe_slash_02" or .assetId == "vfx.scythe_cleave" or .assetId == "vfx.cannon_muzzle_full") then "768x768 sheet; 256x256 frames"
  else "384x384 sheet; 128x128 frames"
  end;

.updatedAt = "2026-08-29T22:20:00+02:00"
| .ludo.status = "complete_required_set"
| .ludo.generationCalls = 162
| .ludo.creditsConsumed = 469.5
| .ludo.creditsConsumedIsEstimate = true
| .ludo.startingBalance = null
| .ludo.endingBalance = null
| .ludo.balanceNote = "No balance endpoint is exposed. 469.5 is calculated from MCP-advertised per-call pricing, not a balance reading. Optional polish was not generated."
| .staticAssets |= map(
    .generationStatus = "complete"
    | .selectionDecision = "accepted"
    | .originalDownloadedFile = static_original
    | .normalizedIntegrationFile = static_normalized
    | .integrationDestination = static_destination
  )
| .animationPacks |= map(
    .generationStatus = "complete"
    | .selectionDecision = "accepted_all_8_directions"
    | .originalDownloadedFile = "art/ludo_delivery/02_characters/\(.character)/candidates/animations/\(.action)/{direction}.webp"
    | .normalizedIntegrationFile = "art/ludo_delivery/02_characters/\(.character)/animations/\(.action)/{direction}.png"
    | .dimensions = (if .character == "devourer" then (if .action == "slam" then "768x768 sheet; 192x192 frames" else "576x576 sheet; 192x192 frames" end) else "384x384 sheet; 128x128 frames" end)
    | .frameLayout = (if .assetId == "anim.devourer.slam" then "16 frames; 4 columns x 4 rows; row-major" else "9 frames; 3 columns x 3 rows; row-major" end)
    | .integrationDestination = (if .character == "player" then "src/TheLostSoulOfFire/Content/Textures/Player/Animations/\(.action)/{direction}.png" else "src/TheLostSoulOfFire/Content/Textures/Enemies/\(.character | ascii_upcase[0:1])\(.character[1:])/Animations/\(.action)/{direction}.png" end)
  )
| .vfxAssets |= map(
    .ludoTool = "createImage:sprite-vfx+animateSprite:forge-pixel"
    |
    .generationStatus = "complete"
    | .selectionDecision = (if .assetId == "vfx.soul_release" then "accepted_after_deterministic_transparency_repair" elif (.assetId == "vfx.scythe_cleave" or .assetId == "vfx.core_hit") then "accepted_quality_reroll" else "accepted" end)
    | .originalDownloadedFile = vfx_original
    | .normalizedIntegrationFile = .path
    | .dimensions = vfx_dimensions
    | .frameLayout = (if (.assetId == "vfx.burning_detonation" or .assetId == "vfx.soul_release" or .assetId == "vfx.resonance_activate") then "16 frames; 4 columns x 4 rows; row-major" else "9 frames; 3 columns x 3 rows; row-major" end)
    | .integrationDestination = "src/TheLostSoulOfFire/Content/Textures/Effects/\(vfx_filename)"
    | .reviewNotes = (if .assetId == "vfx.soul_release" then "Ludo animation reroll retained a baked checker field; rebuilt from the accepted transparent reroll master with deterministic nearest-neighbor rise/fade. Rejected sheets preserved." elif .assetId == "vfx.core_hit" then "Initial checker-field sheet rejected; one quality reroll accepted with clean alpha." elif .assetId == "vfx.scythe_cleave" then "Initial explosion-like sheet rejected; one crescent-only quality reroll accepted." else .reviewNotes end)
  )
| .counts.generatedStaticAssets = 7
| .counts.generatedAnimationSheets = 96
| .counts.generatedVfxSheets = 12
| .integration.status = "complete"
| .integration.contentPipelineUpdated = true
| .integration.runtimeUpdated = true
| .integration.buildAfterIntegration = "passed_0_warnings_0_errors"
| .integration.testsAfterIntegration = "no_test_project_found"
| .integration.productionPngCount = 116
| .baselineVerification.gameplayLaunch = "passed_desktopgl_content_loaded_first_wave_captured"
| .baselineVerification.reviewArtifacts = [
    "art/ludo_delivery/review/01_locked_references.png",
    "art/ludo_delivery/review/02_static_cast_and_weapons.png",
    "art/ludo_delivery/review/03_arena_and_gameplay.png",
    "art/ludo_delivery/review/04_character_animations.png",
    "art/ludo_delivery/review/05_vfx.png",
    "art/ludo_delivery/review/06_final_ingame.png"
  ]
