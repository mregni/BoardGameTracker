import { beforeAll, describe, expect, it, vi } from "vitest";
import { act, fireEvent, renderWithTheme, screen, userEvent, waitFor } from "@/test/test-utils";
import { BgtSelect } from "./BgtSelect";
import { BgtSimpleSelect } from "./BgtSimpleSelect";

const items = [
	{ value: 1, label: "One player" },
	{ value: 2, label: "Two players" },
	{ value: 3, label: "Three players" },
];

const renderers = [
	{
		name: "BgtSelect",
		render: (onValueChange: (value: string | number) => void) =>
			renderWithTheme(<BgtSelect label="Players" value={null} onValueChange={onValueChange} items={items} />),
	},
	{
		name: "BgtSimpleSelect",
		render: (onValueChange: (value: string | number) => void) =>
			renderWithTheme(
				<BgtSimpleSelect placeholder="Players" value={null} onValueChange={onValueChange} items={items} />,
			),
	},
];

const flushOutsideListener = () => act(() => new Promise((resolve) => setTimeout(resolve, 0)));

const triggerRect = (left: number) => ({
	left,
	right: left + 100,
	top: 0,
	bottom: 40,
	width: 100,
	height: 40,
	x: left,
	y: 0,
	toJSON: () => ({}),
});

const pressOutside = (pointerType: string, clientX: number) =>
	fireEvent.pointerDown(document.documentElement, { pointerType, button: 0, clientX, clientY: 10 });

const openList = async (user: ReturnType<typeof userEvent.setup>) => {
	await user.click(screen.getByRole("combobox"));
	expect(await screen.findByRole("listbox")).toBeInTheDocument();
	await flushOutsideListener();
};

beforeAll(() => {
	Element.prototype.hasPointerCapture = vi.fn(() => false);
	Element.prototype.setPointerCapture = vi.fn();
	Element.prototype.releasePointerCapture = vi.fn();
	Element.prototype.scrollIntoView = vi.fn();
});

describe.each(renderers)("$name trigger toggle", ({ render }) => {
	it("closes when the trigger is clicked with the mouse and keeps focus on it", async () => {
		const user = userEvent.setup();
		render(vi.fn());
		const trigger = screen.getByRole("combobox");
		vi.spyOn(trigger, "getBoundingClientRect").mockReturnValue(triggerRect(0) as DOMRect);

		await openList(user);
		pressOutside("mouse", 50);
		fireEvent.click(document.documentElement);

		await waitFor(() => expect(screen.queryByRole("listbox")).not.toBeInTheDocument());
		expect(trigger).toHaveFocus();
		expect(trigger).toHaveAttribute("aria-expanded", "false");
	});

	it("stays closed when a tap on the trigger of a list opened by touch lands as a click on it", async () => {
		render(vi.fn());
		const trigger = screen.getByRole("combobox");
		vi.spyOn(trigger, "getBoundingClientRect").mockReturnValue(triggerRect(0) as DOMRect);

		fireEvent.pointerDown(trigger, { pointerType: "touch", button: 0 });
		fireEvent.click(trigger);
		expect(await screen.findByRole("listbox")).toBeInTheDocument();
		await flushOutsideListener();

		pressOutside("touch", 50);
		fireEvent.click(trigger);
		await flushOutsideListener();

		expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
		expect(trigger).toHaveAttribute("aria-expanded", "false");
		expect(trigger).toHaveFocus();
	});

	it("closes when the trigger receives the pointer press itself", async () => {
		const user = userEvent.setup({ pointerEventsCheck: 0 });
		render(vi.fn());
		const trigger = screen.getByRole("combobox");
		vi.spyOn(trigger, "getBoundingClientRect").mockReturnValue(triggerRect(0) as DOMRect);

		await openList(user);
		await user.click(trigger);

		await waitFor(() => expect(screen.queryByRole("listbox")).not.toBeInTheDocument());
		expect(trigger).toHaveFocus();
	});

	it("reopens on the next click after closing from the trigger", async () => {
		const user = userEvent.setup();
		render(vi.fn());
		const trigger = screen.getByRole("combobox");
		vi.spyOn(trigger, "getBoundingClientRect").mockReturnValue(triggerRect(0) as DOMRect);

		await openList(user);
		pressOutside("mouse", 50);
		fireEvent.click(document.documentElement);
		await waitFor(() => expect(screen.queryByRole("listbox")).not.toBeInTheDocument());

		await user.click(trigger);

		expect(await screen.findByRole("listbox")).toBeInTheDocument();
	});

	it("opens on a tap after an earlier close from the trigger", async () => {
		const user = userEvent.setup();
		render(vi.fn());
		const trigger = screen.getByRole("combobox");
		vi.spyOn(trigger, "getBoundingClientRect").mockReturnValue(triggerRect(0) as DOMRect);

		await openList(user);
		pressOutside("touch", 50);
		await waitFor(() => expect(screen.queryByRole("listbox")).not.toBeInTheDocument());

		fireEvent.pointerDown(trigger, { pointerType: "touch", button: 0 });
		fireEvent.click(trigger);

		expect(await screen.findByRole("listbox")).toBeInTheDocument();
	});

	it("still opens on a tap after the list was closed elsewhere", async () => {
		const user = userEvent.setup();
		render(vi.fn());
		const trigger = screen.getByRole("combobox");
		vi.spyOn(trigger, "getBoundingClientRect").mockReturnValue(triggerRect(500) as DOMRect);

		await openList(user);
		pressOutside("touch", 50);
		await waitFor(() => expect(screen.queryByRole("listbox")).not.toBeInTheDocument());

		fireEvent.pointerDown(trigger, { pointerType: "touch", button: 0 });
		fireEvent.click(trigger);

		expect(await screen.findByRole("listbox")).toBeInTheDocument();
	});

	it("still picks an option with the mouse", async () => {
		const user = userEvent.setup();
		const onValueChange = vi.fn();
		render(onValueChange);

		await openList(user);
		await user.click(screen.getByRole("option", { name: "Two players" }));

		expect(onValueChange).toHaveBeenCalledWith(2);
		await waitFor(() => expect(screen.queryByRole("listbox")).not.toBeInTheDocument());
	});

	it("still opens with the keyboard and closes with Escape", async () => {
		const user = userEvent.setup();
		render(vi.fn());
		const trigger = screen.getByRole("combobox");

		trigger.focus();
		await user.keyboard("{Enter}");
		expect(await screen.findByRole("listbox")).toBeInTheDocument();

		await user.keyboard("{Escape}");

		await waitFor(() => expect(screen.queryByRole("listbox")).not.toBeInTheDocument());
		expect(trigger).toHaveFocus();
	});
});
