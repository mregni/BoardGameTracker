import { readdirSync, readFileSync, statSync } from "node:fs";
import { join } from "node:path";
import { describe, expect, it } from "vitest";

const clientRoot = join(__dirname, "..", "..");
const baseDir = join(clientRoot, "public", "locales", "base");
const srcDir = join(clientRoot, "src");
const backendConstants = join(clientRoot, "..", "BoardGameTracker.Common", "Constants.cs");
const defaultNamespace = "common";

const flattenKeys = (value: unknown, prefix = ""): string[] => {
	if (value === null || typeof value !== "object" || Array.isArray(value)) {
		return [prefix];
	}

	return Object.entries(value as Record<string, unknown>).flatMap(([key, child]) =>
		flattenKeys(child, prefix ? `${prefix}.${key}` : key),
	);
};

const baseKeys = new Map(
	readdirSync(baseDir)
		.filter((name) => name.endsWith(".json"))
		.map((name) => [name.slice(0, -5), new Set(flattenKeys(JSON.parse(readFileSync(join(baseDir, name), "utf-8"))))]),
);

const sourceFiles = (dir: string): string[] =>
	readdirSync(dir).flatMap((name) => {
		const path = join(dir, name);
		if (statSync(path).isDirectory()) {
			return sourceFiles(path);
		}

		return /\.(ts|tsx)$/.test(name) && !/\.test\.|\.d\.ts$|routeTree\.gen/.test(name) ? [path] : [];
	});

const keyExists = (namespace: string, key: string) => {
	const keys = baseKeys.get(namespace);
	return keys !== undefined && (keys.has(key) || keys.has(`${key}_one`) || keys.has(`${key}_other`));
};

const hookNamespaces = (source: string): string[] => {
	const calls = [...source.matchAll(/useTranslation\(\s*(?:"([^"]+)"|\[\s*"([^"]+)")?/g)];
	return calls.length === 0 ? [defaultNamespace] : calls.map((call) => call[1] ?? call[2] ?? defaultNamespace);
};

const resolves = (literal: string, namespaces: string[]) => {
	if (literal.includes(":")) {
		const [namespace, key] = literal.split(/:(.*)/s);
		return keyExists(namespace, key);
	}

	return namespaces.some((namespace) => keyExists(namespace, literal));
};

describe("translation keys used in code", () => {
	it("should all exist in the namespace the hook resolves them in", () => {
		const unresolved: string[] = [];
		for (const file of sourceFiles(srcDir)) {
			const source = readFileSync(file, "utf-8");
			const namespaces = hookNamespaces(source);
			for (const match of source.matchAll(/\bt\(\s*"([^"${}]+)"/g)) {
				if (!resolves(match[1], namespaces)) {
					unresolved.push(`${file.slice(clientRoot.length + 1)}: ${match[1]}`);
				}
			}
		}

		expect(unresolved).toEqual([]);
	});
});

describe("error codes returned by the backend", () => {
	it("should all be translated in the error namespace", () => {
		const codes = [...readFileSync(backendConstants, "utf-8").matchAll(/"error\.([a-z0-9.-]+)"/g)].map(
			(match) => match[1],
		);

		expect(codes.length).toBeGreaterThan(0);
		expect(codes.filter((code) => !keyExists("error", code))).toEqual([]);
	});
});
