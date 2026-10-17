import { describe, expect, it } from "vitest";
import { useAppForm } from "@/hooks/form";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";
import { settingsFormOpts } from "../-utils/settingsFormOpts";
import { GeneralSettings } from "./GeneralSettings";

const Harness = ({ publicUrl }: { publicUrl: string }) => {
	const form = useAppForm({
		...settingsFormOpts,
		defaultValues: { ...settingsFormOpts.defaultValues, publicUrl },
	});

	return <GeneralSettings form={form} languages={[]} disabled={false} overrides={{}} />;
};

const publicUrlInput = () => screen.getByPlaceholderText(window.location.origin);

describe("GeneralSettings public URL", () => {
	it("leaves the field empty when unset, with the address in use as placeholder and a not-set hint", () => {
		renderWithProviders(<Harness publicUrl="" />);

		expect(publicUrlInput()).toHaveValue("");
		expect(screen.getByTestId("public-url-not-set")).toHaveTextContent("general.public-url.not-set");
		expect(screen.queryByRole("alert")).not.toBeInTheDocument();
	});

	it("drops the not-set hint once a URL is typed and warns about another host", async () => {
		const user = userEvent.setup();
		renderWithProviders(<Harness publicUrl="" />);

		await user.type(publicUrlInput(), "https://games.example.com");

		expect(screen.queryByTestId("public-url-not-set")).not.toBeInTheDocument();
		expect(screen.getByRole("alert")).toHaveTextContent("general.public-url.host-mismatch-warning");
	});

	it("shows neither hint nor warning for the address the app is opened on", () => {
		renderWithProviders(<Harness publicUrl={window.location.origin} />);

		expect(publicUrlInput()).toHaveValue(window.location.origin);
		expect(screen.queryByTestId("public-url-not-set")).not.toBeInTheDocument();
		expect(screen.queryByRole("alert")).not.toBeInTheDocument();
	});
});
