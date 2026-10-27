import { differenceInMinutes } from "date-fns";
import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import XIcon from "@/assets/icons/x.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtIconButton } from "@/components/BgtIconButton/BgtIconButton";
import { BgtText } from "@/components/BgtText/BgtText";
import { useShortDuration } from "@/hooks/useShortDuration";
import type { Game, Player, RunningGame, Settings } from "@/models";
import { toDisplayTime } from "@/utils/dateUtils";

const TICK_MS = 30_000;

interface Props {
	runningGame: RunningGame;
	game: Game | null;
	players: Player[];
	settings: Settings;
	canWrite: boolean;
	onFinish: (runningGame: RunningGame) => void;
	onDiscard: (runningGame: RunningGame) => void;
}

export const RunningGameCard = (props: Props) => {
	const { runningGame, game, players, settings, canWrite, onFinish, onDiscard } = props;
	const { t } = useTranslation("game-nights");
	const shortDuration = useShortDuration();
	const [now, setNow] = useState(() => new Date());

	useEffect(() => {
		const timer = setInterval(() => setNow(new Date()), TICK_MS);
		return () => clearInterval(timer);
	}, []);

	const names = runningGame.playerIds
		.map((id) => players.find((player) => player.id === id)?.name)
		.filter((name): name is string => name !== undefined)
		.join(", ");

	return (
		<BgtCard className="border-primary/50" data-testid="running-game">
			<div className="grid grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-3">
				<BgtAvatar size="large" title={game?.title} image={game?.image} />
				<div className="flex min-w-0 flex-col gap-0.5">
					<BgtText size="3" weight="bold" className="wrap-anywhere">
						{t("play.now-playing", { game: game?.title ?? "" })}
					</BgtText>
					<BgtText size="2" color="gray">
						{t("play.running", {
							time: toDisplayTime(runningGame.start, settings.timeFormat, settings.uiLanguage),
							duration: shortDuration(Math.max(1, differenceInMinutes(now, runningGame.start))),
							players: names,
						})}
					</BgtText>
				</div>
				{canWrite && (
					<div className="flex items-center gap-1">
						<BgtButton variant="primary" onClick={() => onFinish(runningGame)}>
							{t("play.finish.button")}
						</BgtButton>
						<BgtIconButton
							intent="subtile"
							icon={<XIcon className="size-4" />}
							aria-label={t("play.discard.button", { game: game?.title ?? "" })}
							onClick={() => onDiscard(runningGame)}
						/>
					</div>
				)}
			</div>
		</BgtCard>
	);
};
