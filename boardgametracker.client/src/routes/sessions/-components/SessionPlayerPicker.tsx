import { cx } from "class-variance-authority";
import { type KeyboardEvent, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import PlusIcon from "@/assets/icons/plus.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import type { Player } from "@/models";

interface Props {
	inputId: string;
	players: Player[];
	selectedPlayerIds: number[];
	frequentPlayerIds: number[];
	disabled: boolean;
	hasErrors: boolean;
	onAddPlayer: (playerId: number) => void;
	onCreatePlayer: (name: string) => Promise<Player | undefined>;
}

type PickerOption = { kind: "player"; id: string; player: Player } | { kind: "create"; id: string; name: string };

export const SessionPlayerPicker = (props: Props) => {
	const { inputId, players, selectedPlayerIds, frequentPlayerIds, disabled, hasErrors, onAddPlayer, onCreatePlayer } =
		props;
	const { t } = useTranslation("player-session");

	const listId = `${inputId}-list`;
	const [query, setQuery] = useState("");
	const [open, setOpen] = useState(false);
	const [activeIndex, setActiveIndex] = useState(0);
	const [isCreating, setIsCreating] = useState(false);

	const options = useMemo<PickerOption[]>(() => {
		const available = players.filter((x) => !selectedPlayerIds.includes(x.id));
		const term = query.trim();

		if (term === "") {
			return frequentPlayerIds
				.map((id) => available.find((x) => x.id === id))
				.filter((x): x is Player => x !== undefined)
				.map((player) => ({ kind: "player", id: `${listId}-${player.id}`, player }));
		}

		const lower = term.toLowerCase();
		const matches: PickerOption[] = available
			.filter((x) => x.name.toLowerCase().includes(lower))
			.map((player) => ({ kind: "player", id: `${listId}-${player.id}`, player }));
		const exists = players.some((x) => x.name.trim().toLowerCase() === lower);

		return exists ? matches : [...matches, { kind: "create", id: `${listId}-create`, name: term }];
	}, [players, selectedPlayerIds, frequentPlayerIds, query, listId]);

	const showList = open && options.length > 0;

	const choose = async (option: PickerOption) => {
		if (isCreating) return;
		if (option.kind === "player") {
			onAddPlayer(option.player.id);
		} else {
			setIsCreating(true);
			try {
				const created = await onCreatePlayer(option.name);
				if (created) onAddPlayer(created.id);
			} finally {
				setIsCreating(false);
			}
		}
		setQuery("");
		setActiveIndex(0);
	};

	const handleKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
		switch (event.key) {
			case "ArrowDown":
				event.preventDefault();
				setOpen(true);
				setActiveIndex((index) => Math.min(index + 1, options.length - 1));
				break;
			case "ArrowUp":
				event.preventDefault();
				setActiveIndex((index) => Math.max(index - 1, 0));
				break;
			case "Enter": {
				event.preventDefault();
				const option = options[activeIndex];
				if (showList && option) void choose(option);
				break;
			}
			case "Escape":
				if (open) {
					event.preventDefault();
					setOpen(false);
				}
				break;
		}
	};

	return (
		<div className="relative">
			<input
				id={inputId}
				type="text"
				role="combobox"
				autoComplete="off"
				aria-expanded={showList}
				aria-controls={showList ? listId : undefined}
				aria-autocomplete="list"
				aria-activedescendant={showList ? options[activeIndex]?.id : undefined}
				placeholder={t("form.players.search")}
				value={query}
				disabled={disabled}
				aria-busy={isCreating}
				onChange={(event) => {
					setQuery(event.target.value);
					setActiveIndex(0);
					setOpen(true);
				}}
				onFocus={() => setOpen(true)}
				onBlur={() => setOpen(false)}
				onKeyDown={handleKeyDown}
				className={cx(
					"w-full bg-background text-white border border-primary/30 rounded-lg h-11 md:h-10 px-4 text-[15px]",
					"focus:border-primary focus:outline-none disabled:opacity-50 disabled:cursor-not-allowed",
					hasErrors && "border-error bg-error-dark!",
				)}
			/>
			{showList && (
				<div
					id={listId}
					role="listbox"
					aria-label={t("form.players.suggestions")}
					className="absolute left-0 right-0 top-full mt-1 z-30 bg-input border border-primary/30 rounded-lg shadow-xl p-1 max-h-72 overflow-y-auto"
				>
					{options.map((option, index) => (
						<div
							key={option.id}
							id={option.id}
							role="option"
							aria-label={
								option.kind === "player" ? option.player.name : t("form.players.create", { name: option.name })
							}
							tabIndex={-1}
							aria-selected={index === activeIndex}
							onMouseDown={(event) => event.preventDefault()}
							onMouseMove={() => setActiveIndex(index)}
							onClick={() => void choose(option)}
							onKeyDown={(event) => {
								if (event.key === "Enter") void choose(option);
							}}
							className={cx(
								"flex items-center gap-2 px-2 h-11 md:h-10 rounded-md text-[14px] cursor-pointer select-none",
								index === activeIndex && "bg-primary/40",
							)}
						>
							{option.kind === "player" ? (
								<>
									<BgtAvatar title={option.player.name} image={option.player.image} />
									<span className="flex-1 min-w-0 truncate">{option.player.name}</span>
								</>
							) : (
								<>
									<span className="size-7 rounded-md bg-white/10 flex items-center justify-center shrink-0">
										<PlusIcon className="size-4" />
									</span>
									<span className="flex-1 min-w-0 truncate">{t("form.players.create", { name: option.name })}</span>
								</>
							)}
						</div>
					))}
				</div>
			)}
		</div>
	);
};
