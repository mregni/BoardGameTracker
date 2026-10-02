import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({ canWrite: true }));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ canWrite: mocks.canWrite }),
}));

vi.mock("@tanstack/react-router", () => ({
	Navigate: ({ to }: { to: string }) => <div>redirect to {to}</div>,
}));

import { RequireWrite } from "./RequireWrite";

describe("RequireWrite", () => {
	it("shows the page to users who can write", () => {
		mocks.canWrite = true;
		render(<RequireWrite>form</RequireWrite>);

		expect(screen.getByText("form")).toBeInTheDocument();
	});

	it("sends readers home", () => {
		mocks.canWrite = false;
		render(<RequireWrite>form</RequireWrite>);

		expect(screen.queryByText("form")).not.toBeInTheDocument();
		expect(screen.getByText("redirect to /")).toBeInTheDocument();
	});
});
