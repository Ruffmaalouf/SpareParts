# Shop Marketing Workflow — How It Works

Four agents, four automated runs, one folder to check each morning.

| When | Agent | What it does | Output |
|---|---|---|---|
| Monday 6:38 AM | shop-profit-analyst | Reads the DB profit export, ranks high-margin parts by make/model/year, flags restock opportunities, adds top items to the queue | `reports/profit-feedback-*.md` |
| Monday 7:01 AM | shop-marketing-strategy | Plans the week's themes across FB/IG/TikTok, led by the profit feedback | `strategy/weekly-calendar-*.md` |
| Daily 8:07 AM | shop-marketing (+ scriptwriter for video days) | Writes ready-to-post captions/listings; writes a shootable script for any video-format day | `daily-posts/*.md`, `video-scripts/*.md` |
| Daily 9:01 AM | shop-video-editor | Checks for footage you filmed and edits it (trim, captions, resize for vertical, music if supplied) | `edited-videos/*.mp4` |
| Anytime | any agent above | Available on demand too — just ask in chat | — |

## Feeding in real profit data (margin by car make/model/year)

The profit analyst can't connect live to your SQL Server — this environment can't reach it (verified, not a permissions issue). Instead:

1. Run `marketing/reports/profit-by-fitment-export.sql` in SSMS (or your app's SQL tool) — it's read-only, safe to run anytime.
2. Export the result to `marketing/reports/parts-profit-export.csv` (and optionally the second query to `parts-sales-velocity-export.csv`).
3. Monday morning, the profit analyst reads it, ranks high-margin parts by make/model/year, flags what's high-margin but low on stock, and feeds the top items straight into `content-queue.csv`.

For zero manual work going forward: ask whoever manages your SQL Server to schedule that query as a nightly SQL Agent job that drops the CSV into that folder automatically. Then it's always fresh without you touching it. These CSVs are gitignored — they hold real cost/pricing data and will never get committed.

## Your job each day (~5-10 minutes)

1. Check `daily-posts/YYYY-MM-DD.md` — copy-paste the FB Marketplace / Instagram / TikTok text into each app.
2. If there's a video script that day (`video-scripts/YYYY-MM-DD-script.md`), film it on your phone following the shot list, then drop the clips into `raw-footage/incoming/`. Next morning's 9:01 AM run edits it automatically — check `edited-videos/` for the finished file.
3. Keep `content-queue.csv` filled with real parts (30 seconds per row) so the daily content has something concrete to feature.

## Your only job: keep the queue filled (30 seconds per part)

Open `content-queue.csv` (Excel or Notepad both work) and add a row whenever you want a part pushed:

| Column | What to put |
|---|---|
| Part | Name + fitment, e.g. "Front Brake Rotor Set - Toyota Camry 2015-2019" |
| Price | Just the number |
| Condition/Notes | New/reconditioned/OEM/aftermarket, stock count, anything worth mentioning |
| Priority | High / Medium / Low — High gets picked first |
| Status | Leave as "Pending" — the agent flips it to "Posted - [date]" once it's used |

Delete the two "Example:" rows whenever — they're just there to show the format.

## If the queue is empty

The agent won't skip the day — it falls back to evergreen content (maintenance tips, top-seller spotlights, OEM-vs-aftermarket explainers, "leave us a review" prompts) so there's always something to post. But real parts with real prices convert better than evergreen filler, so keeping the queue filled is worth the 30 seconds.

## Where your posts land

Check `daily-posts/` each morning — one file per day, everything pre-written for Facebook Marketplace, Instagram, and TikTok.

## Note on automation

I can't safely auto-post directly to Facebook, Instagram, or TikTok — that risks the accounts getting flagged. This system does all the writing/thinking for you; posting itself is a manual copy-paste. If you want to go further, Meta Business Suite (free, official) lets you schedule Facebook + Instagram posts in advance from the pre-written content — worth setting up if you want to batch a week at once instead of daily.
