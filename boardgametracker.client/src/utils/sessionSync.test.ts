import { afterEach, describe, expect, it, vi } from "vitest";

class FakeChannel {
	static instances: FakeChannel[] = [];
	listeners = new Set<(event: MessageEvent) => void>();
	posted: unknown[] = [];

	constructor(public name: string) {
		FakeChannel.instances.push(this);
	}

	postMessage(message: unknown) {
		this.posted.push(message);
	}

	addEventListener(_type: string, listener: (event: MessageEvent) => void) {
		this.listeners.add(listener);
	}

	removeEventListener(_type: string, listener: (event: MessageEvent) => void) {
		this.listeners.delete(listener);
	}

	deliver(data: unknown) {
		for (const listener of this.listeners) {
			listener(new MessageEvent("message", { data }));
		}
	}
}

const loadModule = async () => {
	vi.resetModules();
	return import("./sessionSync");
};

describe("sessionSync", () => {
	afterEach(() => {
		vi.unstubAllGlobals();
		FakeChannel.instances = [];
	});

	it("announces on one shared channel and hands other tabs' changes to the listener", async () => {
		vi.stubGlobal("BroadcastChannel", FakeChannel);
		const { announceSessionChange, listenForSessionChanges } = await loadModule();
		const handler = vi.fn();

		const stop = listenForSessionChanges(handler);
		announceSessionChange("login");
		const [channel] = FakeChannel.instances;
		channel.deliver("logout");
		channel.deliver("something-else");

		expect(FakeChannel.instances).toHaveLength(1);
		expect(channel.name).toBe("bgt-session");
		expect(channel.posted).toEqual(["login"]);
		expect(handler).toHaveBeenCalledTimes(1);
		expect(handler).toHaveBeenCalledWith("logout");

		stop();
		channel.deliver("login");
		expect(handler).toHaveBeenCalledTimes(1);
	});

	it("does nothing when the browser has no BroadcastChannel", async () => {
		vi.stubGlobal("BroadcastChannel", undefined);
		const { announceSessionChange, listenForSessionChanges } = await loadModule();

		expect(() => announceSessionChange("logout")).not.toThrow();
		const stop = listenForSessionChanges(vi.fn());
		expect(() => stop()).not.toThrow();
	});
});
