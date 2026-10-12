import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import SparklesIcon from "@/assets/icons/sparkles.svg?react";
import TrophyIcon from "@/assets/icons/trophy.svg?react";
import XIcon from "@/assets/icons/x.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtInputContainer } from "@/components/BgtForm";
import type { CreateSessionPlayer, Player } from "@/models";

interface Props {
	entry: CreateSessionPlayer;
	player: Player | null;
	hasScoring: boolean;
	disabled: boolean;
	onScoreChange: (playerId: number, score: number | null) => void;
	onToggleWon: (playerId: number) => void;
	onToggleFirstPlay: (playerId: number) => void;
	onRemove: (playerId: number) => void;
}

const toScoreText = (score: number | null | undefined) => (score === null || score === undefined ? "" : String(score));

const toggleClasses =
	"inline-flex items-center gap-1 h-8 px-2 rounded-full border text-xs font-semibold whitespace-nowrap";

export const SessionPlayerRow = (props: Props) => {
	const { entry, player, hasScoring, disabled, onScoreChange, onToggleWon, onToggleFirstPlay, onRemove } = props;
	const { t } = useTranslation("player-session");
	const name = player?.name ?? "";

	const handleScoreChange = (value: string) => {
		if (value.trim() === "") {
			onScoreChange(entry.playerId, null);
			return;
		}
		const parsed = Number(value);
		if (!Number.isNaN(parsed)) onScoreChange(entry.playerId, parsed);
	};

	return (
		<li className="grid grid-cols-[1.75rem_minmax(0,1fr)_auto] @xl:grid-cols-[1.75rem_minmax(0,1fr)_auto_auto] items-center gap-x-2.5 gap-y-1.5 rounded-lg border border-primary/30 bg-background px-2.5 py-1.5">
			<BgtAvatar size="medium" title={name} image={player?.image} />
			<span className="min-w-0 break-words text-sm font-medium">{name}</span>
			<div className="col-span-3 row-start-2 flex items-center justify-between gap-2 @xl:col-span-1 @xl:col-start-3 @xl:row-start-1 @xl:justify-end">
				{hasScoring ? (
					<BgtInputContainer disabled={disabled} className="h-9 w-20 shrink-0 px-2! text-sm">
						<input
							type="number"
							inputMode="numeric"
							value={toScoreText(entry.score)}
							disabled={disabled}
							aria-label={t("form.player.score", { name })}
							placeholder={t("form.player.score-placeholder")}
							onChange={(event) => handleScoreChange(event.target.value)}
							className="hide-arrow h-full w-full bg-transparent text-center font-semibold tabular-nums text-white focus:outline-none disabled:cursor-not-allowed [appearance:textfield]"
						/>
					</BgtInputContainer>
				) : (
					<span />
				)}
				<div className="flex items-center gap-1.5">
					<button
						type="button"
						aria-pressed={entry.won}
						disabled={disabled}
						onClick={() => onToggleWon(entry.playerId)}
						className={cx(
							toggleClasses,
							entry.won
								? "border-yellow-500 text-yellow-400 bg-yellow-500/10"
								: "border-primary/30 text-white/60 hover:text-white hover:border-primary/60",
						)}
					>
						<TrophyIcon className="size-3" />
						{t("form.player.won")}
					</button>
					<button
						type="button"
						aria-pressed={entry.firstPlay}
						disabled={disabled}
						onClick={() => onToggleFirstPlay(entry.playerId)}
						className={cx(
							toggleClasses,
							entry.firstPlay
								? "border-card-value text-card-value bg-card-value/10"
								: "border-primary/30 text-white/60 hover:text-white hover:border-primary/60",
						)}
					>
						<SparklesIcon className="size-3" />
						{t("form.player.first-play")}
					</button>
				</div>
			</div>
			<button
				type="button"
				disabled={disabled}
				aria-label={t("form.player.remove", { name })}
				title={t("form.player.remove", { name })}
				onClick={() => onRemove(entry.playerId)}
				className="col-start-3 row-start-1 @xl:col-start-4 justify-self-end size-8 inline-flex items-center justify-center rounded-md text-white/60 hover:text-white hover:bg-white/10 disabled:opacity-50"
			>
				<XIcon className="size-4" />
			</button>
		</li>
	);
};
