import { h } from "../../core/react-runtime.js";
import { KpiTile } from "./KpiTile.js";

// Ignition KpiBand (Report 03 §03) — thin responsive-grid wrapper over
// KpiTile. `tiles` is an array of KpiTile prop objects; `columns` picks the
// desktop grid (4 | 6 | 8). Rendered as role="list" with listitem tiles for
// screen readers (Report 03 §11). Loading renders the same tile count as
// skeletons so the band is the LCP element with zero layout shift.
export function KpiBand({ tiles = [], columns = 4, loading = false }) {
  const colsClass = columns === 6 ? " ig-cols-6" : columns === 8 ? " ig-cols-8" : "";

  return h("div", { className: `ig-kpi-band${colsClass}`, role: "list", "aria-label": "Key performance indicators" },
    tiles.map((tile, index) =>
      h("div", { key: tile.key || tile.label || index, role: "listitem", style: { display: "contents" } },
        h(KpiTile, { ...tile, loading: loading || tile.loading }))
    )
  );
}
