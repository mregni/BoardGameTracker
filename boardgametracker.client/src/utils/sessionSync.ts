export type SessionChange = "login" | "logout";

const CHANNEL_NAME = "bgt-session";

let channel: BroadcastChannel | null | undefined;

const sessionChannel = (): BroadcastChannel | null => {
	if (channel === undefined) {
		channel = typeof BroadcastChannel === "undefined" ? null : new BroadcastChannel(CHANNEL_NAME);
	}
	return channel;
};

const isSessionChange = (value: unknown): value is SessionChange => value === "login" || value === "logout";

export const announceSessionChange = (change: SessionChange) => {
	sessionChannel()?.postMessage(change);
};

export const listenForSessionChanges = (handler: (change: SessionChange) => void): (() => void) => {
	const current = sessionChannel();
	if (!current) {
		return () => undefined;
	}

	const listener = (event: MessageEvent) => {
		if (isSessionChange(event.data)) {
			handler(event.data);
		}
	};
	current.addEventListener("message", listener);
	return () => current.removeEventListener("message", listener);
};
