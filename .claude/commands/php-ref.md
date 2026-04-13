# PHP Reference: $ARGUMENTS

Look up the PHP reference implementation for the feature or page described above.

A live running instance of the PHP site is available at **https://tft.nervaware.co.uk/stats/hlstats.php** — use it to verify visual layout and behaviour in addition to reading the source code. The .NET rewrite of this site also runs at **https://tft.nervaware.co.uk/stats** — use this to check what is already implemented and what differences exist.

Search in `OriginalPHP/pages/` for the relevant file(s). If the argument is vague (e.g. "player profile"), check multiple files — the PHP site splits pages across several includes (e.g. `playerinfo.php` + `playerinfo_general.php` + `playerinfo_weapons.php`).

For each relevant file found, summarise:
1. **What it renders** — what data is displayed, in what format
2. **SQL queries** — the key queries, which tables they hit, any JOINs or aggregations
3. **URL parameters** — what GET params it reads
4. **Edge cases** — anything non-obvious (hidden players, game filtering, rank logic, etc.)

This summary is the spec for the .NET implementation. Do not start writing any code — just provide the analysis.
