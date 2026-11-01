import { renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { useDocumentTitle } from "./useDocumentTitle";

describe("useDocumentTitle", () => {
	it("names the tab after the page and restores the old title on leave", () => {
		document.title = "BoardGameTracker";
		const { rerender, unmount } = renderHook(({ title }) => useDocumentTitle(title), {
			initialProps: { title: undefined as string | undefined },
		});
		expect(document.title).toBe("BoardGameTracker");

		rerender({ title: "Fun game" });
		expect(document.title).toBe("Fun game · BoardGameTracker");

		unmount();
		expect(document.title).toBe("BoardGameTracker");
	});
});
