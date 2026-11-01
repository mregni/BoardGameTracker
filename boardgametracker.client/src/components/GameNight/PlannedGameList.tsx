import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import Clock from "@/assets/icons/clock.svg?react";
import Users from "@/assets/icons/users.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { useDurationRange } from "@/hooks/useDurationRange";
import type { Game } from "@/models";
import { formatRange } from "@/utils/rangeUtils";

interface Props {
	games: Game[];
	onOpenGame?: (gameId: number) => void;
}

const row = "flex min-h-14 w-full items-center gap-3 rounded-lg bg-white/5 p-2 pr-3 text-left";

export const PlannedGameList = ({ games, onOpenGame }: Props) => {
	const { t } = useTranslation("common");
	const durationRange = useDurationRange();

	return (
		<ul className="flex flex-col gap-1.5">
			{games.map((game) => {
				const players = formatRange(game.minPlayers, game.maxPlayers);
				const duration = durationRange(game.minPlayTime, game.maxPlayTime);
				const content = (
					<>
						<BgtAvatar title={game.title} image={game.image} size="large" />
						<span className="flex min-w-0 flex-col gap-0.5">
							<span className="wrap-anywhere text-sm font-semibold text-white">{game.title}</span>
							{(players !== null || duration !== null) && (
								<span className="flex flex-wrap gap-x-3 text-xs text-gray-400 tabular-nums">
									{players !== null && (
										<span className="inline-flex items-center gap-1">
											<Users className="size-3.5" aria-hidden />
											<span className="sr-only">{t("players")}:</span>
											{players}
										</span>
									)}
									{duration !== null && (
										<span className="inline-flex items-center gap-1">
											<Clock className="size-3.5" aria-hidden />
											<span className="sr-only">{t("duration")}:</span>
											{duration}
										</span>
									)}
								</span>
							)}
						</span>
					</>
				);

				return (
					<li key={game.id}>
						{onOpenGame ? (
							<button
								type="button"
								onClick={() => onOpenGame(game.id)}
								className={cx(row, "transition-colors hover:bg-white/10")}
							>
								{content}
							</button>
						) : (
							<div className={row}>{content}</div>
						)}
					</li>
				);
			})}
		</ul>
	);
};
