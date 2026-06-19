import { h } from "../../core/react-runtime.js";

// Form section: heading region (title + optional hint), a responsive field
// grid region for inputs, used to break long add/edit forms into scannable
// chunks instead of one flat stack of labels.
export function FormSection({ title, hint, columns = 2, children }) {
  return h("section", { className: "form-section-v2" },
    (title || hint) && h("div", { className: "form-section-v2-head" },
      title && h("strong", null, title),
      hint && h("span", null, hint)
    ),
    h("div", { className: "form-section-v2-grid", style: { "--form-section-columns": columns } }, children)
  );
}
