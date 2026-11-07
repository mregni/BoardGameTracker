import { describe, expect, it, vi } from "vitest";
import { handleFormSubmit, ignoreApiError } from "./formUtils";

const refused = { kind: "client", status: 400, message: "error.test.unknown", url: "/settings" };
const event = () =>
	({
		preventDefault: vi.fn(),
		stopPropagation: vi.fn(),
		currentTarget: document.createElement("form"),
	}) as unknown as React.FormEvent;

describe("handleFormSubmit", () => {
	it("stops the browser submit and runs the form", async () => {
		const submit = event();
		const handleSubmit = vi.fn().mockResolvedValue(undefined);

		await handleFormSubmit({ handleSubmit })(submit);

		expect(submit.preventDefault).toHaveBeenCalled();
		expect(submit.stopPropagation).toHaveBeenCalled();
		expect(handleSubmit).toHaveBeenCalled();
	});

	it("does not reject when the server refuses the save", async () => {
		await expect(handleFormSubmit({ handleSubmit: () => Promise.reject(refused) })(event())).resolves.toBeUndefined();
	});

	it("keeps rejecting on a programming error", async () => {
		const bug = new TypeError("bug");

		await expect(handleFormSubmit({ handleSubmit: () => Promise.reject(bug) })(event())).rejects.toBe(bug);
	});
});

describe("ignoreApiError", () => {
	it("accepts work that is not a promise", async () => {
		await expect(ignoreApiError(undefined)).resolves.toBeUndefined();
	});
});
