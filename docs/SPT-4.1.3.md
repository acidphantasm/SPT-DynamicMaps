# Add Icebreaker decks and PNG map loading for SPT 4.1.3

Include the actual Icebreaker map data used in the tested SPT 4.1.3 setup, with all fourteen deck layers and the latest applied calibration. This contribution also preserves the local corpse-marker UI changes from the working DynamicMaps source.

## Icebreaker map and renderer

- Register both Suburbs and suburbs. Fourteen decks cover levels -3 through 10, default level 1, rotation 180 degrees.
- Include the exact deployed PNGs and Icebreaker.jsonc. Every layer now uses ImageBounds min (-19.69, -90.44), max (18.31, 93.56). Overall Bounds and GameBounds retain their prior values. This translates the artwork right/up at the current rotation without altering its scale.
- Add direct PNG decoding to the existing cached sprite loader. SVG maps retain their tessellation path; raster decoding does not parse SVG. Texture data becomes non-readable after upload. The installed test used a separate Harmony PNG adapter; this PR integrates equivalent loading directly into DynamicMaps, so that adapter is not needed with this build.
- Include tools/icebreaker generation/cropping scripts. Source artwork is supplied explicitly to the crop tool; no machine-specific input path is required.

## Attribution and license

Icebreaker v1.5 artwork is by RE3MR: https://reemr.se/Icebreaker/ . The fourteen crops and derived map package retain **CC BY-NC-SA 4.0**, https://creativecommons.org/licenses/by-nc-sa/4.0/ . This asset license is distinct from the repository's code license; attribution and noncommercial/share-alike terms must accompany distributions. CREDITS_AND_LICENSE.txt travels with the map resources. No third-party Icebreaker adapter DLL was used.

## Other applied DynamicMaps changes

The local source also adds a corpse loot-screen hide/show marker button, tracks searched bodies per raid, updates map-screen/provider helpers and texture handling, and adds UI lifecycle diagnostics. These are listed explicitly because they are additional to Icebreaker map support. Existing diagnostics remain; this PR does not claim to solve periodic stutters.

## Validation and calibration limits

The tester confirmed map display and player tracking in raid using the PNG adapter. The latest uniform translation was installed afterward in both test installations, with matching SHA256, but **has not yet been confirmed by another in-game calibration test**. Exact alignment at the bow, stern and helipad remains a manual check. Deck-height boundaries were inferred from spawn heights and the approximate deck spacing; stairs/double-height rooms can need additional GameBounds.

The cursor reading was (-11.80, 24.25), player (-11.11, 20.42, 23.81). Shift direction followed the tester's red-circle/arrow screenshots and explicit right/up request. A single point does not establish scale or rotation accuracy; the source images are schematic. The integrated native PNG path is build-checked, not separately raid-tested. Approximately 80 MiB of decoded textures can be cached; no frame-time profiling was performed.

Build with dotnet build Plugin/DynamicMaps.csproj -c Release -p:TarkovDir=<SPT-root-with-trailing-separator>. DeployToGame=true explicitly enables installation. Remove the separate DynamicMapsIcebreaker adapter when evaluating native PNG support.

## Compatibility and evidence

Target: **SPT 4.1.3**, EFT **0.16.9.5.40743**. This is a source contribution for that environment, not a claim of compatibility with future SPT releases or Fika.

The tester successfully loaded Icebreaker, entered a raid and extracted. In subsequent feedback they confirmed the blowtorch, extraction and doors work, and Black Division bots appeared to behave normally after the SAIN fix. These are user-reported functional observations, not automated coverage of every encounter or performance benchmarks. Ten continuous hours, all quests and multiplayer have not been tested.

The migration used installed SPT 4.1.3 assemblies and these guides:
- https://wiki.sp-tushonka.com/en/modding/SPT_41_Modding/Server_413_Changes
- https://wiki.sp-tushonka.com/en/modding/SPT_41_Modding/client/Class_Name_Mappings
- https://wiki.sp-tushonka.com/en/modding/SPT_41_Modding/server/Mod_Web_Pages


## Contribution build checks

The publication working copies were built in Release against the installed SPT 4.1.3 assemblies with deployment disabled. Client/server builds passed for Icebreaker, MoreBotsAPI, BlackDiv and ManimalCSGas; the Backport prepatcher and DynamicMaps client also passed. Existing compiler warnings remain in several ports. Fika was not built as part of this contribution gate. These checks validate compilation, not untested gameplay.
