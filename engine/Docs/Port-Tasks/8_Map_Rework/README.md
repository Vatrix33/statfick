# Task 8's approved continent: layout draft 6

The layout of the continent the user approved on 2026-11-07 (`../../Engine-Plan.md`, "Decisions for task 8, Map
Rework", W26; the road to it W17–W25). Kept as the reference for the port's visual redesign of the map (W27), and for
building the layout into the old game.

- `continent-draft-6.png`: the picture the user approved. Region colours only tell regions apart; the real map paints
  each region's own terrain, blending softly where two regions meet.
- `draft6.py`: makes that picture (`python3 draft6.py out.png`; numpy and Pillow; a few seconds). It reproduces
  `continent-draft-6.png` exactly.
- `draft3.py`: draft 3, kept because draft 6 takes two of its places as they were (W25: the Deep Desert's island and the
  water between Duskpine and the Crimson Divide); `draft6.py` runs it for that.
- `regions.json`: the 17 regions' data the drafts read (names, level ranges, roads, settlements, sites, exits, lake,
  own sea), exported from `old-game/scripts/world-region-data.js` before the layout moved anything.

## Units

Continent units (C): a region's own map is 1600 × 1000 units, placed on the continent enlarged 1.4× (W3), so it covers
2240 × 1400 C; C = origin + local × 1.4. 40/3 C make a kilometre (a region about 168 × 105 km). The picture's grid is
16 C a pixel, x from −7200 and y from −6100 (north up).

## Where everything is (each region's top-left corner, local (0, 0), in C)

| Region | Origin | Levels | Change from draft 2 (W6) |
|---|---|---|---|
| Stoneveil Summit | (−406, −5100) | 85–95 | joined to the High Stoneveil as one mountain (W22) |
| High Stoneveil | (−406, −3400) | 45–60 | |
| Stoneveil Foothills | (0, −1700) | 10–20 | |
| Sunward Coast | (2484, −1700) | 15–30 | joined to the mainland by the rocky highlands (W22) |
| Cinder Isle | (7900, 490) | 25–40 | about 250 km offshore (W21), level with the Verdant Reach (W22); the boat keeps 12.6 h |
| The Alder Vale | (0, 0) | 1–15 | |
| The Verdant Reach | (−2484, 490) | 5–20 | |
| Stormwake Hills | (−1812, 2134) | 15–35 | grows west and south (W22) |
| Gloamfen Marsh | (−4296, 2134) | 35–55 | grows east and south (W22) |
| Verdigris Jungle | (−4338, 3778) | 45–65 | grows north and east (W22) |
| Mirrormere Basin | (−1770, 3778) | 20–40 | its lake the only inland water near it (W22) |
| Saffron Expanse | (−1770, 5422) | 35–50 | grows south to the Crimson Divide (W22) |
| Deep Desert | (714, 4330) | 45–55 | moved up beside Saffron (W19, W22); its island (W24, W25) |
| Crimson Divide | (−1770, 7066) | 40–55 | |
| Bloodstone Coast | (−1770, 8710) | 55–70 | |
| Ancient Battlefield | (−4254, 8710) | 65–75 | |
| Duskpine Forest | (−4254, 7066) | 60–75 | moved north of the Battlefield (W19) |
| **New: rocky highlands** | (2484, −56) | (15–30 proposed) | east of the Alder Vale, south of the Sunward Coast; the one rocky region (W20) |
| **New: steppe** | (672, 1644) | (20–35 proposed) | south of the Alder Vale, east of Stormwake |
| **New: west-coast delta** | (−4296, 5422) | (50–65 proposed) | below the Jungle, above Duskpine, west of Saffron |

The three new regions' names, identities, level bands and contents are **not decided**: they are left empty in the old
game and made entirely in the port (W27). The labels and level bands in the picture are only the draft's proposals.

## Roads moved (every connection kept, W19)

Positions are in each region's own units (1600 × 1000).
- The Ancient Battlefield's road to Duskpine leaves north: its last piece turns from (350, 500) to a north exit at
  (500, 20); Duskpine's road runs from Ravenwatch (815, 505) south to (500, 980).
- Saffron's road to the Deep Desert leaves its east side higher, from Dunecross (1050, 585) to (1580, 120); the Deep
  Desert's road runs from Lastlight (330, 270) to a west exit at (20, 900).
- Every other road and exit is as it was.

## The land's shape

- The coast is natural (W17), up to 30 km (400 C) in or out from the old straight edges (W18), as broken as draft 3's
  (W24): capes and bays only on the outer coast.
- Regions grow into the ground between them (W22): no sea or rock between neighbours, and nothing forces two regions
  apart (W20). Where they touch, the map's header shows the levels of the region you stand in (W23).
- Inland waters (W24, W25): a long water between the Alder Vale and the rocky highlands (centre about (2362, 760) C),
  a larger one between Duskpine and the Crimson Divide as in draft 3, and Mirrormere's own lake.
- The Deep Desert's island off its south-east coast as in draft 3 (W25).
- Cinder Isle keeps its own oval shape with a natural coast.
