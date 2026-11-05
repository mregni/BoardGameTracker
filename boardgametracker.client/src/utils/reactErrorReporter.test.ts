import { beforeEach, describe, expect, it, vi } from "vitest";

const errorInfo = { componentStack: "at Dashboard" };
const consoleError = vi.spyOn(console, "error");

const loadReporter = async () => {
	vi.resetModules();
	return import("./reactErrorReporter");
};

describe("reportReactError", () => {
	beforeEach(() => {
		consoleError.mockImplementation(() => {});
	});

	it("should send errors from before the reporter was set exactly once", async () => {
		const { reportReactError, setReactErrorReporter } = await loadReporter();
		const reporter = vi.fn();
		const error = new Error("early");

		reportReactError(error, errorInfo);
		setReactErrorReporter(reporter);
		setReactErrorReporter(reporter);

		expect(reporter).toHaveBeenCalledTimes(1);
		expect(reporter).toHaveBeenCalledWith(error, errorInfo);
	});

	it("should send later errors directly", async () => {
		const { reportReactError, setReactErrorReporter } = await loadReporter();
		const reporter = vi.fn();
		const error = new Error("late");

		setReactErrorReporter(reporter);
		reportReactError(error, errorInfo);

		expect(reporter).toHaveBeenCalledWith(error, errorInfo);
	});

	it("should keep logging to the console", async () => {
		const { reportReactError } = await loadReporter();
		const error = new Error("logged");

		reportReactError(error, errorInfo);

		expect(consoleError).toHaveBeenCalledWith(error);
	});
});
