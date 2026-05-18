import { normalizeBaseUrl } from "../core/formatters.js";

const hubPath = "/hubs/notifications";

export function createPartNotificationClient({ apiBaseUrl, token, onPartAdded }) {
  if (!token || !window.signalR?.HubConnectionBuilder) {
    return null;
  }

  const connection = new window.signalR.HubConnectionBuilder()
    .withUrl(`${normalizeBaseUrl(apiBaseUrl)}${hubPath}`, {
      accessTokenFactory: () => token,
      withCredentials: false
    })
    .withAutomaticReconnect()
    .build();

  connection.on("partAdded", (notification) => {
    if (notification) onPartAdded(notification);
  });

  return {
    start: () => connection.start(),
    stop: () => connection.stop()
  };
}
