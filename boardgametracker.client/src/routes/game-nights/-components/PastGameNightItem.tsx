import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import CaretRight from "@/assets/icons/caret-right.svg?react";
import { BgtText } from "@/components/BgtText/BgtText";
import { GameNightDateBox } from "@/components/GameNight/GameNightDateBox";
import { type GameNight, GameNightRsvpState, type Player, type Settings } from "@/models";

interface Props {
	gameNight: GameNight;
	settings: Settings;
	players: Player[];
}

export const PastGameNightItem = (props: Props) => {
	const { gameNight, settings, players } = props;
	const { t } = useTranslation("game-nights");

	const attended = gameNight.invitedPlayers.filter((rsvp) => rsvp.state === GameNightRsvpState.Accepted).length;
	const { gamesPlayed, winnerIds } = gameNight.summary;
	const winners = winnerIds
		.map((id) => players.find((player) => player.id === id)?.name)
		.filter((name): name is string => name !== undefined)
		.join(", ");

	const summary = [
		gameNight.location.name,
		t("past-night.attended-count", { count: attended }),
		gamesPlayed === 0 ? t("past-night.nothing-logged") : t("past-night.games-played", { count: gamesPlayed }),
		winners && t("past-night.winner", { names: winners }),
	]
		.filter(Boolean)
		.join(" · ");

	return (
		<Link
			to="/game-nights/$gameNightId"
			params={{ gameNightId: gameNight.id }}
			className="grid grid-cols-[3.25rem_minmax(0,1fr)_auto] items-center gap-3 rounded-lg border border-primary/20 bg-primary/10 px-3 py-2.5 hover:border-primary/60"
		>
			<GameNightDateBox date={gameNight.startDate} uiLanguage={settings.uiLanguage} variant="compact" />
			<div className="flex min-w-0 flex-col gap-0.5">
				<BgtText size="3" weight="bold" className="wrap-anywhere">
					{gameNight.title}
				</BgtText>
				<BgtText size="2" color="gray">
					{summary}
				</BgtText>
			</div>
			<CaretRight className="size-4 text-gray-400" />
		</Link>
	);
};
