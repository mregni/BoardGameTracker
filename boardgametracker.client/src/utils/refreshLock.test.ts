import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { LEASE_KEY, LEASE_MS, LEASE_POLL_MS, LEASE_SETTLE_MS, REFRESH_LOCK, withRefreshLock } from "./refreshLock";

const otherTabLease = (expiresIn: number) =>
	localStorage.setItem(LEASE_KEY, JSON.stringify({ owner: "other-tab", expires: Date.now() + expiresIn }));

describe("withRefreshLock", () => {
	beforeEach(() => {
		localStorage.clear();
		vi.useFakeTimers();
		vi.stubGlobal("navigator", { ...navigator, locks: undefined });
	});

	afterEach(() => {
		vi.useRealTimers();
		vi.unstubAllGlobals();
		vi.restoreAllMocks();
		localStorage.clear();
	});

	it("uses a Web Lock when the browser has one", async () => {
		const request = vi.fn((_name: string, task: () => Promise<unknown>) => task());
		vi.stubGlobal("navigator", { ...navigator, locks: { request } });

		await expect(withRefreshLock(async () => "done")).resolves.toBe("done");

		expect(request).toHaveBeenCalledWith(REFRESH_LOCK, expect.any(Function));
		expect(localStorage.getItem(LEASE_KEY)).toBeNull();
	});

	it("takes a lease in localStorage without Web Locks, and releases it afterwards", async () => {
		let leaseDuringTask: string | null = null;
		const pending = withRefreshLock(async () => {
			leaseDuringTask = localStorage.getItem(LEASE_KEY);
			return "done";
		});
		await vi.advanceTimersByTimeAsync(LEASE_SETTLE_MS);

		await expect(pending).resolves.toBe("done");
		expect(leaseDuringTask).not.toBeNull();
		expect(localStorage.getItem(LEASE_KEY)).toBeNull();
	});

	it("releases the lease when the task fails", async () => {
		const pending = withRefreshLock(async () => {
			throw new Error("refresh failed");
		});
		const assertion = expect(pending).rejects.toThrow("refresh failed");
		await vi.advanceTimersByTimeAsync(LEASE_SETTLE_MS);

		await assertion;
		expect(localStorage.getItem(LEASE_KEY)).toBeNull();
	});

	it("waits while another tab holds the lease, and runs once it is released", async () => {
		otherTabLease(LEASE_MS);
		const task = vi.fn(async () => "mine");

		const pending = withRefreshLock(task);
		await vi.advanceTimersByTimeAsync(LEASE_POLL_MS * 5);
		expect(task).not.toHaveBeenCalled();

		localStorage.removeItem(LEASE_KEY);
		await vi.advanceTimersByTimeAsync(LEASE_POLL_MS + LEASE_SETTLE_MS);

		await expect(pending).resolves.toBe("mine");
		expect(task).toHaveBeenCalledTimes(1);
	});

	it("takes over a lease that expired, for a tab that closed mid-refresh", async () => {
		otherTabLease(-1);
		const task = vi.fn(async () => "mine");

		const pending = withRefreshLock(task);
		await vi.advanceTimersByTimeAsync(LEASE_SETTLE_MS);

		await expect(pending).resolves.toBe("mine");
	});

	it("serialises two refreshes that start at the same moment", async () => {
		const order: string[] = [];
		const run = (name: string) =>
			withRefreshLock(async () => {
				order.push(`${name}:start`);
				await new Promise((resolve) => setTimeout(resolve, 200));
				order.push(`${name}:end`);
			});

		const first = run("a");
		await vi.advanceTimersByTimeAsync(LEASE_SETTLE_MS);
		const second = run("b");
		await vi.advanceTimersByTimeAsync(2_000);
		await Promise.all([first, second]);

		expect(order).toEqual(["a:start", "a:end", "b:start", "b:end"]);
	});

	it("loses a tie to the tab whose write landed last", async () => {
		const task = vi.fn(async () => "mine");

		const pending = withRefreshLock(task);
		otherTabLease(LEASE_MS);
		await vi.advanceTimersByTimeAsync(LEASE_SETTLE_MS + LEASE_POLL_MS);
		expect(task).not.toHaveBeenCalled();

		localStorage.removeItem(LEASE_KEY);
		await vi.advanceTimersByTimeAsync(LEASE_POLL_MS + LEASE_SETTLE_MS);
		await expect(pending).resolves.toBe("mine");
	});

	it("still refreshes after waiting a full lease, rather than hanging", async () => {
		otherTabLease(LEASE_MS * 10);
		const task = vi.fn(async () => "mine");

		const pending = withRefreshLock(task);
		await vi.advanceTimersByTimeAsync(LEASE_MS + LEASE_POLL_MS + LEASE_SETTLE_MS);

		await expect(pending).resolves.toBe("mine");
		expect(JSON.parse(localStorage.getItem(LEASE_KEY) ?? "{}").owner).toBe("other-tab");
	});

	it("ignores a malformed lease", async () => {
		localStorage.setItem(LEASE_KEY, "{not json");

		const pending = withRefreshLock(async () => "mine");
		await vi.advanceTimersByTimeAsync(LEASE_SETTLE_MS);

		await expect(pending).resolves.toBe("mine");
	});

	it("runs without a lock when storage is unavailable", async () => {
		vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => {
			throw new Error("quota");
		});

		await expect(withRefreshLock(async () => "mine")).resolves.toBe("mine");
	});

	it("runs without a lock when localStorage cannot be reached at all", async () => {
		vi.stubGlobal("localStorage", undefined);

		await expect(withRefreshLock(async () => "mine")).resolves.toBe("mine");
	});
});
