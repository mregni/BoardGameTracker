import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import SparklesIcon from "@/assets/icons/sparkles.svg?react";
import TrophyIcon from "@/assets/icons/trophy.svg?react";
import XIcon from "@/assets/icons/x.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtInputContainer } from "@/components/BgtForm";
import { BgtIconButton } from "@/components/BgtIconButton/BgtIconButton";
import type { CreateSessionPlayer, Player } from "@/models";

interface Props {
	entry: CreateSessionPlayer;
	player: Player | null;
	hasScoring: boolean;
	isCoop: boolean;
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
	const { entry, player, hasScoring, isCoop, disabled, onScoreChange, onToggleWon, onToggleFirstPlay, onRemove } =
		props;
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

	const firstPlayToggle = (
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
	);

	const removeButton = (className: string) => (
		<BgtIconButton
			icon={<XIcon className="size-4" />}
			intent="subtile"
			size="1"
			disabled={disabled}
			aria-label={t("form.player.remove", { name })}
			onClick={() => onRemove(entry.playerId)}
			className={className}
		/>
	);

	if (isCoop) {
		return (
			<li
				data-testid="session-player-row"
				className="grid grid-cols-[1.75rem_minmax(0,1fr)_auto_auto] items-center gap-x-2.5 rounded-lg border border-primary/30 bg-background px-2.5 py-1.5"
			>
				<BgtAvatar size="medium" title={name} image={player?.image} />
				<span className="min-w-0 truncate text-sm font-medium">{name}</span>
				{firstPlayToggle}
				{removeButton("justify-self-end")}
			</li>
		);
	}

	return (
		<li
			data-testid="session-player-row"
			className="grid grid-cols-[1.75rem_minmax(0,1fr)_auto] @md:grid-cols-[1.75rem_minmax(0,1fr)_auto_auto] items-center gap-x-2.5 gap-y-1.5 rounded-lg border border-primary/30 bg-background px-2.5 py-1.5"
		>
			<BgtAvatar size="medium" title={name} image={player?.image} />
			<span className="min-w-0 break-words text-sm font-medium">{name}</span>
			<div className="col-span-3 row-start-2 flex items-center justify-between gap-2 @md:col-span-1 @md:col-start-3 @md:row-start-1 @md:justify-end">
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
					{firstPlayToggle}
				</div>
			</div>
			{removeButton("col-start-3 row-start-1 @md:col-start-4 justify-self-end")}
		</li>
	);
};
