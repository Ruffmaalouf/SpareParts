import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { money } from "../core/formatters.js";
import { Badge, PageHeader, StatusLine } from "../components/shared.js";

const emptyForm = {
  title: "",
  description: "",
  videoUrl: "",
  thumbnailUrl: "",
  linkedPartId: "",
  partName: "",
  price: "",
  currency: "USD"
};

function statusTone(status) {
  if (status === "Active") return "positive";
  if (status === "Archived") return "neutral";
  return "neutral";
}

function ReelCard({ reel, onLike, onArchive, isSaving }) {
  return h("div", { className: "data-card" },
    reel.thumbnailUrl && h("div", { style: { marginBottom: "8px" } },
      h("img", {
        src: reel.thumbnailUrl,
        alt: reel.title,
        style: { width: "100%", maxHeight: "160px", objectFit: "cover", borderRadius: "4px" }
      })
    ),
    h("div", { className: "data-card-header" },
      h("strong", null, reel.title || "Untitled Reel"),
      h(Badge, { tone: statusTone(reel.statusLabel) }, reel.statusLabel || "Active")
    ),
    h("div", { className: "data-card-body" },
      reel.partName && h("div", null, h("span", null, "Part: "), h("strong", null, reel.partName)),
      reel.price && h("div", null, h("span", null, "Price: "), h("strong", null, money(reel.price, reel.currency || "USD"))),
      h("div", { style: { display: "flex", gap: "12px", marginTop: "4px" } },
        h("span", null, `${reel.likeCount || 0} likes`),
        h("span", null, `${reel.viewCount || 0} views`)
      ),
      reel.description && h("p", null, reel.description)
    ),
    h("div", { className: "row-actions" },
      reel.videoUrl && h("button", {
        type: "button",
        className: "secondary-button",
        onClick: () => window.open(reel.videoUrl, "_blank")
      }, "Play"),
      h("button", {
        type: "button",
        className: "secondary-button",
        onClick: () => onLike(reel.id),
        disabled: isSaving
      }, "Like"),
      reel.isOwner && reel.statusLabel !== "Archived" && h("button", {
        type: "button",
        className: "danger-button",
        onClick: () => onArchive(reel.id),
        disabled: isSaving
      }, "Archive")
    )
  );
}

export function PartReelView({ api }) {
  const [reels, setReels] = useState([]);
  const [form, setForm] = useState({ ...emptyForm });
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [showForm, setShowForm] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading reels...");
    try {
      const data = await api.get("/api/part-reels");
      setReels(Array.isArray(data) ? data : []);
      setStatus("");
    } catch (e) {
      setStatus(e.message || "Could not load reels.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const setField = useCallback((key, value) => {
    setForm(prev => ({ ...prev, [key]: value }));
  }, []);

  const handleSubmit = useCallback(async (e) => {
    e.preventDefault();
    if (!form.title.trim() || !form.videoUrl.trim()) {
      setStatus("Title and video URL are required.");
      return;
    }
    setIsSaving(true);
    setStatus("Creating reel...");
    try {
      await api.post("/api/part-reels", {
        title: form.title.trim(),
        description: form.description.trim() || null,
        videoUrl: form.videoUrl.trim(),
        thumbnailUrl: form.thumbnailUrl.trim() || null,
        partId: form.linkedPartId.trim() ? Number(form.linkedPartId.trim()) : null,
        partName: form.partName.trim() || null,
        price: form.price ? Number(form.price) : null,
        currency: form.currency
      });
      setForm({ ...emptyForm });
      setShowForm(false);
      setStatus("Reel created.");
      await load();
    } catch (e) {
      setStatus(e.message || "Could not create reel.");
    } finally {
      setIsSaving(false);
    }
  }, [api, form, load]);

  const handleLike = useCallback(async (id) => {
    setIsSaving(true);
    try {
      await api.put(`/api/part-reels/${id}/like`);
      await load();
    } catch (e) {
      setStatus(e.message || "Could not like reel.");
    } finally {
      setIsSaving(false);
    }
  }, [api, load]);

  const handleArchive = useCallback(async (id) => {
    if (!window.confirm("Archive this reel?")) return;
    setIsSaving(true);
    setStatus("Archiving reel...");
    try {
      await api.put(`/api/part-reels/${id}/archive`);
      setStatus("Reel archived.");
      await load();
    } catch (e) {
      setStatus(e.message || "Could not archive reel.");
    } finally {
      setIsSaving(false);
    }
  }, [api, load]);

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Part Reels",
      subtitle: "Short video showcases for your parts listings.",
      action: h("div", { className: "row-actions" },
        h("button", { type: "button", className: "secondary-button", onClick: load, disabled: isLoading }, "Refresh"),
        h("button", {
          type: "button",
          className: "primary-button",
          onClick: () => setShowForm(v => !v)
        }, showForm ? "Cancel" : "Create Reel")
      )
    }),

    h(StatusLine, { status }),

    showForm && h("form", { className: "admin-panel", onSubmit: handleSubmit, style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" },
        h("h3", null, "Create Reel"),
        h("button", { type: "submit", className: "primary-button", disabled: isSaving }, "Publish Reel")
      ),
      h("div", { className: "editor-grid" },
        h("label", null,
          h("span", null, "Title *"),
          h("input", { value: form.title, onChange: e => setField("title", e.target.value), required: true, placeholder: "e.g. BMW E46 Front Bumper - Like New" })
        ),
        h("label", null,
          h("span", null, "Video URL *"),
          h("input", { value: form.videoUrl, onChange: e => setField("videoUrl", e.target.value), required: true, placeholder: "https://..." })
        ),
        h("label", null,
          h("span", null, "Thumbnail URL"),
          h("input", { value: form.thumbnailUrl, onChange: e => setField("thumbnailUrl", e.target.value), placeholder: "https://..." })
        ),
        h("label", null,
          h("span", null, "Linked Part ID"),
          h("input", { value: form.linkedPartId, onChange: e => setField("linkedPartId", e.target.value), placeholder: "Internal part ID (optional)" })
        ),
        h("label", null,
          h("span", null, "Part Name"),
          h("input", { value: form.partName, onChange: e => setField("partName", e.target.value), placeholder: "e.g. Front Bumper" })
        ),
        h("label", null,
          h("span", null, "Price"),
          h("input", { type: "number", value: form.price, onChange: e => setField("price", e.target.value), placeholder: "0.00" })
        ),
        h("label", null,
          h("span", null, "Currency"),
          h("select", { value: form.currency, onChange: e => setField("currency", e.target.value) },
            h("option", { value: "USD" }, "USD"),
            h("option", { value: "LBP" }, "LBP")
          )
        ),
        h("label", null,
          h("span", null, "Description"),
          h("textarea", { value: form.description, onChange: e => setField("description", e.target.value), rows: 3, placeholder: "Brief description of the part..." })
        )
      )
    ),

    isLoading
      ? h("p", null, "Loading...")
      : reels.length === 0
        ? h("p", { className: "empty-state" }, "No reels yet. Create your first reel to showcase a part.")
        : h("div", { className: "data-card-grid" },
          reels.map(r =>
            h(ReelCard, {
              key: r.id,
              reel: r,
              onLike: handleLike,
              onArchive: handleArchive,
              isSaving
            })
          )
        )
  );
}
