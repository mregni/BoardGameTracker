import { describe, expect, it, vi } from "vitest";
import type { IntegrationConnectionStatus, IntegrationStatus } from "@/models/Settings/IntegrationStatus";
import { render, screen } from "@/test/test-utils";
import { IntegrationStatusBadge } from "./IntegrationStatusBadge";

vi.mock("react-i18next", () => ({
	useTranslation: () => ({
		t: (key: string, options?: { time?: string }) => (options?.time ? `${key} ${options.time}` : key),
		i18n: { language: "en-US" },
	}),
}));

const createStatus = (status: IntegrationConnectionStatus, messageKey = "ok"): IntegrationStatus => ({
	integration: "bgg",
	status,
	checkedAt: new Date(2026, 9, 16, 9, 5).toISOString(),
	messageKey,
});

describe("IntegrationStatusBadge", () => {
	it.each([
		["ok", "text-ok"],
		["unauthorized", "text-bad"],
		["unreachable", "text-warn"],
		["rateLimited", "text-warn"],
		["notConfigured", "text-white/60"],
	] as const)("renders the %s status with its token", (status, className) => {
		render(<IntegrationStatusBadge status={createStatus(status)} isChecking={false} timeFormat="HH:mm" />);

		expect(screen.getByText(`integrations.status.${status}`)).toHaveClass(className);
	});

	it("shows when a connected integration was checked, in the user's time format", () => {
		render(<IntegrationStatusBadge status={createStatus("ok")} isChecking={false} timeFormat="HH:mm" />);

		expect(screen.getByText("integrations.checked-at 09:05")).toBeInTheDocument();
		expect(screen.queryByText("integrations.reason.ok")).not.toBeInTheDocument();
	});

	it("explains why a connection failed", () => {
		render(
			<IntegrationStatusBadge
				status={createStatus("unauthorized", "unauthorized")}
				isChecking={false}
				timeFormat="HH:mm"
			/>,
		);

		expect(screen.getByText("integrations.reason.unauthorized")).toBeInTheDocument();
		expect(screen.getByText("integrations.checked-at 09:05")).toBeInTheDocument();
	});

	it("does not show a check time or reason when nothing is configured", () => {
		render(
			<IntegrationStatusBadge
				status={createStatus("notConfigured", "not-configured")}
				isChecking={false}
				timeFormat="HH:mm"
			/>,
		);

		expect(screen.queryByText(/integrations.checked-at/)).not.toBeInTheDocument();
		expect(screen.queryByText("integrations.reason.not-configured")).not.toBeInTheDocument();
	});

	it("marks the badge busy while it is re-checked", () => {
		render(<IntegrationStatusBadge status={createStatus("ok")} isChecking timeFormat="HH:mm" />);

		expect(screen.getByRole("status")).toHaveAttribute("aria-busy", "true");
		expect(screen.getByText("integrations.checking")).toBeInTheDocument();
	});

	it("shows a checking placeholder before the first status arrives", () => {
		render(<IntegrationStatusBadge status={undefined} isChecking timeFormat="HH:mm" />);

		expect(screen.getByRole("status")).toHaveTextContent("integrations.checking");
	});

	it("renders nothing without a status", () => {
		const { container } = render(<IntegrationStatusBadge status={undefined} isChecking={false} timeFormat="HH:mm" />);

		expect(container).toBeEmptyDOMElement();
	});
});
