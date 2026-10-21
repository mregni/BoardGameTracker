export const REFRESH_LOCK = "bgt-token-refresh";
export const LEASE_KEY = "bgt-token-refresh-lease";
export const LEASE_MS = 15_000;
export const LEASE_SETTLE_MS = 40;
export const LEASE_POLL_MS = 100;

interface Lease {
	owner: string;
	expires: number;
}

const randomId = () =>
	Array.from(globalThis.crypto.getRandomValues(new Uint32Array(2)), (part) => part.toString(36)).join("");

const tabId = `${Date.now().toString(36)}-${randomId()}`;

const sleep = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

const storage = (): Storage | null => {
	try {
		return globalThis.localStorage ?? null;
	} catch {
		return null;
	}
};

const readLease = (store: Storage): Lease | null => {
	try {
		const value = JSON.parse(store.getItem(LEASE_KEY) ?? "null") as Partial<Lease> | null;
		return typeof value?.owner === "string" && typeof value.expires === "number"
			? { owner: value.owner, expires: value.expires }
			: null;
	} catch {
		return null;
	}
};

const isFree = (lease: Lease | null) => lease === null || lease.expires <= Date.now();

const tryAcquire = async (store: Storage, owner: string): Promise<boolean> => {
	if (!isFree(readLease(store))) {
		return false;
	}
	store.setItem(LEASE_KEY, JSON.stringify({ owner, expires: Date.now() + LEASE_MS }));
	await sleep(LEASE_SETTLE_MS);
	return readLease(store)?.owner === owner;
};

const release = (store: Storage, owner: string): boolean => {
	try {
		if (readLease(store)?.owner !== owner) {
			return false;
		}
		store.removeItem(LEASE_KEY);
		return true;
	} catch {
		return false;
	}
};

const withLease = async <T>(store: Storage, task: () => Promise<T>): Promise<T> => {
	const owner = `${tabId}-${randomId()}`;
	const giveUpAt = Date.now() + LEASE_MS;
	let acquired = false;
	try {
		while (!acquired && Date.now() < giveUpAt) {
			acquired = await tryAcquire(store, owner);
			if (!acquired) {
				await sleep(LEASE_POLL_MS);
			}
		}
	} catch {
		acquired = false;
	}

	try {
		return await task();
	} finally {
		if (acquired) {
			release(store, owner);
		}
	}
};

export const withRefreshLock = <T>(task: () => Promise<T>): Promise<T> => {
	const locks = globalThis.navigator?.locks;
	if (locks) {
		return locks.request(REFRESH_LOCK, () => task()) as Promise<T>;
	}
	const store = storage();
	return store ? withLease(store, task) : task();
};
