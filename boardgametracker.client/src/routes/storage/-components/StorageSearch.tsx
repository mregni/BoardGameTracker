import { cx } from "class-variance-authority";
import { type KeyboardEvent, useId, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import SearchIcon from "@/assets/icons/magnifying-glass.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { type Expansion, type Game, type Location, StorageLayout, type StorageLocation } from "@/models";
import { getItemStateTranslationKey } from "@/utils/ItemStateUtils";
import { canStore, placeLabel, placeOf, type StoragePlace } from "@/utils/storage";

export interface SearchHit {
	game: Game;
	expansion: Expansion | null;
	place: StoragePlace | null;
	inBaseBox: boolean;
}

interface Props {
	games: Game[];
	nodes: StorageLocation[];
	locations: Location[];
	onPick: (hit: SearchHit) => void;
}

const SHOWN = 6;

const findHits = (games: Game[], query: string): SearchHit[] => {
	const term = query.trim().toLowerCase();
	if (!term) return [];
	return games.flatMap((game) => {
		const baseMatches = game.title.toLowerCase().includes(term);
		const hits: SearchHit[] = baseMatches ? [{ game, expansion: null, place: placeOf(game), inBaseBox: false }] : [];
		for (const expansion of game.expansions) {
			if (!baseMatches && !expansion.title.toLowerCase().includes(term)) continue;
			const own = placeOf(expansion);
			hits.push({
				game,
				expansion,
				place: own ?? (canStore(expansion.state) ? placeOf(game) : null),
				inBaseBox: own === null,
			});
		}
		return hits;
	});
};

const SlotMap = ({ place, nodes }: { place: StoragePlace | null; nodes: StorageLocation[] }) => {
	const slot = nodes.find((node) => node.id === place?.storageLocationId);
	const cabinet = nodes.find((node) => node.id === slot?.parentId);
	if (!slot || slot.layout !== StorageLayout.Slot || !cabinet) return null;
	const rows = cabinet.rows ?? 1;
	const columns = cabinet.columns ?? 1;
	const width = 56 / columns;
	const height = 44 / rows;
	return (
		<svg viewBox="0 0 56 44" className="h-11 w-14 flex-none max-md:hidden" aria-hidden="true">
			{Array.from({ length: rows * columns }, (_, index) => {
				const row = Math.floor(index / columns) + 1;
				const column = (index % columns) + 1;
				return (
					<rect
						key={index}
						x={(column - 1) * width + 1}
						y={(row - 1) * height + 1}
						width={width - 2}
						height={height - 2}
						rx="1.5"
						className={row === slot.row && column === slot.column ? "fill-card-value" : "fill-card-border"}
					/>
				);
			})}
		</svg>
	);
};

export const StorageSearch = ({ games, nodes, locations, onPick }: Props) => {
	const { t } = useTranslation(["storage", "game"]);
	const listId = useId();
	const inputRef = useRef<HTMLInputElement>(null);
	const [query, setQuery] = useState("");
	const [open, setOpen] = useState(false);
	const [active, setActive] = useState(0);
	const hits = findHits(games, query);
	const shown = hits.slice(0, SHOWN);
	const expanded = open && query.trim().length > 0;

	const pick = (hit: SearchHit) => {
		if (!canStore(hit.expansion?.state ?? hit.game.state)) return;
		setOpen(false);
		onPick(hit);
	};

	const keyDown = (event: KeyboardEvent<HTMLInputElement>) => {
		if (event.key === "Escape") {
			setOpen(false);
		} else if (event.key === "ArrowDown") {
			event.preventDefault();
			setOpen(true);
			setActive((index) => Math.min(index + 1, shown.length - 1));
		} else if (event.key === "ArrowUp") {
			event.preventDefault();
			setActive((index) => Math.max(index - 1, 0));
		} else if (event.key === "Enter" && shown[active]) {
			event.preventDefault();
			pick(shown[active]);
		}
	};

	return (
		<div
			className="relative mb-5 max-w-[760px]"
			onBlur={(event) => {
				if (!event.currentTarget.contains(event.relatedTarget as Node | null)) setOpen(false);
			}}
		>
			<label className="flex h-12 items-center gap-2 rounded-[10px] border border-primary/30 bg-background px-3.5 focus-within:border-primary">
				<SearchIcon className="size-5 flex-none text-white/60" aria-hidden="true" />
				<span className="sr-only">{t("page.search")}</span>
				<input
					ref={inputRef}
					role="combobox"
					aria-expanded={expanded}
					aria-controls={listId}
					aria-activedescendant={expanded && shown[active] ? `${listId}-${active}` : undefined}
					aria-autocomplete="list"
					autoComplete="off"
					value={query}
					placeholder={t("page.search")}
					onChange={(event) => {
						setQuery(event.target.value);
						setActive(0);
						setOpen(true);
					}}
					onFocus={() => setOpen(true)}
					onKeyDown={keyDown}
					className="min-w-0 flex-1 bg-transparent text-base text-white outline-none"
				/>
			</label>
			{expanded && (
				<div className="absolute inset-x-0 top-[calc(100%+6px)] z-20 max-h-[70vh] overflow-auto rounded-lg border border-card-border bg-card p-1.5 shadow-xl">
					{hits.length > 0 && (
						<p className="px-2.5 pb-1.5 pt-1 text-xs text-white/60">{t("page.found", { count: hits.length })}</p>
					)}
					<div id={listId} role="listbox" aria-label={t("page.search")} className="flex flex-col gap-0.5">
						{shown.map((hit, index) => {
							const storable = canStore(hit.expansion?.state ?? hit.game.state);
							const title = hit.expansion?.title ?? hit.game.title;
							return (
								// biome-ignore lint/a11y/useKeyWithClickEvents: the combobox input handles the keyboard
								<div
									key={`${hit.game.id}-${hit.expansion?.id ?? 0}`}
									id={`${listId}-${index}`}
									role="option"
									tabIndex={-1}
									aria-selected={index === active}
									aria-disabled={!storable}
									onMouseDown={(event) => event.preventDefault()}
									onClick={() => pick(hit)}
									onMouseEnter={() => setActive(index)}
									className={cx(
										"grid grid-cols-[40px_minmax(0,1fr)_auto] items-center gap-3 rounded-md px-2.5 py-2",
										storable ? "cursor-pointer" : "opacity-60",
										index === active && storable && "bg-primary/20",
									)}
								>
									<BgtAvatar image={hit.expansion?.image ?? hit.game.image} title={title} />
									<span className="min-w-0">
										<span className="font-bold">{title}</span>
										{hit.expansion && (
											<span className="text-xs text-white/60">
												{" "}
												· {t("page.expansion-of", { title: hit.game.title })}
											</span>
										)}
										<span className="mt-0.5 block text-sm text-white/80">
											{!storable
												? `${t(getItemStateTranslationKey(hit.expansion?.state ?? hit.game.state, false))} · ${t("page.not-in-collection")}`
												: hit.place
													? placeLabel(hit.place, nodes, locations, t)
													: t("place.none")}
											{storable && hit.expansion && hit.inBaseBox && hit.place && (
												<span className="text-xs text-white/50">
													{" "}
													· {t("place.with-base", { title: hit.game.title })}
												</span>
											)}
											{!hit.expansion && hit.game.isLoaned && (
												<span className="ml-1.5 rounded-full border border-warn/40 px-1.5 text-xs text-warn">
													{t("box.loaned-short")}
												</span>
											)}
										</span>
									</span>
									<SlotMap place={hit.place} nodes={nodes} />
								</div>
							);
						})}
					</div>
					{hits.length === 0 && (
						<p className="m-2 text-sm text-white/60">{t("page.not-found", { query: query.trim() })}</p>
					)}
				</div>
			)}
		</div>
	);
};
