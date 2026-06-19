import { h, useState } from "../../core/react-runtime.js";

// Gallery: large preview region + thumbnail strip region, with a fallback
// monogram region when no images are present (parts/vehicles often lack
// photos).
export function ImageGallery({ images, fallbackLabel, onUpload }) {
  const list = Array.isArray(images) ? images.filter(Boolean) : [];
  const [activeIndex, setActiveIndex] = useState(0);
  const active = list[Math.min(activeIndex, list.length - 1)];

  return h("div", { className: "image-gallery-v2" },
    h("div", { className: "image-gallery-v2-preview" },
      active
        ? h("img", { src: active.url || active, alt: active.alt || "Photo" })
        : h("span", { className: "image-gallery-v2-fallback" }, String(fallbackLabel || "?").slice(0, 2).toUpperCase()),
      onUpload && h("button", { type: "button", className: "image-gallery-v2-upload", onClick: onUpload }, "Add photo")
    ),
    list.length > 1 && h("div", { className: "image-gallery-v2-thumbs" },
      list.map((image, index) =>
        h("button", {
          key: image.id || index,
          type: "button",
          className: index === activeIndex ? "image-gallery-v2-thumb active" : "image-gallery-v2-thumb",
          onClick: () => setActiveIndex(index)
        }, h("img", { src: image.url || image, alt: image.alt || `Photo ${index + 1}` }))
      )
    )
  );
}
