import type { AnyFieldApi } from "@tanstack/react-form";
import { cx } from "class-variance-authority";
import { type KeyboardEvent, useEffect, useId, useMemo, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import CaretDownIcon from "@/assets/icons/caret-down.svg?react";
import CaretUpIcon from "@/assets/icons/caret-up.svg?react";
import CheckIcon from "@/assets/icons/check.svg?react";
import SearchIcon from "@/assets/icons/magnifying-glass.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { FormFieldWrapper } from "@/components/BgtForm";
import type { Game } from "@/models";

interface Props {
	field: AnyFieldApi;
	label: string;
	games: Game[];
	recentGameIds: number[];
	placeholder: string;
	disabled?: boolean;
}

interface GameGroup {
	key: "recent" | "all";
	label: string;
	games: Game[];
}

interface GameOption {
	id: string;
	group: GameGroup["key"];
	game: Game;
}

export const SessionGameCombobox = (props: Props) => {
	const { field, label, games, recentGameIds, placeholder, disabled = false } = props;
	const { t } = useTranslation("player-session");

	const baseId = useId();
	const triggerId = `${baseId}-trigger`;
	const listId = `${baseId}-list`;
	const wrapperRef = useRef<HTMLDivElement>(null);
	const triggerRef = useRef<HTMLButtonElement>(null);
	const searchRef = useRef<HTMLInputElement>(null);

	const [open, setOpen] = useState(false);
	const [query, setQuery] = useState("");
	const [activeIndex, setActiveIndex] = useState(0);

	const selectedId = Number(field.state.value ?? 0);
	const selectedGame = games.find((x) => x.id === selectedId);
	const hasErrors = field.state.meta.errors.length > 0;

	const groups = useMemo<GameGroup[]>(() => {
		const term = query.trim().toLowerCase();
		if (term !== "") {
			return [
				{
					key: "all",
					label: t("form.game.all"),
					games: games.filter((x) => x.title.toLowerCase().includes(term)),
				},
			];
		}

		const recent = recentGameIds.map((id) => games.find((x) => x.id === id)).filter((x): x is Game => x !== undefined);

		return [
			{ key: "recent" as const, label: t("form.game.recent"), games: recent },
			{ key: "all" as const, label: t("form.game.all"), games },
		].filter((group) => group.games.length > 0);
	}, [games, recentGameIds, query, t]);

	const options = useMemo<GameOption[]>(
		() =>
			groups.flatMap((group) =>
				group.games.map((game) => ({ id: `${listId}-${group.key}-${game.id}`, group: group.key, game })),
			),
		[groups, listId],
	);

	useEffect(() => {
		if (!open) return;
		searchRef.current?.focus();
		const handlePointerDown = (event: MouseEvent) => {
			if (!wrapperRef.current?.contains(event.target as Node)) {
				setOpen(false);
				setQuery("");
			}
		};
		document.addEventListener("mousedown", handlePointerDown);
		return () => document.removeEventListener("mousedown", handlePointerDown);
	}, [open]);

	useEffect(() => {
		if (!open) return;
		document.getElementById(options[activeIndex]?.id ?? "")?.scrollIntoView?.({ block: "nearest" });
	}, [open, activeIndex, options]);

	const openList = () => {
		if (disabled) return;
		const selectedIndex = options.findIndex((x) => x.game.id === selectedId);
		setActiveIndex(selectedIndex === -1 ? 0 : selectedIndex);
		setOpen(true);
	};

	const close = (focusTrigger: boolean) => {
		setOpen(false);
		setQuery("");
		if (focusTrigger) triggerRef.current?.focus();
	};

	const select = (game: Game) => {
		field.handleChange(game.id);
		field.handleBlur();
		close(true);
	};

	const handleQueryChange = (value: string) => {
		setQuery(value);
		setActiveIndex(0);
	};

	const handleSearchKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
		switch (event.key) {
			case "ArrowDown":
				event.preventDefault();
				setActiveIndex((index) => Math.min(index + 1, options.length - 1));
				break;
			case "ArrowUp":
				event.preventDefault();
				setActiveIndex((index) => Math.max(index - 1, 0));
				break;
			case "Home":
				event.preventDefault();
				setActiveIndex(0);
				break;
			case "End":
				event.preventDefault();
				setActiveIndex(Math.max(options.length - 1, 0));
				break;
			case "Enter": {
				event.preventDefault();
				const option = options[activeIndex];
				if (option) select(option.game);
				break;
			}
			case "Escape":
				event.preventDefault();
				close(true);
				break;
			case "Tab":
				close(false);
				break;
		}
	};

	const handleTriggerKeyDown = (event: KeyboardEvent<HTMLButtonElement>) => {
		if (event.key === "ArrowDown" || event.key === "ArrowUp") {
			event.preventDefault();
			openList();
		}
	};

	return (
		<FormFieldWrapper label={label} htmlFor={triggerId} errors={field.state.meta.errors}>
			<div ref={wrapperRef} className="relative">
				<button
					ref={triggerRef}
					id={triggerId}
					type="button"
					disabled={disabled}
					aria-haspopup="listbox"
					aria-expanded={open}
					aria-controls={open ? listId : undefined}
					onClick={() => (open ? close(false) : openList())}
					onKeyDown={handleTriggerKeyDown}
					className={cx(
						"w-full bg-background text-white border border-primary/30 focus:border-primary focus:outline-none",
						"py-2 h-11 md:h-10 shadow-none inline-flex justify-between items-center gap-2 rounded-lg leading-none text-[15px] text-left",
						selectedGame ? "pl-2 pr-4" : "px-4",
						hasErrors && "border border-error bg-error-dark!",
						disabled && "opacity-50 cursor-not-allowed",
					)}
				>
					{selectedGame && <BgtAvatar title={selectedGame.title} image={selectedGame.image} />}
					<span className="flex-1 min-w-0 truncate">{selectedGame?.title ?? placeholder}</span>
					{open ? <CaretUpIcon className="size-5 shrink-0" /> : <CaretDownIcon className="size-5 shrink-0" />}
				</button>

				{open && (
					<div className="absolute left-0 right-0 top-full mt-[5px] z-30 overflow-hidden bg-input rounded-md">
						<div className="p-2 border-b border-gray-700">
							<div className="flex items-center px-2 bg-input rounded-sm">
								<SearchIcon className="size-4 text-gray-400 mr-2" />
								<input
									ref={searchRef}
									type="text"
									role="combobox"
									aria-expanded={true}
									aria-controls={listId}
									aria-autocomplete="list"
									aria-activedescendant={options[activeIndex]?.id}
									aria-label={t("form.game.search")}
									placeholder={t("form.game.search")}
									value={query}
									onChange={(event) => handleQueryChange(event.target.value)}
									onKeyDown={handleSearchKeyDown}
									className="bg-transparent border-none outline-hidden py-2 text-sm w-full"
								/>
							</div>
						</div>
						<div
							id={listId}
							role="listbox"
							aria-label={t("form.game.list")}
							className="p-1 max-h-[300px] overflow-y-auto"
						>
							{options.length === 0 && (
								<div className="text-[13px] py-2 px-4 text-gray-400">{t("form.game.no-results")}</div>
							)}
							{groups.map((group) => (
								<fieldset key={group.key} className="m-0 min-w-0 border-0 p-0">
									<legend className="text-[11px] uppercase tracking-widest text-gray-400 pl-[25px] pr-2 pt-2 pb-1">
										{group.label}
									</legend>
									{options.map((option, index) => {
										if (option.group !== group.key) return null;
										const game = option.game;
										const isActive = index === activeIndex;
										const isSelected = game.id === selectedId;
										return (
											<div
												key={option.id}
												id={option.id}
												role="option"
												aria-label={game.title}
												tabIndex={-1}
												aria-selected={isSelected}
												onMouseDown={(event) => event.preventDefault()}
												onMouseMove={() => setActiveIndex(index)}
												onClick={() => select(game)}
												onKeyDown={(event) => {
													if (event.key === "Enter") select(game);
												}}
												className={cx(
													"relative flex items-center gap-2 text-[13px] leading-none rounded-lg h-11 md:h-10 pr-[35px] pl-[25px] cursor-pointer select-none",
													isActive && "bg-primary/60",
												)}
											>
												{isSelected && (
													<span className="absolute left-0 w-[25px] inline-flex items-center justify-center">
														<CheckIcon className="size-4" />
													</span>
												)}
												<BgtAvatar title={game.title} image={game.image} />
												<span className="flex-1 min-w-0 truncate">{game.title}</span>
											</div>
										);
									})}
								</fieldset>
							))}
						</div>
					</div>
				)}
			</div>
		</FormFieldWrapper>
	);
};
