import { h, ReactDOM } from "./js/core/react-runtime.js";
import { App } from "./js/app-shell.js";
import { ErrorBoundary } from "./js/components/ErrorBoundary.js";

ReactDOM.createRoot(document.getElementById("root")).render(h(ErrorBoundary, null, h(App)));
