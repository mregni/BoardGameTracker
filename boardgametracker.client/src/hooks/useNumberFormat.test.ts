import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useDecimalInput } from "./useDecimalInput";
import { useNumberFormat } from "./useNumberFormat";

const mocks = vi.hoisted(() => ({ language: "nl-BE" }));

vi.mock("react-i18next", () => ({
	useTranslation: () => ({ t: (key: string) => key, i18n: { language: mocks.language } }),
}));

describe("useNumberFormat", () => {
	beforeEach(() => {
		mocks.language = "nl-BE";
	});

	it("formats with the ui language", () => {
		const { result } = renderHook(() => useNumberFormat());

		expect(result.current.locale).toBe("nl-BE");
		expect(result.current.decimal(8.14)).toBe("8,1");
		expect(result.current.decimal(8, 1, 1)).toBe("8,0");
		expect(result.current.integer(12345)).toBe("12.345");
		expect(result.current.percent(0.5)).toBe("50%");
		expect(result.current.ratio(1, 4)).toBe("25%");
		expect(result.current.price(12.5, "€")).toBe("€12,50");
		expect(result.current.fileSize(1536)).toBe("1,5 KB");
		expect(result.current.input(1234.5)).toBe("1234,5");
		expect(result.current.number(0.12345, { maximumFractionDigits: 2 })).toBe("0,12");
	});

	it("prefers an explicit culture", () => {
		const { result } = renderHook(() => useNumberFormat("en-US"));

		expect(result.current.locale).toBe("en-US");
		expect(result.current.decimal(8.1)).toBe("8.1");
	});

	it("follows a language change", () => {
		const { result, rerender } = renderHook(() => useNumberFormat());
		expect(result.current.decimal(2.5)).toBe("2,5");

		mocks.language = "en-US";
		rerender();

		expect(result.current.decimal(2.5)).toBe("2.5");
	});
});

describe("useDecimalInput", () => {
	beforeEach(() => {
		mocks.language = "nl-BE";
	});

	it("shows the value in the culture format", () => {
		const { result } = renderHook(() => useDecimalInput(8.1, vi.fn()));

		expect(result.current.value).toBe("8,1");
	});

	it.each([
		["8,1", 8.1],
		["8.1", 8.1],
		["", null],
	])("parses %s", (typed, expected) => {
		const onValueChange = vi.fn();
		const { result } = renderHook(() => useDecimalInput(null, onValueChange));

		act(() => result.current.onChange(typed));

		expect(onValueChange).toHaveBeenCalledWith(expected);
		expect(result.current.value).toBe(typed);
	});

	it("ignores characters that cannot be part of a number", () => {
		const onValueChange = vi.fn();
		const { result } = renderHook(() => useDecimalInput(2, onValueChange));

		act(() => result.current.onChange("2a"));

		expect(onValueChange).not.toHaveBeenCalled();
		expect(result.current.value).toBe("2");
	});

	it("shows the formatted value again after blur", () => {
		const { result, rerender } = renderHook(({ value }) => useDecimalInput(value, vi.fn()), {
			initialProps: { value: 3 as number | null },
		});

		act(() => result.current.onChange("3.50"));
		expect(result.current.value).toBe("3.50");

		rerender({ value: 3.5 });
		act(() => result.current.onBlur());

		expect(result.current.value).toBe("3,5");
	});
});
