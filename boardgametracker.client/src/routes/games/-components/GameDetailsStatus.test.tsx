import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { GameDetailsStatus } from "./GameDetailsStatus";

describe("GameDetailsStatus", () => {
	it("shows a spinner while loading", () => {
		renderWithTheme(<GameDetailsStatus failed={false} onRetry={vi.fn()} />);

		expect(screen.getByRole("status")).toHaveTextContent("common:loading-data");
	});

	it("explains the failure and retries", async () => {
		const onRetry = vi.fn();
		renderWithTheme(<GameDetailsStatus failed onRetry={onRetry} />);

		expect(screen.getByRole("alert")).toHaveTextContent("load-error.details-failed");
		await userEvent.click(screen.getByRole("button", { name: "load-error.retry" }));
		expect(onRetry).toHaveBeenCalledTimes(1);
	});
});
