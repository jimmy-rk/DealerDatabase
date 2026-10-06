DECISIONS

• Matching approach
- Normalize company display names by removing non-alphanumeric chars and uppercasing to produce a compact key for grouping (letters/digits only).
- Replace common abbreviations (e.g. "LTD" → "LIMITED") before normalization so "Foo Ltd" and "Foo Limited" group together.
- Group source records by that normalized key and treat each group as one logical dealer for merging.

• When two records are the same dealer
- Records with the same normalized name key are considered the same dealer (deterministic grouping).

• Conflict resolution (source priority and rules)
- Address fields: prefer FCA Register values first, then Companies House, then ICO, then MarketCheck/crawl. FCA is treated as authoritative for address data.
- Company identifiers: prefer Companies House company number when present; prefer deterministic MarketCheck key (cols[0]) if available for MarketCheck rows.
- Display name: pick the longest incoming name when updating an existing stored name (assumes longer = more complete).
- Scalar fields: keep existing DB values unless the incoming value is non-empty and DB value is missing, except where a source has explicit higher priority (see address rule).
- Collections (SIC codes, officers, FCA permissions): union and deduplicate by canonical keys (code, name or permission text).

• VAT population
- A separate data/vat_lookups/*.json source is scanned. If a vat file contains a postcode, it is used to map into dealers by normalized postcode.
- The importer writes the vat lookup filename (without .json) into Dealer.VatNumber for dealers whose Postcode normalises to the same value.

• Assumptions about the data
- Primary audience is UK data: postcode formats, Companies House identifiers, FCA fields are UK-centric.
- Many source fields are optional, inconsistently named, or contain literal strings like "nan"; code treats these as missing.

• What I chose not to do (and next steps with more time)
- Add authenctication and authorization.
- No external VAT API lookup — currently we map VAT by filename/postcode only. With more time: call an authoritative VAT dataset/API.
- Hardening: more logging and unit tests.
- Better  UI and ability to modify dealers  data.

