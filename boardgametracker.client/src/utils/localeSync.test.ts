import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { describe, expect, it } from "vitest";

const localesRoot = join(__dirname, "..", "..", "public", "locales");
const baseDir = join(localesRoot, "base");
const locales = readdirSync(localesRoot).filter((name) => name !== "base");
const namespaces = readdirSync(baseDir).filter((name) => name.endsWith(".json"));

const flattenKeys = (value: unknown, prefix = ""): string[] => {
	if (value === null || typeof value !== "object" || Array.isArray(value)) {
		return [prefix];
	}

	return Object.entries(value as Record<string, unknown>).flatMap(([key, child]) =>
		flattenKeys(child, prefix ? `${prefix}.${key}` : key),
	);
};

const readKeys = (locale: string, namespace: string): string[] => {
	const file = readFileSync(join(localesRoot, locale, namespace), "utf-8");
	return flattenKeys(JSON.parse(file)).sort();
};

describe("locale files", () => {
	it("should ship every base namespace in every locale", () => {
		for (const locale of locales) {
			const files = readdirSync(join(localesRoot, locale));
			expect(files, `${locale} is missing namespaces`).toEqual(expect.arrayContaining(namespaces));
		}
	});

	it.each(locales)("%s should contain exactly the keys of base", (locale) => {
		for (const namespace of namespaces) {
			const baseKeys = readKeys("base", namespace);
			const localeKeys = readKeys(locale, namespace);
			const missing = baseKeys.filter((key) => !localeKeys.includes(key));
			const extra = localeKeys.filter((key) => !baseKeys.includes(key));
			expect({ namespace, missing, extra }).toEqual({ namespace, missing: [], extra: [] });
		}
	});
});
