import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Crown from "@/assets/icons/crown.svg?react";
import Trophy from "@/assets/icons/trophy.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtNoData } from "@/components/BgtNoData/BgtNoData";
import type { Session } from "@/models";
import { useGameById } from "@/routes/-hooks/useGameById";
import { usePlayerById } from "@/routes/-hooks/usePlayerById";
import { mostPlayedGames, topWinners } from "../-utils/locationInsights";

interface Props {
	sessions: Session[];
}

interface Row {
	id: number;
	link: string;
	name: string;
	image: string | null | undefined;
	detail: string;
}

const RankedList = ({ rows, more }: { rows: Row[]; more?: string }) => {
	if (rows.length === 0) return <BgtNoData />;

	return (
		<>
			<ol className="flex flex-col divide-y divide-primary/10">
				{rows.map((row) => (
					<li key={row.id}>
						<Link to={row.link} className="flex items-center gap-3 py-2 hover:text-primary-text">
							<span aria-hidden="true">
								<BgtAvatar image={row.image} title={row.name} size="medium" />
							</span>
							<span className="min-w-0 flex-1 truncate font-semibold">{row.name}</span>
							<span className="shrink-0 text-sm tabular-nums text-white/60">{row.detail}</span>
						</Link>
					</li>
				))}
			</ol>
			{more && <p className="pt-2 text-sm text-white/60">{more}</p>}
		</>
	);
};

export const LocationPlayedHere = (props: Props) => {
	const { sessions } = props;
	const { t } = useTranslation(["location", "common"]);
	const { gameById } = useGameById();
	const { playerById } = usePlayerById();

	const playedGames = mostPlayedGames(sessions);
	const games = playedGames.top.flatMap(({ id, count }) => {
		const game = gameById(id);
		return game
			? [
					{
						id,
						link: `/games/${id}`,
						name: game.title,
						image: game.image,
						detail: t("detail.session-count", { count }),
					},
				]
			: [];
	});

	const winningPlayers = topWinners(sessions);
	const winners = winningPlayers.top.flatMap(({ id, count }) => {
		const player = playerById(id);
		return player
			? [
					{
						id,
						link: `/players/${id}`,
						name: player.name,
						image: player.image,
						detail: t("detail.win-count", { count }),
					},
				]
			: [];
	});

	return (
		<div className="grid gap-3 md:grid-cols-2 xl:gap-6">
			<BgtCard title={t("detail.most-played")} icon={Trophy}>
				<RankedList
					rows={games}
					more={playedGames.more > 0 ? t("detail.more-games", { count: playedGames.more }) : undefined}
				/>
			</BgtCard>
			<BgtCard title={t("detail.top-winners")} icon={Crown}>
				<RankedList
					rows={winners}
					more={winningPlayers.more > 0 ? t("detail.more-players", { count: winningPlayers.more }) : undefined}
				/>
			</BgtCard>
		</div>
	);
};
