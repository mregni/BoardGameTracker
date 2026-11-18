import { cx } from "class-variance-authority";
import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import CheckIcon from "@/assets/icons/check.svg?react";
import InfoIcon from "@/assets/icons/info.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { StoragePicker } from "@/components/BgtStorage/StoragePicker";
import { useMediaQuery } from "@/hooks/useMediaQuery";
import type { Game } from "@/models";
import type { StoragePlace } from "@/utils/storage";

interface Props {
	games: Game[];
	checked: Set<number>;
	flashId: number | null;
	onFlashEnd: () => void;
	canWrite: boolean;
	onToggle: (gameId: number, checked: boolean) => void;
	onMove: (place: StoragePlace) => void;
}

const useLeavingRows = (games: Game[]) => {
	const reduced = useMediaQuery("(prefers-reduced-motion: reduce)");
	const [previous, setPrevious] = useState(games);
	const [leaving, setLeaving] = useState<{ game: Game; index: number }[]>([]);
	const [countFlash, setCountFlash] = useState(0);
	const [putAway, setPutAway] = useState(0);

	if (previous.length !== games.length || previous.some((game, index) => game.id !== games[index].id)) {
		const ids = new Set(games.map((game) => game.id));
		const gone = previous.flatMap((game, index) => (ids.has(game.id) ? [] : [{ game, index }]));
		setPrevious(games);
		if (gone.length > 0) {
			setCountFlash((count) => count + 1);
			setPutAway((count) => count + gone.length);
			if (!reduced) setLeaving((rows) => [...rows.filter((row) => !ids.has(row.game.id)), ...gone]);
		}
	}

	const rows = games.map((game) => ({ game, gone: false }));
	for (const row of [...leaving].sort((a, b) => a.index - b.index)) {
		rows.splice(Math.min(row.index, rows.length), 0, { game: row.game, gone: true });
	}
	const left = (gameId: number) => setLeaving((current) => current.filter((row) => row.game.id !== gameId));

	return { rows, left, countFlash, putAway };
};

export const NeedsAPlaceCard = ({ games, checked, flashId, onFlashEnd, canWrite, onToggle, onMove }: Props) => {
	const { t } = useTranslation("storage");
	const { rows, left, countFlash, putAway } = useLeavingRows(games);

	useEffect(() => {
		if (flashId === null) return;
		const reduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
		document
			.querySelector(`[data-needs="${flashId}"]`)
			?.scrollIntoView({ block: "nearest", behavior: reduced ? "auto" : "smooth" });
	}, [flashId]);

	return (
		<BgtCard aria-labelledby="needs-a-place-title">
			<h2 id="needs-a-place-title" className="mb-2 flex items-center gap-2 tracking-wide">
				<InfoIcon className="size-5 text-primary" aria-hidden="true" />
				{t("inbox.title")}
				<span
					key={countFlash}
					className={cx(
						"-mx-1 rounded px-1 text-sm font-medium tabular-nums text-white/60",
						countFlash > 0 && "animate-value-flash",
					)}
				>
					{games.length}
				</span>
			</h2>
			{rows.length === 0 && putAway > 0 && (
				<output className="flex items-center gap-3 rounded-lg bg-ok/10 p-3">
					<span className="relative grid size-9 flex-none place-items-center">
						<span
							aria-hidden="true"
							className="absolute inset-0 rounded-full border-2 border-ok opacity-0 motion-safe:animate-done-ring"
						/>
						<span className="grid size-9 place-items-center rounded-full bg-ok/20 text-ok motion-safe:animate-done-pop">
							<CheckIcon className="size-5" aria-hidden="true" />
						</span>
					</span>
					<span className="min-w-0">
						<span className="block font-semibold">{t("inbox.done")}</span>
						<span className="text-sm text-white/70">{t("inbox.put-away", { count: putAway })}</span>
					</span>
				</output>
			)}
			{rows.length === 0 && putAway === 0 && (
				<p className="flex items-center gap-1.5 text-sm text-white/60">
					<CheckIcon className="size-4 text-ok" aria-hidden="true" />
					{t("inbox.done")}
				</p>
			)}
			{rows.length > 0 && (
				<>
					<p className="mb-2 text-sm text-white/60">{t("inbox.description")}</p>
					<ul className="flex max-h-[45vh] flex-col overflow-y-auto">
						{rows.map(({ game, gone }) => (
							<li
								key={game.id}
								inert={gone}
								onAnimationEnd={(event) => gone && event.target === event.currentTarget && left(game.id)}
								className={cx("border-b border-card-border last:border-b-0", gone && "overflow-hidden animate-row-out")}
							>
								<label
									data-needs={game.id}
									onAnimationEnd={flashId === game.id ? onFlashEnd : undefined}
									className={cx(
										"grid cursor-pointer grid-cols-[18px_36px_minmax(0,1fr)] items-center gap-2.5 rounded px-0.5 py-1.5",
										flashId === game.id && "animate-value-flash",
									)}
								>
									<input
										type="checkbox"
										className="size-[18px] accent-primary"
										checked={gone || checked.has(game.id)}
										disabled={!canWrite}
										onChange={(event) => onToggle(game.id, event.target.checked)}
									/>
									<BgtAvatar image={game.image} title={game.title} />
									<span className="truncate font-semibold">{game.title}</span>
								</label>
							</li>
						))}
					</ul>
					{canWrite && (
						<StoragePicker
							className="mt-3"
							value={null}
							onChange={(place) => place && onMove(place)}
							placeholder={checked.size > 0 ? t("inbox.move", { count: checked.size }) : t("inbox.select")}
							aria-label={checked.size > 0 ? t("inbox.move", { count: checked.size }) : t("inbox.select")}
							disabled={checked.size === 0}
							canCreate
						/>
					)}
				</>
			)}
		</BgtCard>
	);
};
