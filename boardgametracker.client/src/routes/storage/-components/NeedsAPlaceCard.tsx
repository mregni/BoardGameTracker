import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import CheckIcon from "@/assets/icons/check.svg?react";
import InfoIcon from "@/assets/icons/info.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { StoragePicker } from "@/components/BgtStorage/StoragePicker";
import type { Game } from "@/models";
import type { StoragePlace } from "@/utils/storage";

interface Props {
	games: Game[];
	checked: Set<number>;
	highlightId: number | null;
	canWrite: boolean;
	onToggle: (gameId: number, checked: boolean) => void;
	onMove: (place: StoragePlace) => void;
}

export const NeedsAPlaceCard = ({ games, checked, highlightId, canWrite, onToggle, onMove }: Props) => {
	const { t } = useTranslation("storage");

	return (
		<BgtCard aria-labelledby="needs-a-place-title">
			<h2 id="needs-a-place-title" className="mb-2 flex items-center gap-2 tracking-wide">
				<InfoIcon className="size-5 text-primary" aria-hidden="true" />
				{t("inbox.title")}
				<span className="text-sm font-medium tabular-nums text-white/60">{games.length}</span>
			</h2>
			{games.length === 0 ? (
				<p className="flex items-center gap-1.5 text-sm text-white/60">
					<CheckIcon className="size-4 text-ok" aria-hidden="true" />
					{t("inbox.done")}
				</p>
			) : (
				<>
					<p className="mb-2 text-sm text-white/60">{t("inbox.description")}</p>
					<ul className="flex max-h-[45vh] flex-col overflow-y-auto">
						{games.map((game) => (
							<li key={game.id} className="border-b border-card-border last:border-b-0">
								<label
									className={cx(
										"grid cursor-pointer grid-cols-[18px_36px_minmax(0,1fr)] items-center gap-2.5 px-0.5 py-1.5",
										highlightId === game.id && "animate-pulse rounded bg-card-value/15",
									)}
								>
									<input
										type="checkbox"
										className="size-[18px] accent-primary"
										checked={checked.has(game.id)}
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
